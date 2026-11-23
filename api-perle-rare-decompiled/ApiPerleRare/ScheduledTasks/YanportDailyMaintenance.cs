using System;
using System.Collections.Generic;
using System.Linq;
using ApiPerleRare.Application.Doublons;

namespace ApiPerleRare.ScheduledTasks;

/// <summary>
/// Maintenance quotidienne unifiée et économique de la base Yanport (Lot 1 fantômes + Lot 2 doublons)
/// en une seule passe API par lot de 150 IDs :
/// - Bucket A : nouveaux biens créés dans les dernières 36 h (détection immédiate des doublons multi-agences).
/// - Bucket B : biens actifs sans mise à jour depuis plus de 14 jours, découpés en 14 tranches quotidiennes
///   déterministes (rotation complète tous les 14 jours pour détecter les 404/fermés et rafraîchir les doublons).
/// - Nettoyage sans crédit des liens orphelins (<c>P_DoublonPropertyId</c> pointant vers un bien fermé) avec
///   promotion automatique du maître suivant si plusieurs doublons restent actifs.
/// </summary>
internal static class YanportDailyMaintenance
{
	internal const string EnvVar = "PR_YANPORT_DAILY_MAINTENANCE";

	internal const string CursorParam = "YanportDailyMaintenanceDay";

	internal const string TaskLogName = "YanportDailyMaintenance";

	internal const int RunHourParis = 4;

	internal const int BatchSize = 150;

	internal const int NewPropertyWindowHours = 36;

	internal const int StaleThresholdDays = 14;

	internal const int RotationDays = 14;

	internal const int MaxTargetsPerRun = 2500;

	internal const int MaxDoublonUpdatesAbort = 5000;

	internal const string SelectActiveSql =
		"SELECT P_PropertyId, P_TypeTransaction, P_DateDebut, P_DateCreation, P_DateLastUpdate, P_DoublonPropertyId "
		+ "FROM property "
		+ "WHERE P_State = 1 "
		+ "AND (P_DateFin IS NULL OR P_DateFin < '1000-01-01') "
		+ "AND IFNULL(P_EstRattrapage, 0) <> 4";

	internal sealed record ActiveRow(
		string PropertyId,
		string TypeTransaction,
		DateTime? DateDebut,
		DateTime? DateCreation,
		DateTime? DateLastUpdate,
		string CurrentDoublonPropertyId);

	internal sealed class TargetSelection
	{
		public List<ActiveRow> Targets { get; } = new List<ActiveRow>();

		public int NewCount { get; set; }

		public int StaleEligibleCount { get; set; }

		public int StaleSliceCount { get; set; }

		public int CappedCount { get; set; }

		public int DaySlot { get; set; }
	}

	internal static bool Enabled(string value)
	{
		return string.Equals(value?.Trim(), "on", StringComparison.OrdinalIgnoreCase);
	}

	internal static DateTime ToParisTime(DateTime utc)
	{
		return YanportGhostCleanup.ToParisTime(utc);
	}

	internal static int DayKey(DateTime utc)
	{
		return YanportGhostCleanup.DayKey(utc);
	}

	internal static bool Due(int? lastDay, DateTime utc)
	{
		DateTime paris = ToParisTime(utc);
		if (paris.Hour < RunHourParis)
		{
			return false;
		}
		return lastDay != paris.Year * 10000 + paris.Month * 100 + paris.Day;
	}

	/// <summary>Hash FNV-1a 32 bits déterministe (indépendant du process .NET) pour la rotation sur 14 jours.</summary>
	internal static int StableBucket(string propertyId, int bucketCount)
	{
		if (bucketCount <= 1 || string.IsNullOrEmpty(propertyId))
		{
			return 0;
		}
		uint hash = 2166136261u;
		foreach (char c in propertyId)
		{
			hash ^= c;
			hash *= 16777619u;
		}
		return (int)(hash % (uint)bucketCount);
	}

	internal static int RotationSlotForParisDate(DateTime parisNow)
	{
		int daysSinceEpoch = (int)(parisNow.Date - new DateTime(2020, 1, 1)).TotalDays;
		int mod = daysSinceEpoch % RotationDays;
		return mod < 0 ? mod + RotationDays : mod;
	}

	internal static TargetSelection SelectTargetsForDay(
		IReadOnlyList<ActiveRow> actives,
		DateTime parisNow,
		int maxTargets = MaxTargetsPerRun)
	{
		TargetSelection selection = new TargetSelection
		{
			DaySlot = RotationSlotForParisDate(parisNow)
		};
		if (actives == null || actives.Count == 0 || maxTargets <= 0)
		{
			return selection;
		}

		DateTime newCutoff = parisNow.AddHours(-NewPropertyWindowHours);
		DateTime staleCutoff = parisNow.AddDays(-StaleThresholdDays);

		List<ActiveRow> bucketNew = new List<ActiveRow>();
		List<ActiveRow> bucketStale = new List<ActiveRow>();
		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

		foreach (ActiveRow row in actives)
		{
			string id = row?.PropertyId?.Trim();
			if (string.IsNullOrEmpty(id) || !seen.Add(id))
			{
				continue;
			}
			bool isNew = row.DateCreation.HasValue
				&& row.DateCreation.Value.Year >= 1000
				&& row.DateCreation.Value >= newCutoff;
			if (isNew)
			{
				bucketNew.Add(row);
				continue;
			}

			bool isStale = !row.DateLastUpdate.HasValue
				|| row.DateLastUpdate.Value.Year < 1000
				|| row.DateLastUpdate.Value < staleCutoff;
			if (isStale)
			{
				selection.StaleEligibleCount++;
				if (StableBucket(id, RotationDays) == selection.DaySlot)
				{
					bucketStale.Add(row);
				}
			}
		}

		// Priorité aux nouveaux biens (doublons détectés dès J+1), puis tranche des anciens.
		bucketNew.Sort((ActiveRow a, ActiveRow b) => string.Compare(a.PropertyId, b.PropertyId, StringComparison.Ordinal));
		bucketStale.Sort((ActiveRow a, ActiveRow b) => string.Compare(a.PropertyId, b.PropertyId, StringComparison.Ordinal));

		foreach (ActiveRow row in bucketNew)
		{
			if (selection.Targets.Count >= maxTargets)
			{
				selection.CappedCount++;
				continue;
			}
			selection.Targets.Add(row);
			selection.NewCount++;
		}
		foreach (ActiveRow row in bucketStale)
		{
			if (selection.Targets.Count >= maxTargets)
			{
				selection.CappedCount++;
				continue;
			}
			selection.Targets.Add(row);
			selection.StaleSliceCount++;
		}

		return selection;
	}

	/// <summary>
	/// Calcule les mises à jour incrémentales de <c>P_DoublonPropertyId</c> sans jamais effacer les liens
	/// des biens non interrogés ce jour-là :
	/// 1. Préserve les liens existants valides entre biens encore actifs de même transaction.
	/// 2. Si un maître a été fermé (par l'import ou par le Lot 1), relie ses survivants actifs entre eux
	///    pour élire le maître suivant sans casser le groupe (0 crédit API).
	/// 3. Fusionne les arêtes fraîches <c>hit.properties[]</c> des biens interrogés aujourd'hui.
	/// 4. Délie un bien explicitement interrogé dont Yanport renvoie 0 voisin.
	/// </summary>
	internal static YanportDoublonSync.SyncPlan ComputeIncrementalDoublonPlan(
		IReadOnlyList<ActiveRow> allActives,
		IReadOnlySet<string> closedIds,
		IReadOnlySet<string> queriedIds,
		IReadOnlyDictionary<string, IReadOnlyList<string>> relatedById,
		IReadOnlySet<string> rejectedPairKeys = null)
	{
		YanportDoublonSync.SyncPlan plan = new YanportDoublonSync.SyncPlan();
		if (allActives == null || allActives.Count == 0)
		{
			return plan;
		}

		bool IsRejected(string a, string b)
		{
			if (rejectedPairKeys == null || rejectedPairKeys.Count == 0)
			{
				return false;
			}
			string key = DoublonReviewLogic.CanonicalPairKey(a, b);
			return !string.IsNullOrEmpty(key) && rejectedPairKeys.Contains(key);
		}

		Dictionary<string, YanportDoublonSync.ActiveProperty> byId = new Dictionary<string, YanportDoublonSync.ActiveProperty>(StringComparer.Ordinal);
		foreach (ActiveRow row in allActives)
		{
			string id = row?.PropertyId?.Trim();
			if (string.IsNullOrEmpty(id))
			{
				continue;
			}
			if (closedIds != null && closedIds.Contains(id))
			{
				continue;
			}
			byId[id] = new YanportDoublonSync.ActiveProperty(
				id,
				row.TypeTransaction,
				row.DateDebut,
				row.DateCreation,
				row.CurrentDoublonPropertyId);
		}

		Dictionary<string, HashSet<string>> adj = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
		Dictionary<string, List<YanportDoublonSync.ActiveProperty>> orphansByDeadMaster = new Dictionary<string, List<YanportDoublonSync.ActiveProperty>>(StringComparer.Ordinal);

		// 1. Liens existants en base
		foreach (YanportDoublonSync.ActiveProperty prop in byId.Values)
		{
			string curParent = prop.CurrentDoublonPropertyId?.Trim();
			if (string.IsNullOrEmpty(curParent) || string.Equals(curParent, prop.PropertyId, StringComparison.Ordinal))
			{
				continue;
			}

			// Si ce bien a été interrogé aujourd'hui et que Yanport confirme explicitement 0 relation,
			// on ne reprend pas son ancien lien (un autre voisin interrogé peut encore le relier à l'étape 3).
			bool explicitlyNoRelations = queriedIds != null
				&& queriedIds.Contains(prop.PropertyId)
				&& relatedById != null
				&& relatedById.TryGetValue(prop.PropertyId, out IReadOnlyList<string> freshRel)
				&& (freshRel == null || freshRel.Count == 0);
			if (explicitlyNoRelations)
			{
				continue;
			}

			if (byId.TryGetValue(curParent, out YanportDoublonSync.ActiveProperty parent))
			{
				if (YanportDoublonSync.SameTransaction(prop.TypeTransaction, parent.TypeTransaction)
					&& !IsRejected(prop.PropertyId, parent.PropertyId))
				{
					AddEdge(adj, prop.PropertyId, parent.PropertyId);
					AddEdge(adj, parent.PropertyId, prop.PropertyId);
				}
			}
			else
			{
				// Ancien maître fermé : on regroupe ses anciens doublons encore actifs pour promouvoir le suivant.
				if (!orphansByDeadMaster.TryGetValue(curParent, out List<YanportDoublonSync.ActiveProperty> siblings))
				{
					siblings = new List<YanportDoublonSync.ActiveProperty>();
					orphansByDeadMaster[curParent] = siblings;
				}
				siblings.Add(prop);
			}
		}

		// 2. Continuité des groupes dont le maître vient d'être fermé
		foreach (List<YanportDoublonSync.ActiveProperty> siblings in orphansByDeadMaster.Values)
		{
			if (siblings.Count < 2)
			{
				continue;
			}
			siblings.Sort(YanportDoublonSync.CompareMasterPriority);
			for (int i = 0; i < siblings.Count; i++)
			{
				for (int j = i + 1; j < siblings.Count; j++)
				{
					if (YanportDoublonSync.SameTransaction(siblings[i].TypeTransaction, siblings[j].TypeTransaction)
						&& !IsRejected(siblings[i].PropertyId, siblings[j].PropertyId))
					{
						AddEdge(adj, siblings[i].PropertyId, siblings[j].PropertyId);
						AddEdge(adj, siblings[j].PropertyId, siblings[i].PropertyId);
					}
				}
			}
		}

		// 3. Arêtes fraîches renvoyées par Yanport sur les cibles du jour
		if (relatedById != null)
		{
			foreach (KeyValuePair<string, IReadOnlyList<string>> kv in relatedById)
			{
				if (!byId.TryGetValue(kv.Key, out YanportDoublonSync.ActiveProperty src) || kv.Value == null)
				{
					continue;
				}
				foreach (string rawOther in kv.Value)
				{
					string otherId = rawOther?.Trim();
					if (string.IsNullOrEmpty(otherId) || string.Equals(otherId, src.PropertyId, StringComparison.Ordinal))
					{
						continue;
					}
					if (!byId.TryGetValue(otherId, out YanportDoublonSync.ActiveProperty dst))
					{
						continue;
					}
					if (!YanportDoublonSync.SameTransaction(src.TypeTransaction, dst.TypeTransaction))
					{
						continue;
					}
					if (IsRejected(src.PropertyId, dst.PropertyId))
					{
						continue;
					}
					AddEdge(adj, src.PropertyId, dst.PropertyId);
					AddEdge(adj, dst.PropertyId, src.PropertyId);
				}
			}
		}

		// 4. Composantes connexes + élection déterministe du maître
		Dictionary<string, string> desiredDoublon = new Dictionary<string, string>(StringComparer.Ordinal);
		HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);

		foreach (string startId in adj.Keys.OrderBy((string k) => k, StringComparer.Ordinal))
		{
			if (!visited.Add(startId))
			{
				continue;
			}
			List<YanportDoublonSync.ActiveProperty> component = new List<YanportDoublonSync.ActiveProperty>();
			Queue<string> queue = new Queue<string>();
			queue.Enqueue(startId);

			while (queue.Count > 0)
			{
				string current = queue.Dequeue();
				component.Add(byId[current]);
				if (!adj.TryGetValue(current, out HashSet<string> neighbors))
				{
					continue;
				}
				foreach (string next in neighbors)
				{
					if (visited.Add(next))
					{
						queue.Enqueue(next);
					}
				}
			}

			if (component.Count < 2)
			{
				continue;
			}

			component.Sort(YanportDoublonSync.CompareMasterPriority);
			string masterId = component[0].PropertyId;
			plan.ClustersCount++;
			plan.TotalDuplicatesInClusters += component.Count - 1;

			desiredDoublon[masterId] = null;
			for (int i = 1; i < component.Count; i++)
			{
				desiredDoublon[component[i].PropertyId] = masterId;
			}
		}

		// 5. Diff minimal
		foreach (YanportDoublonSync.ActiveProperty prop in byId.Values.OrderBy((YanportDoublonSync.ActiveProperty p) => p.PropertyId, StringComparer.Ordinal))
		{
			desiredDoublon.TryGetValue(prop.PropertyId, out string target);
			string current = string.IsNullOrWhiteSpace(prop.CurrentDoublonPropertyId)
				? null
				: prop.CurrentDoublonPropertyId.Trim();

			if (string.Equals(current, target, StringComparison.Ordinal))
			{
				plan.UnchangedCount++;
			}
			else if (target != null)
			{
				plan.ToLink.Add(new YanportDoublonSync.DoublonUpdate(prop.PropertyId, target));
			}
			else
			{
				plan.ToClear.Add(new YanportDoublonSync.DoublonUpdate(prop.PropertyId, null));
			}
		}

		return plan;
	}

	internal static bool ShouldAbortDoublonUpdates(int totalUpdates)
	{
		return totalUpdates > MaxDoublonUpdatesAbort;
	}

	private static void AddEdge(Dictionary<string, HashSet<string>> adj, string from, string to)
	{
		if (!adj.TryGetValue(from, out HashSet<string> set))
		{
			set = new HashSet<string>(StringComparer.Ordinal);
			adj[from] = set;
		}
		set.Add(to);
	}
}
