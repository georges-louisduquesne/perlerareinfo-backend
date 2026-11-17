using System;
using ApiPerleRare.Application.Monitoring;
using ApiPerleRare.ScheduledTasks;
using Xunit;

namespace ApiPerleRare.Tests;

public class RecrawlStockMailTests
{
	[Theory]
	[InlineData(null, false)]
	[InlineData("", false)]
	[InlineData("off", false)]
	[InlineData("on", true)]
	[InlineData(" ON ", true)]
	public void Job_runs_only_when_explicitly_on(string value, bool expected)
	{
		Assert.Equal(expected, RecrawlStockMail.Enabled(value));
	}

	[Fact]
	public void Sql_counts_open_properties_over_90_days_and_between_30_and_90()
	{
		Assert.Contains("P_State = 1", RecrawlStockQuery.Sql, StringComparison.Ordinal);
		Assert.Contains("P_DateFin IS NULL OR P_DateFin < '1000-01-01'", RecrawlStockQuery.Sql, StringComparison.Ordinal);
		Assert.Contains("INTERVAL 90 DAY", RecrawlStockQuery.Sql, StringComparison.Ordinal);
		Assert.Contains("INTERVAL 30 DAY", RecrawlStockQuery.Sql, StringComparison.Ordinal);
		Assert.Contains("FROM property", RecrawlStockQuery.Sql, StringComparison.Ordinal);
		Assert.Contains("P_EstRattrapage, 0) <> 4", RecrawlStockQuery.Sql, StringComparison.Ordinal);
	}

	[Fact]
	public void Mail_is_due_once_per_paris_month_after_8()
	{
		DateTime beforeEight = new DateTime(2026, 11, 1, 6, 30, 0, DateTimeKind.Utc);
		Assert.False(RecrawlStockMail.Due(null, beforeEight));
		DateTime morning = new DateTime(2026, 11, 17, 7, 30, 0, DateTimeKind.Utc);
		Assert.True(RecrawlStockMail.Due(null, morning));
		Assert.Equal(202611, RecrawlStockMail.MonthKey(morning));
		Assert.False(RecrawlStockMail.Due(202611, morning));
		DateTime nextMonth = new DateTime(2026, 12, 1, 7, 30, 0, DateTimeKind.Utc);
		Assert.True(RecrawlStockMail.Due(202611, nextMonth));
		Assert.Equal(202612, RecrawlStockMail.MonthKey(nextMonth));
	}

	[Fact]
	public void Body_carries_both_volumes_and_the_cursor_prompts()
	{
		string body = RecrawlStockMail.Body(879, 7868);
		Assert.Contains("Plus de 90 jours : 879", body, StringComparison.Ordinal);
		Assert.Contains("30 à 90 jours : 7868", body, StringComparison.Ordinal);
		Assert.Contains("Playwright DataDome", body, StringComparison.Ordinal);
		Assert.Contains("Ne lance pas Playwright", body, StringComparison.Ordinal);
		Assert.Contains("apify/website-content-crawler", body, StringComparison.Ordinal);
		Assert.Contains("ne ferme aucun bien", body, StringComparison.Ordinal);
		Assert.StartsWith("Biens actifs", body, StringComparison.Ordinal);
		string html = RecrawlStockMail.ToHtml("a < b\nc");
		Assert.Contains("a &lt; b<br>c", html, StringComparison.Ordinal);
	}
}
