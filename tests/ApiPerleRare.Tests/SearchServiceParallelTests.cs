using System;
using System.Collections.Generic;
using System.Linq;
using ApiPerleRare.Services;
using Xunit;

namespace ApiPerleRare.Tests;

public class SearchServiceParallelTests
{
	private sealed record FakeConfig(string Table, List<int> Stream);

	[Fact]
	public void Tiers_keep_config_order_and_group_consecutive_types()
	{
		var tiers = SearchService.ConsecutiveTiers(new[] { "E1", "E2", "S1", "S2", "C1", "C2", "C3" }, (string s) => s[0]);
		Assert.Equal("E1,E2|S1,S2|C1,C2,C3", string.Join("|", tiers.Select((List<string> t) => string.Join(",", t))));
	}

	[Fact]
	public void Only_exact_and_prefix_tiers_start_ahead()
	{
		Assert.True(SearchService.StartsAhead(SearchQueryType.Equal));
		Assert.True(SearchService.StartsAhead(SearchQueryType.StartsWith));
		Assert.False(SearchService.StartsAhead(SearchQueryType.Contains));
	}

	[Fact]
	public void Prefetch_skips_ids_already_taken_and_stops_at_need()
	{
		var rows = Prefetched(new[] { 9, 8, 7, 6 }, truncated: false);
		var ids = new HashSet<int> { 8 };
		var res = new List<SelectResult>();
		Assert.True(SearchService.TakeFromPrefetch(rows, ids, res, 2));
		Assert.Equal(new[] { 9, 7 }, res.Select((SelectResult r) => r.Id));
	}

	[Fact]
	public void Truncated_prefix_that_runs_out_asks_for_the_sequential_query()
	{
		var rows = Prefetched(new[] { 9, 9, 9 }, truncated: true);
		var res = new List<SelectResult>();
		Assert.False(SearchService.TakeFromPrefetch(rows, new HashSet<int>(), res, 5));
		Assert.Single(res);
	}

	[Fact]
	public void Parallel_prefetch_returns_exactly_the_sequential_results()
	{
		var random = new Random(20260928);
		for (int run = 0; run < 3000; run++)
		{
			int max = random.Next(1, 25);
			string[] tables = { "contacts", "agences", "biens" };
			var configs = Enumerable.Range(0, random.Next(1, 12)).Select((int _) =>
			{
				string table = tables[random.Next(tables.Length)];
				int count = random.Next(0, 60);
				int pool = random.Next(1, 40);
				var stream = Enumerable.Range(0, count).Select((int _) => random.Next(1, pool + 1)).ToList();
				if (random.Next(2) == 0)
				{
					stream = stream.Distinct().OrderByDescending((int x) => x).ToList();
				}
				return new FakeConfig(table, stream);
			}).ToList();

			Assert.Equal(Describe(Sequential(configs, max)), Describe(Parallel(configs, max)));
		}
	}

	private static List<SelectResult> Sequential(List<FakeConfig> configs, int max)
	{
		var store = new Dictionary<string, HashSet<int>>();
		var res = new List<SelectResult>();
		for (int i = 0; i < configs.Count && res.Count < max; i++)
		{
			res.AddRange(SequentialConfig(configs[i], i, Ids(store, configs[i].Table), max - res.Count));
		}
		return res;
	}

	/// <summary>Mirror of the original paging: LIMIT pageSize with NOT IN (ids taken so far).</summary>
	private static List<SelectResult> SequentialConfig(FakeConfig config, int index, HashSet<int> ids, int max)
	{
		var res = new List<SelectResult>();
		int pageSize = Math.Max(max, 15);
		bool again = true;
		while (again)
		{
			again = false;
			var page = config.Stream.Where((int id) => !ids.Contains(id)).Take(pageSize).ToList();
			int read = 0;
			foreach (int id in page)
			{
				read++;
				if (!ids.Contains(id))
				{
					res.Add(Row(index, id));
					ids.Add(id);
					if (res.Count == max)
					{
						break;
					}
				}
			}
			if (read == pageSize && res.Count < max)
			{
				again = true;
			}
		}
		return res;
	}

	private static List<SelectResult> Parallel(List<FakeConfig> configs, int max)
	{
		var store = new Dictionary<string, HashSet<int>>();
		var res = new List<SelectResult>();
		int limit = SearchService.PrefetchSize(max);
		var prefetched = configs.Select((FakeConfig c, int i) =>
		{
			var rows = new SearchService.PrefetchedRows { Truncated = c.Stream.Count >= limit };
			rows.Rows.AddRange(c.Stream.Take(limit).Select((int id) => (id, Row(i, id))));
			return rows;
		}).ToList();
		for (int i = 0; i < configs.Count && res.Count < max; i++)
		{
			var ids = Ids(store, configs[i].Table);
			if (!SearchService.TakeFromPrefetch(prefetched[i], ids, res, max - res.Count))
			{
				res.AddRange(SequentialConfig(configs[i], i, ids, max - res.Count));
			}
		}
		return res;
	}

	private static SearchService.PrefetchedRows Prefetched(int[] ids, bool truncated)
	{
		var rows = new SearchService.PrefetchedRows { Truncated = truncated };
		rows.Rows.AddRange(ids.Select((int id) => (id, Row(0, id))));
		return rows;
	}

	private static HashSet<int> Ids(Dictionary<string, HashSet<int>> store, string table)
	{
		if (!store.TryGetValue(table, out var ids))
		{
			ids = new HashSet<int>();
			store[table] = ids;
		}
		return ids;
	}

	private static SelectResult Row(int config, int id) => new SelectResult { CategoryId = "B" + config, Id = id };

	private static string Describe(List<SelectResult> rows) => string.Join(",", rows.Select((SelectResult r) => r.CategoryId + ":" + r.Id));
}
