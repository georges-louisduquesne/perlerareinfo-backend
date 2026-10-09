using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.YanportModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ApiPerleRare.Tests;

public class AgencyYanportLinkTests
{
	[Theory]
	[InlineData(0, 0, false)]
	[InlineData(0, 5477959295355904L, true)]
	[InlineData(5477959295355904L, 5477959295355904L, false)]
	[InlineData(5477959295355904L, 0, true)]
	[InlineData(11, 22, true)]
	public void Runs_only_when_the_yanport_id_is_set_or_changed(long previous, long next, bool expected)
	{
		Assert.Equal(expected, AgencyYanportLink.ShouldRun(previous, next));
	}

	[Fact]
	public void Matches_the_dealer_id_without_catching_a_longer_one()
	{
		string json = "[{\"Id\":5477959295355904,\"Name\":\"Breteuil\"},{\"Id\":12}]";
		Assert.True(AgencyYanportLink.AnnouncersContainDealerId(json, 5477959295355904L));
		Assert.True(AgencyYanportLink.AnnouncersContainDealerId(json, 12));
		Assert.False(AgencyYanportLink.AnnouncersContainDealerId(json, 547795929535590L));
		Assert.False(AgencyYanportLink.AnnouncersContainDealerId(json, 1));
		Assert.False(AgencyYanportLink.AnnouncersContainDealerId("[{\"Id\": 12}]", 123));
		Assert.True(AgencyYanportLink.AnnouncersContainDealerId("[{\"Id\": 12}]", 12));
		Assert.False(AgencyYanportLink.AnnouncersContainDealerId(null, 12));
		Assert.False(AgencyYanportLink.AnnouncersContainDealerId(json, 0));
	}

	[Fact]
	public void Sql_only_rewrites_the_field_row_and_published_links()
	{
		Assert.Equal(5, AgencyYanportLink.SaisieSource.Length);
		Assert.Contains("I_Source IN ('saisi','init')", AgencyYanportLink.DeleteFieldRowSql);
		Assert.DoesNotContain("email", AgencyYanportLink.DeleteFieldRowSql, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("tel", AgencyYanportLink.DeleteFieldRowSql, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("INSERT IGNORE INTO intermediaires_directs_yanport", AgencyYanportLink.InsertFieldRowSql);
		Assert.Contains("p.P_State=1", AgencyYanportLink.InsertPublishedLinksSql);
		Assert.Contains("p.P_State=1", AgencyYanportLink.DeletePublishedLinksSql);
		Assert.Contains("@agency", AgencyYanportLink.InsertPublishedLinksSql);
		Assert.Contains("@idComma", AgencyYanportLink.InsertPublishedLinksSql);
		string[] sql = {
			AgencyYanportLink.DeleteFieldRowSql,
			AgencyYanportLink.InsertFieldRowSql,
			AgencyYanportLink.InsertPublishedLinksSql,
			AgencyYanportLink.DeletePublishedLinksSql
		};
		foreach (string statement in sql)
		{
			Assert.DoesNotContain("DROP", statement, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("TRUNCATE", statement, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain(";", statement);
		}
	}

	[Fact]
	public void Enqueue_skips_an_unchanged_id()
	{
		RecordingQueue queue = new RecordingQueue();
		AgencyYanportLinkJob job = new AgencyYanportLinkJob(queue, new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(), NullLogger<AgencyYanportLinkJob>.Instance);
		job.Enqueue(6236, 5477959295355904L, 5477959295355904L);
		job.Enqueue(0, 0, 99);
		Assert.Empty(queue.Items);
		job.Enqueue(6236, 0, 5477959295355904L);
		Assert.Single(queue.Items);
	}

	private sealed class RecordingQueue : IBackgroundTaskQueue
	{
		public List<Func<CancellationToken, Task>> Items { get; } = new List<Func<CancellationToken, Task>>();

		public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
		{
			Items.Add(workItem);
		}

		public Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}
	}
}
