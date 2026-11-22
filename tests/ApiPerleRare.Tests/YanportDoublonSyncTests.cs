using System;
using System.Collections.Generic;
using System.Linq;
using ApiPerleRare.ScheduledTasks;
using Xunit;

namespace ApiPerleRare.Tests;

public class YanportDoublonSyncTests
{
	[Theory]
	[InlineData("on", true)]
	[InlineData("ON", true)]
	[InlineData(" on ", true)]
	[InlineData("off", false)]
	[InlineData("", false)]
	[InlineData(null, false)]
	public void Enabled_OnlyMatchesOn(string raw, bool expected)
	{
		Assert.Equal(expected, YanportDoublonSync.Enabled(raw));
	}

	[Fact]
	public void Due_WaitsUntil5amParisAndRunsOncePerDay()
	{
		// 2026-11-22 03:59 UTC = 04:59 Paris (CET = UTC+1) -> trop tôt (5 h requis)
		DateTime beforeWindow = new DateTime(2026, 11, 22, 3, 59, 0, DateTimeKind.Utc);
		Assert.False(YanportDoublonSync.Due(null, beforeWindow));

		// 2026-11-22 04:00 UTC = 05:00 Paris -> dû
		DateTime atWindow = new DateTime(2026, 11, 22, 4, 0, 0, DateTimeKind.Utc);
		Assert.True(YanportDoublonSync.Due(null, atWindow));
		Assert.True(YanportDoublonSync.Due(20261121, atWindow));
		Assert.False(YanportDoublonSync.Due(20261122, atWindow));
	}

	[Fact]
	public void TryParseBatchRelatedIds_ExtractsAdObjectsAndPlainStrings()
	{
		string json = """
		{
		  "hits": [
		    {
		      "id": "p1",
		      "properties": [
		        { "id": "p2", "source": "AD" },
		        { "id": "p1", "source": "AD" },
		        { "id": "p3", "source": "OTHER" }
		      ]
		    },
		    {
		      "id": "p4",
		      "properties": ["p5"]
		    }
		  ]
		}
		""";

		Assert.True(YanportDoublonSync.TryParseBatchRelatedIds(json, out Dictionary<string, IReadOnlyList<string>> parsed));
		Assert.Equal(new[] { "p2" }, parsed["p1"]);
		Assert.Equal(new[] { "p5" }, parsed["p4"]);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("NotFound")]
	[InlineData("Error500")]
	[InlineData("{invalid")]
	[InlineData("{\"other\": []}")]
	public void TryParseBatchRelatedIds_RejectsInvalidResponses(string raw)
	{
		Assert.False(YanportDoublonSync.TryParseBatchRelatedIds(raw, out _));
	}

	[Fact]
	public void ComputeSyncPlan_GroupsConnectedComponentAndPicksOldestDateDebutAsMaster()
	{
		var actives = new[]
		{
			new YanportDoublonSync.ActiveProperty("b-newer", "A", new DateTime(2026, 10, 10), new DateTime(2026, 10, 10), null),
			new YanportDoublonSync.ActiveProperty("a-oldest", "A", new DateTime(2026, 9, 1), new DateTime(2026, 9, 2), null),
			new YanportDoublonSync.ActiveProperty("c-middle", "A", new DateTime(2026, 9, 15), new DateTime(2026, 9, 15), null),
			new YanportDoublonSync.ActiveProperty("standalone", "A", new DateTime(2026, 8, 1), new DateTime(2026, 8, 1), null),
		};

		// Graphe non symétrique en chaîne : b -> c, c -> a => un seul groupe {a, b, c}
		var related = new Dictionary<string, IReadOnlyList<string>>
		{
			["b-newer"] = new[] { "c-middle" },
			["c-middle"] = new[] { "a-oldest" },
		};

		YanportDoublonSync.SyncPlan plan = YanportDoublonSync.ComputeSyncPlan(actives, related);

		Assert.Equal(1, plan.ClustersCount);
		Assert.Equal(2, plan.TotalDuplicatesInClusters);
		Assert.Empty(plan.ToClear);
		Assert.Equal(2, plan.ToLink.Count);
		Assert.Contains(plan.ToLink, u => u.PropertyId == "b-newer" && u.TargetDoublonPropertyId == "a-oldest");
		Assert.Contains(plan.ToLink, u => u.PropertyId == "c-middle" && u.TargetDoublonPropertyId == "a-oldest");
		Assert.Equal(2, plan.UnchangedCount); // a-oldest (null) + standalone (null)
	}

	[Fact]
	public void ComputeSyncPlan_DoesNotCrossLinkDifferentTransactionTypes()
	{
		var actives = new[]
		{
			new YanportDoublonSync.ActiveProperty("sale-1", "A", new DateTime(2026, 9, 1), null, null),
			new YanportDoublonSync.ActiveProperty("rent-1", "L", new DateTime(2026, 9, 2), null, null),
		};

		var related = new Dictionary<string, IReadOnlyList<string>>
		{
			["sale-1"] = new[] { "rent-1" },
			["rent-1"] = new[] { "sale-1" },
		};

		YanportDoublonSync.SyncPlan plan = YanportDoublonSync.ComputeSyncPlan(actives, related);

		Assert.Equal(0, plan.ClustersCount);
		Assert.Empty(plan.ToLink);
		Assert.Empty(plan.ToClear);
	}

	[Fact]
	public void ComputeSyncPlan_ClearsStaleDoublonLinksAndIsIdempotent()
	{
		var actives = new[]
		{
			new YanportDoublonSync.ActiveProperty("master", "A", new DateTime(2026, 9, 1), null, "old-dead-master"),
			new YanportDoublonSync.ActiveProperty("dup", "A", new DateTime(2026, 9, 5), null, "master"),
			new YanportDoublonSync.ActiveProperty("orphan", "A", new DateTime(2026, 9, 2), null, "closed-sibling"),
		};

		var related = new Dictionary<string, IReadOnlyList<string>>
		{
			["master"] = new[] { "dup" },
		};

		YanportDoublonSync.SyncPlan plan = YanportDoublonSync.ComputeSyncPlan(actives, related);

		Assert.Equal(1, plan.ClustersCount);
		Assert.Empty(plan.ToLink); // dup pointe déjà vers master
		Assert.Equal(2, plan.ToClear.Count); // master et orphan remis à NULL
		Assert.Contains(plan.ToClear, u => u.PropertyId == "master" && u.TargetDoublonPropertyId == null);
		Assert.Contains(plan.ToClear, u => u.PropertyId == "orphan" && u.TargetDoublonPropertyId == null);
		Assert.Equal(1, plan.UnchangedCount);
	}

	[Fact]
	public void CompareMasterPriority_BreaksTiesDeterministicallyByCreationThenId()
	{
		var p1 = new YanportDoublonSync.ActiveProperty("id-b", "A", null, new DateTime(2026, 9, 1), null);
		var p2 = new YanportDoublonSync.ActiveProperty("id-a", "A", null, new DateTime(2026, 9, 1), null);
		Assert.True(YanportDoublonSync.CompareMasterPriority(p2, p1) < 0);

		var p3 = new YanportDoublonSync.ActiveProperty("id-z", "A", new DateTime(1, 1, 1), new DateTime(2026, 8, 1), null);
		var p4 = new YanportDoublonSync.ActiveProperty("id-a", "A", new DateTime(2026, 10, 1), new DateTime(2026, 10, 1), null);
		// DateDebut valide (p4) prime sur DateDebut sentinelle 0001-01-01 (p3)
		Assert.True(YanportDoublonSync.CompareMasterPriority(p4, p3) < 0);
	}

	[Fact]
	public void ShouldAbortForAnomaly_ProtectsAgainstRunawayLinking()
	{
		Assert.False(YanportDoublonSync.ShouldAbortForAnomaly(100, 90));
		Assert.False(YanportDoublonSync.ShouldAbortForAnomaly(47000, 10530)); // ~22.4 % normal
		Assert.True(YanportDoublonSync.ShouldAbortForAnomaly(47000, 25000));  // ~53.2 % anormal
	}

	[Fact]
	public void SqlGuards_NeverClosePropertiesOrTouchRattrapage4OrDateLastUpdate()
	{
		Assert.Contains("IFNULL(P_EstRattrapage, 0) <> 4", YanportDoublonSync.SelectActiveSql);
		Assert.Contains("IFNULL(P_EstRattrapage, 0) <> 4", YanportDoublonSync.UpdateDoublonSql);
		Assert.Contains("SET P_DoublonPropertyId = @doublonId", YanportDoublonSync.UpdateDoublonSql);
		Assert.DoesNotContain("P_State = 0", YanportDoublonSync.UpdateDoublonSql);
		Assert.DoesNotContain("P_DateFin =", YanportDoublonSync.UpdateDoublonSql);
		Assert.DoesNotContain("P_DateLastUpdate", YanportDoublonSync.UpdateDoublonSql);
		Assert.DoesNotContain("DELETE", YanportDoublonSync.UpdateDoublonSql, StringComparison.OrdinalIgnoreCase);
	}
}
