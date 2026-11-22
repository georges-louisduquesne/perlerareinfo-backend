using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ApiPerleRare.ScheduledTasks;

/// <summary>
/// Lot 2 — Synchronisation quotidienne des liens de doublons multi-agences (<c>property.P_DoublonPropertyId</c>)
/// à partir du graphe <c>hit.properties[]</c> renvoyé par l'API Yanport.
/// Ne ferme aucun bien (<c>P_State</c>, <c>P_DateFin</c>, <c>P_EstRattrapage</c> et <c>P_DateLastUpdate</c> intacts).
/// </summary>
internal static class YanportDoublonSync
{
	internal const string EnvVar = "PR_YANPORT_DOUBLON_SYNC";

	internal const string CursorParam = "YanportDoublonSyncDay";

	internal const string TaskLogName = "YanportDoublonSync";

	internal const int RunHourParis = 5;

	internal const int BatchSize = 150;

	internal const int AnomalyMinChecked = 500;

	internal const double MaxDuplicateRatioAbort = 0.50;

	internal const double MinBatchSuccessRatio = 0.95;

	internal const string SelectActiveSql =
		"SELECT P_PropertyId, P_TypeTransaction, P_DateDebut, P_DateCreation, P_DoublonPropertyId "
		+ "FROM property "
		+ "WHERE P_State = 1 "
		+ "AND (P_DateFin IS NULL OR P_DateFin < '1000-01-01') "
		+ "AND IFNULL(P_EstRattrapage, 0) <> 4";

	internal const string UpdateDoublonSql =
		"UPDATE property "
		+ "SET P_DoublonPropertyId = @doublonId "
		+ "WHERE P_PropertyId = @id "
		+ "  AND P_State = 1 "
		+ "  AND (P_DateFin IS NULL OR P_DateFin < '1000-01-01') "
		+ "  AND IFNULL(P_EstRattrapage, 0) <> 4";

	internal sealed record ActiveProperty(
		string PropertyId,
		string TypeTransaction,
		DateTime? DateDebut,
		DateTime? DateCreation,
		string CurrentDoublonPropertyId);

	internal sealed record DoublonUpdate(string PropertyId, string TargetDoublonPropertyId);

	internal sealed class SyncPlan
	{
		public int ClustersCount { get; set; }

		public int TotalDuplicatesInClusters { get; set; }

		public int UnchangedCount { get; set; }

		public List<DoublonUpdate> ToLink { get; } = new List<DoublonUpdate>();

		public List<DoublonUpdate> ToClear { get; } = new List<DoublonUpdate>();
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

	/// <summary>Une fois par jour calendaire de Paris, à partir de 5 h (après le Lot 1 à 4 h).</summary>
	internal static bool Due(int? lastDay, DateTime utc)
	{
		DateTime paris = ToParisTime(utc);
		if (paris.Hour < RunHourParis)
		{
			return false;
		}
		return lastDay != paris.Year * 10000 + paris.Month * 100 + paris.Day;
	}

	internal static string BuildBatchUrl(IReadOnlyList<string> propertyIds)
	{
		return YanportGhostCleanup.BuildBatchUrl(propertyIds);
	}

	/// <summary>
	/// Parse la réponse JSON de <c>GET /properties?size=…&amp;ids=…</c> et extrait pour chaque hit
	/// les identifiants liés dans <c>properties[]</c> (source=AD ou chaîne UUID).
	/// Retourne false si la réponse est vide, sentinelle ou malformée.
	/// </summary>
	internal static bool TryParseBatchRelatedIds(string json, out Dictionary<string, IReadOnlyList<string>> relatedById)
	{
		relatedById = null;
		if (string.IsNullOrWhiteSpace(json))
		{
			return false;
		}
		string trimmed = json.Trim();
		if (trimmed == "NotFound" || trimmed == "Error500" || !trimmed.StartsWith("{", StringComparison.Ordinal))
		{
			return false;
		}

		try
		{
			using JsonDocument doc = JsonDocument.Parse(trimmed);
			if (doc.RootElement.ValueKind != JsonValueKind.Object)
			{
				return false;
			}
			if (!doc.RootElement.TryGetProperty("hits", out JsonElement hitsEl) || hitsEl.ValueKind != JsonValueKind.Array)
			{
				return false;
			}

			Dictionary<string, IReadOnlyList<string>> result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
			foreach (JsonElement hit in hitsEl.EnumerateArray())
			{
				if (hit.ValueKind != JsonValueKind.Object)
				{
					continue;
				}
				if (!hit.TryGetProperty("id", out JsonElement idEl) || idEl.ValueKind != JsonValueKind.String)
				{
					continue;
				}
				string id = idEl.GetString()?.Trim();
				if (string.IsNullOrEmpty(id))
				{
					continue;
				}

				List<string> related = new List<string>();
				if (hit.TryGetProperty("properties", out JsonElement propsEl) && propsEl.ValueKind == JsonValueKind.Array)
				{
					foreach (JsonElement item in propsEl.EnumerateArray())
					{
						if (item.ValueKind == JsonValueKind.String)
						{
							string other = item.GetString()?.Trim();
							if (!string.IsNullOrEmpty(other) && !string.Equals(other, id, StringComparison.Ordinal))
							{
								related.Add(other);
							}
						}
						else if (item.ValueKind == JsonValueKind.Object)
						{
							if (item.TryGetProperty("source", out JsonElement srcEl) && srcEl.ValueKind == JsonValueKind.String)
							{
								string src = srcEl.GetString()?.Trim();
								if (!string.IsNullOrEmpty(src) && !string.Equals(src, "AD", StringComparison.OrdinalIgnoreCase))
								{
									continue;
								}
							}
							if (item.TryGetProperty("id", out JsonElement otherIdEl) && otherIdEl.ValueKind == JsonValueKind.String)
							{
								string other = otherIdEl.GetString()?.Trim();
								if (!string.IsNullOrEmpty(other) && !string.Equals(other, id, StringComparison.Ordinal))
								{
									related.Add(other);
								}
							}
						}
					}
				}
				result[id] = related;
			}

			relatedById = result;
			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}

	internal static bool SameTransaction(string a, string b)
	{
		string na = a?.Trim();
		string nb = b?.Trim();
		return !string.IsNullOrEmpty(na)
			&& !string.IsNullOrEmpty(nb)
			&& string.Equals(na, nb, StringComparison.OrdinalIgnoreCase);
	}

	internal static int CompareMasterPriority(ActiveProperty a, ActiveProperty b)
	{
		int c = CompareNullableValidDate(a.DateDebut, b.DateDebut);
		if (c != 0)
		{
			return c;
		}
		c = CompareNullableValidDate(a.DateCreation, b.DateCreation);
		if (c != 0)
		{
			return c;
		}
		return string.Compare(a.PropertyId, b.PropertyId, StringComparison.Ordinal);
	}

	private static int CompareNullableValidDate(DateTime? a, DateTime? b)
	{
		bool aValid = a.HasValue && a.Value.Year >= 1000;
		bool bValid = b.HasValue && b.Value.Year >= 1000;
		if (aValid && bValid)
		{
			return DateTime.Compare(a.Value, b.Value);
		}
		if (aValid)
		{
			return -1;
		}
		if (bValid)
		{
			return 1;
		}
		return 0;
	}

	internal static SyncPlan ComputeSyncPlan(
		IReadOnlyList<ActiveProperty> actives,
		IReadOnlyDictionary<string, IReadOnlyList<string>> relatedById)
	{
		SyncPlan plan = new SyncPlan();
		if (actives == null || actives.Count == 0)
		{
			return plan;
		}

		Dictionary<string, ActiveProperty> byId = new Dictionary<string, ActiveProperty>(StringComparer.Ordinal);
		foreach (ActiveProperty prop in actives)
		{
			if (!string.IsNullOrWhiteSpace(prop?.PropertyId))
			{
				byId[prop.PropertyId.Trim()] = prop;
			}
		}

		Dictionary<string, HashSet<string>> adj = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
		if (relatedById != null)
		{
			foreach (KeyValuePair<string, IReadOnlyList<string>> kv in relatedById)
			{
				if (!byId.TryGetValue(kv.Key, out ActiveProperty src) || kv.Value == null)
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
					if (!byId.TryGetValue(otherId, out ActiveProperty dst))
					{
						continue;
					}
					if (!SameTransaction(src.TypeTransaction, dst.TypeTransaction))
					{
						continue;
					}
					AddEdge(adj, src.PropertyId, dst.PropertyId);
					AddEdge(adj, dst.PropertyId, src.PropertyId);
				}
			}
		}

		Dictionary<string, string> desiredDoublon = new Dictionary<string, string>(StringComparer.Ordinal);
		HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);

		foreach (string startId in adj.Keys.OrderBy((string k) => k, StringComparer.Ordinal))
		{
			if (!visited.Add(startId))
			{
				continue;
			}
			List<ActiveProperty> component = new List<ActiveProperty>();
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

			component.Sort(CompareMasterPriority);
			string masterId = component[0].PropertyId;
			plan.ClustersCount++;
			plan.TotalDuplicatesInClusters += component.Count - 1;

			desiredDoublon[masterId] = null;
			for (int i = 1; i < component.Count; i++)
			{
				desiredDoublon[component[i].PropertyId] = masterId;
			}
		}

		foreach (ActiveProperty prop in byId.Values.OrderBy((ActiveProperty p) => p.PropertyId, StringComparer.Ordinal))
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
				plan.ToLink.Add(new DoublonUpdate(prop.PropertyId, target));
			}
			else
			{
				plan.ToClear.Add(new DoublonUpdate(prop.PropertyId, null));
			}
		}

		return plan;
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

	internal static bool IsBatchSuccessAcceptable(int totalBatches, int failedBatches)
	{
		if (totalBatches <= 0)
		{
			return true;
		}
		int ok = Math.Max(0, totalBatches - failedBatches);
		return (double)ok / totalBatches >= MinBatchSuccessRatio;
	}

	internal static bool ShouldAbortForAnomaly(int totalChecked, int totalDuplicatesInClusters)
	{
		if (totalChecked < AnomalyMinChecked)
		{
			return false;
		}
		return (double)totalDuplicatesInClusters / totalChecked > MaxDuplicateRatioAbort;
	}
}
