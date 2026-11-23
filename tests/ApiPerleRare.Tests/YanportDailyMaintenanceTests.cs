using System;
using System.Collections.Generic;
using System.Linq;
using ApiPerleRare.ScheduledTasks;
using Xunit;

namespace ApiPerleRare.Tests;

public class YanportDailyMaintenanceTests
{
	[Theory]
	[InlineData(null, false)]
	[InlineData("", false)]
	[InlineData("off", false)]
	[InlineData("1", false)]
	[InlineData("on", true)]
	[InlineData("ON", true)]
	[InlineData(" on ", true)]
	public void Enabled_requires_explicit_on(string value, bool expected)
	{
		Assert.Equal(expected, YanportDailyMaintenance.Enabled(value));
	}

	[Fact]
	public void Due_waits_for_04h_paris_and_runs_once_per_day()
	{
		// 2026-11-22 02:59 UTC = 03:59 Paris (CET +1) -> trop tôt
		Assert.False(YanportDailyMaintenance.Due(null, new DateTime(2026, 11, 22, 2, 59, 0, DateTimeKind.Utc)));
		// 2026-11-22 03:00 UTC = 04:00 Paris -> dû
		Assert.True(YanportDailyMaintenance.Due(null, new DateTime(2026, 11, 22, 3, 0, 0, DateTimeKind.Utc)));
		// Déjà tourné aujourd'hui -> pas dû
		Assert.False(YanportDailyMaintenance.Due(20261122, new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc)));
		// Lendemain 04:00 Paris -> dû
		Assert.True(YanportDailyMaintenance.Due(20261122, new DateTime(2026, 11, 23, 3, 0, 0, DateTimeKind.Utc)));
	}

	[Fact]
	public void SelectTargetsForDay_picks_recent_creations_and_1_of_14_stale_slice_only()
	{
		DateTime now = new DateTime(2026, 11, 22, 4, 0, 0);
		List<YanportDailyMaintenance.ActiveRow> actives = new List<YanportDailyMaintenance.ActiveRow>
		{
			// Nouveau bien (< 36h) : doit toujours être sélectionné
			new("new-1", "SALE", now.AddHours(-5), now.AddHours(-5), now.AddHours(-1), null),
			// Bien frais (MAJ il y a 3 jours, créé il y a 10 jours) : ne doit JAMAIS être interrogé
			new("fresh-1", "SALE", now.AddDays(-10), now.AddDays(-10), now.AddDays(-3), null),
		};

		// 140 biens anciens (MAJ > 14j)
		List<string> staleIds = new List<string>();
		for (int i = 0; i < 140; i++)
		{
			string id = $"stale-{i:D4}";
			staleIds.Add(id);
			actives.Add(new YanportDailyMaintenance.ActiveRow(id, "SALE", now.AddDays(-60), now.AddDays(-60), now.AddDays(-20), null));
		}

		// Sur 14 jours consécutifs, chaque bien ancien doit être sélectionné exactement 1 fois,
		// le bien frais 0 fois, et le nouveau bien tant qu'il reste dans la fenêtre 36h.
		Dictionary<string, int> staleHits = staleIds.ToDictionary((string id) => id, _ => 0);
		for (int day = 0; day < YanportDailyMaintenance.RotationDays; day++)
		{
			DateTime dayNow = now.AddDays(day);
			YanportDailyMaintenance.TargetSelection sel = YanportDailyMaintenance.SelectTargetsForDay(actives, dayNow);
			Assert.DoesNotContain(sel.Targets, (YanportDailyMaintenance.ActiveRow r) => r.PropertyId == "fresh-1");
			if (day == 0)
			{
				Assert.Contains(sel.Targets, (YanportDailyMaintenance.ActiveRow r) => r.PropertyId == "new-1");
				Assert.Equal(1, sel.NewCount);
			}
			foreach (YanportDailyMaintenance.ActiveRow r in sel.Targets)
			{
				if (staleHits.ContainsKey(r.PropertyId))
				{
					staleHits[r.PropertyId]++;
				}
			}
		}

		Assert.All(staleHits.Values, (int count) => Assert.Equal(1, count));
	}

	[Fact]
	public void SelectTargetsForDay_prioritizes_new_properties_under_cap()
	{
		DateTime now = new DateTime(2026, 11, 22, 4, 0, 0);
		int slot = YanportDailyMaintenance.RotationSlotForParisDate(now);

		// Trouver 3 IDs qui tombent dans le slot du jour
		List<string> matchingStale = Enumerable.Range(0, 500)
			.Select((int i) => $"s-{i}")
			.Where((string id) => YanportDailyMaintenance.StableBucket(id, YanportDailyMaintenance.RotationDays) == slot)
			.Take(3)
			.ToList();

		List<YanportDailyMaintenance.ActiveRow> actives = new List<YanportDailyMaintenance.ActiveRow>
		{
			new("new-a", "SALE", now.AddHours(-2), now.AddHours(-2), now.AddHours(-1), null),
			new("new-b", "SALE", now.AddHours(-3), now.AddHours(-3), now.AddHours(-1), null),
		};
		foreach (string id in matchingStale)
		{
			actives.Add(new YanportDailyMaintenance.ActiveRow(id, "SALE", now.AddDays(-30), now.AddDays(-30), now.AddDays(-20), null));
		}

		YanportDailyMaintenance.TargetSelection sel = YanportDailyMaintenance.SelectTargetsForDay(actives, now, maxTargets: 3);

		Assert.Equal(3, sel.Targets.Count);
		Assert.Equal(2, sel.NewCount);
		Assert.Equal(1, sel.StaleSliceCount);
		Assert.Equal(2, sel.CappedCount);
		Assert.Contains(sel.Targets, (YanportDailyMaintenance.ActiveRow r) => r.PropertyId == "new-a");
		Assert.Contains(sel.Targets, (YanportDailyMaintenance.ActiveRow r) => r.PropertyId == "new-b");
	}

	[Fact]
	public void ComputeIncrementalDoublonPlan_preserves_unqueried_groups_and_links_new_duplicate_transitively()
	{
		DateTime d1 = new DateTime(2026, 9, 1);
		DateTime d2 = new DateTime(2026, 10, 1);
		DateTime d3 = new DateTime(2026, 11, 22);

		List<YanportDailyMaintenance.ActiveRow> actives = new List<YanportDailyMaintenance.ActiveRow>
		{
			// Groupe existant non interrogé : {m1, d1}
			new("m1", "SALE", d1, d1, d2, null),
			new("d1", "SALE", d2, d2, d2, "m1"),
			// Autre groupe existant non interrogé : {m2, d2}
			new("m2", "SALE", d1, d1, d2, null),
			new("d2", "SALE", d2, d2, d2, "m2"),
			// Nouveau bien interrogé aujourd'hui : Yanport dit qu'il est lié à d1 (pas directement à m1)
			new("newDup", "SALE", d3, d3, d3, null),
		};

		HashSet<string> queried = new HashSet<string>(StringComparer.Ordinal) { "newDup" };
		Dictionary<string, IReadOnlyList<string>> related = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
		{
			["newDup"] = new[] { "d1" }
		};

		YanportDoublonSync.SyncPlan plan = YanportDailyMaintenance.ComputeIncrementalDoublonPlan(
			actives,
			closedIds: new HashSet<string>(StringComparer.Ordinal),
			queriedIds: queried,
			relatedById: related);

		Assert.Equal(2, plan.ClustersCount);
		Assert.Empty(plan.ToClear);
		// Seul newDup doit être mis à jour -> pointé directement sur le maître m1 par transitivité !
		YanportDoublonSync.DoublonUpdate upd = Assert.Single(plan.ToLink);
		Assert.Equal("newDup", upd.PropertyId);
		Assert.Equal("m1", upd.TargetDoublonPropertyId);
	}

	[Fact]
	public void ComputeIncrementalDoublonPlan_promotes_next_master_when_master_is_closed_and_clears_lone_survivor()
	{
		DateTime d1 = new DateTime(2026, 9, 1);
		DateTime d2 = new DateTime(2026, 10, 1);
		DateTime d3 = new DateTime(2026, 10, 15);

		List<YanportDailyMaintenance.ActiveRow> actives = new List<YanportDailyMaintenance.ActiveRow>
		{
			// Groupe 1 : maître m1 fermé aujourd'hui par Lot 1, 2 doublons actifs d1a et d1b restent
			new("m1", "SALE", d1, d1, d1, null),
			new("d1a", "SALE", d2, d2, d2, "m1"),
			new("d1b", "SALE", d3, d3, d3, "m1"),
			// Groupe 2 : ancien maître m2 déjà fermé hors base, 1 seul survivant d2a reste
			new("d2a", "SALE", d2, d2, d2, "m2-gone"),
		};

		HashSet<string> closedIds = new HashSet<string>(StringComparer.Ordinal) { "m1" };

		YanportDoublonSync.SyncPlan plan = YanportDailyMaintenance.ComputeIncrementalDoublonPlan(
			actives,
			closedIds: closedIds,
			queriedIds: new HashSet<string>(StringComparer.Ordinal),
			relatedById: new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal));

		// d1a devient le nouveau maître (ToClear), d1b pointe sur d1a (ToLink), d2a est délié (ToClear)
		Assert.Equal(1, plan.ClustersCount);
		YanportDoublonSync.DoublonUpdate relink = Assert.Single(plan.ToLink);
		Assert.Equal("d1b", relink.PropertyId);
		Assert.Equal("d1a", relink.TargetDoublonPropertyId);

		Assert.Equal(2, plan.ToClear.Count);
		Assert.Contains(plan.ToClear, (YanportDoublonSync.DoublonUpdate u) => u.PropertyId == "d1a" && u.TargetDoublonPropertyId == null);
		Assert.Contains(plan.ToClear, (YanportDoublonSync.DoublonUpdate u) => u.PropertyId == "d2a" && u.TargetDoublonPropertyId == null);
	}

	[Fact]
	public void ComputeIncrementalDoublonPlan_unlinks_queried_property_when_yanport_returns_empty_relations()
	{
		DateTime d1 = new DateTime(2026, 9, 1);
		DateTime d2 = new DateTime(2026, 10, 1);

		List<YanportDailyMaintenance.ActiveRow> actives = new List<YanportDailyMaintenance.ActiveRow>
		{
			new("m1", "SALE", d1, d1, d1, null),
			new("d1", "SALE", d2, d2, d2, "m1"),
		};

		HashSet<string> queried = new HashSet<string>(StringComparer.Ordinal) { "d1" };
		Dictionary<string, IReadOnlyList<string>> related = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
		{
			["d1"] = Array.Empty<string>()
		};

		YanportDoublonSync.SyncPlan plan = YanportDailyMaintenance.ComputeIncrementalDoublonPlan(
			actives,
			closedIds: new HashSet<string>(StringComparer.Ordinal),
			queriedIds: queried,
			relatedById: related);

		Assert.Equal(0, plan.ClustersCount);
		Assert.Empty(plan.ToLink);
		YanportDoublonSync.DoublonUpdate clear = Assert.Single(plan.ToClear);
		Assert.Equal("d1", clear.PropertyId);
		Assert.Null(clear.TargetDoublonPropertyId);
	}

	[Fact]
	public void ComputeIncrementalDoublonPlan_never_relinks_admin_rejected_pairs()
	{
		DateTime d1 = new DateTime(2026, 9, 1);
		DateTime d2 = new DateTime(2026, 10, 1);

		List<YanportDailyMaintenance.ActiveRow> actives = new List<YanportDailyMaintenance.ActiveRow>
		{
			new("lotA", "SALE", d1, d1, d1, null),
			new("lotB", "SALE", d2, d2, d2, null),
			new("lotC", "SALE", d2, d2, d2, "lotA"),
		};

		HashSet<string> queried = new HashSet<string>(StringComparer.Ordinal) { "lotB" };
		Dictionary<string, IReadOnlyList<string>> related = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
		{
			["lotB"] = new[] { "lotA" }
		};
		HashSet<string> rejected = new HashSet<string>(StringComparer.Ordinal)
		{
			ApiPerleRare.Application.Doublons.DoublonReviewLogic.CanonicalPairKey("lotA", "lotB"),
			ApiPerleRare.Application.Doublons.DoublonReviewLogic.CanonicalPairKey("lotA", "lotC"),
		};

		YanportDoublonSync.SyncPlan plan = YanportDailyMaintenance.ComputeIncrementalDoublonPlan(
			actives,
			closedIds: new HashSet<string>(StringComparer.Ordinal),
			queriedIds: queried,
			relatedById: related,
			rejectedPairKeys: rejected);

		Assert.Equal(0, plan.ClustersCount);
		Assert.Empty(plan.ToLink);
		YanportDoublonSync.DoublonUpdate clear = Assert.Single(plan.ToClear);
		Assert.Equal("lotC", clear.PropertyId);
		Assert.Null(clear.TargetDoublonPropertyId);
	}

	[Fact]
	public void Sql_and_guards_are_safe()
	{
		Assert.Contains("IFNULL(P_EstRattrapage, 0) <> 4", YanportDailyMaintenance.SelectActiveSql);
		Assert.Contains("P_State = 1", YanportDailyMaintenance.SelectActiveSql);
		Assert.False(YanportDailyMaintenance.ShouldAbortDoublonUpdates(5000));
		Assert.True(YanportDailyMaintenance.ShouldAbortDoublonUpdates(5001));
	}
}
