using System;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.RecupInfos;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace ApiPerleRare.Tests;

public class RecupInfosCancellationTests
{
	[Fact]
	public void Volume_counts_follow_request_aborted()
	{
		using CancellationTokenSource cts = new CancellationTokenSource();
		CancellationToken token = RecupInfoSearcher.CountsCancellation(new Filter { Apply = false }, cts.Token);
		cts.Cancel();
		Assert.True(token.IsCancellationRequested);
	}

	[Fact]
	public void Visualiser_apply_is_never_cancelled_by_the_browser()
	{
		using CancellationTokenSource cts = new CancellationTokenSource();
		CancellationToken token = RecupInfoSearcher.CountsCancellation(new Filter { Apply = true }, cts.Token);
		cts.Cancel();
		Assert.False(token.CanBeCanceled);
	}

	[Fact]
	public async Task Cancelled_facet_query_is_not_cached()
	{
		using MemoryCache cache = new MemoryCache(new MemoryCacheOptions());
		await Assert.ThrowsAnyAsync<Exception>(() => cache.GetOrCreateAsync<int[]>("SELECT 1", _ =>
			throw new OperationCanceledException()));
		Assert.False(cache.TryGetValue("SELECT 1", out _));
		int[] counts = await cache.GetOrCreateAsync("SELECT 1", _ => Task.FromResult(new[] { 42 }));
		Assert.Equal(42, counts[0]);
	}
}
