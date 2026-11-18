using System;
using System.Threading;
using ApiPerleRare.Application.BiensNonVus;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace ApiPerleRare.Tests;

public class BiensNonVusCountTests
{
	[Fact]
	public void Sql_looks_up_each_conseiller_column_and_keeps_the_property_join()
	{
		string sql = BiensNonVusCount.Sql;
		Assert.Equal(2, CountOf(sql, "INNER JOIN property p ON p.P_PropertyId = pc.PC_PropertyId"));
		Assert.Equal(2, CountOf(sql, "pc.PC_Actif = 1 AND pc.PC_Vu <> 1"));
		Assert.Contains("cr.C_NomFamilleConseiller = @conseillerid", sql);
		Assert.Contains("cr.C_2emeConseiller = @conseillerid2", sql);
		Assert.Contains("UNION", sql);
		Assert.DoesNotContain(" OR ", sql);
	}

	[Fact]
	public void Cache_serves_the_same_login_and_isolates_another()
	{
		using MemoryCache cache = new MemoryCache(new MemoryCacheOptions());
		int calls = 0;
		long Next()
		{
			calls++;
			return calls;
		}
		Assert.Equal(1L, BiensNonVusCount.GetOrCompute(cache, "alice", Next));
		Assert.Equal(1L, BiensNonVusCount.GetOrCompute(cache, "alice", Next));
		Assert.Equal(2L, BiensNonVusCount.GetOrCompute(cache, "bob", Next));
		Assert.Equal(2, calls);
	}

	[Fact]
	public void Failed_count_is_not_cached()
	{
		using MemoryCache cache = new MemoryCache(new MemoryCacheOptions());
		Assert.Throws<InvalidOperationException>(() =>
			BiensNonVusCount.GetOrCompute(cache, "alice", () => throw new InvalidOperationException("boom")));
		Assert.Equal(4L, BiensNonVusCount.GetOrCompute(cache, "alice", () => 4L));
	}

	[Fact]
	public void Expired_count_is_recomputed()
	{
		using MemoryCache cache = new MemoryCache(new MemoryCacheOptions());
		Assert.Equal(1L, BiensNonVusCount.GetOrCompute(cache, "alice", () => 1L, TimeSpan.FromMilliseconds(20)));
		Thread.Sleep(80);
		Assert.Equal(2L, BiensNonVusCount.GetOrCompute(cache, "alice", () => 2L, TimeSpan.FromMilliseconds(20)));
	}

	[Fact]
	public void Cache_ttl_covers_one_topbar_poll()
	{
		Assert.InRange(BiensNonVusCount.CacheTtl.TotalSeconds, 30, 60);
	}

	[Fact]
	public void ReadCount_accepts_the_scalar_types_the_driver_returns()
	{
		Assert.Equal(12L, BiensNonVusCount.ReadCount(12L));
		Assert.Equal(7L, BiensNonVusCount.ReadCount(7));
		Assert.Equal(-1L, BiensNonVusCount.ReadCount(null));
		Assert.Equal(-1L, BiensNonVusCount.ReadCount(DBNull.Value));
		Assert.Throws<Exception>(() => BiensNonVusCount.ReadCount("12"));
	}

	private static int CountOf(string text, string fragment)
	{
		int count = 0;
		int from = 0;
		while (true)
		{
			int at = text.IndexOf(fragment, from, StringComparison.Ordinal);
			if (at < 0)
			{
				return count;
			}
			count++;
			from = at + fragment.Length;
		}
	}
}
