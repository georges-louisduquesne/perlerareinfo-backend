using System;
using System.Collections.Generic;
using ApiPerleRare.ScheduledTasks;
using Xunit;

namespace ApiPerleRare.Tests;

public class YanportGhostCleanupTests
{
	[Theory]
	[InlineData(null, false)]
	[InlineData("", false)]
	[InlineData("off", false)]
	[InlineData("false", false)]
	[InlineData("on", true)]
	[InlineData(" ON ", true)]
	public void Job_runs_only_when_explicitly_on(string value, bool expected)
	{
		Assert.Equal(expected, YanportGhostCleanup.Enabled(value));
	}

	[Fact]
	public void Job_is_due_once_per_paris_day_after_4am()
	{
		// 2026-11-22 is CET (UTC+1): 02:30 UTC = 03:30 Paris -> too early
		DateTime beforeFour = new DateTime(2026, 11, 22, 2, 30, 0, DateTimeKind.Utc);
		Assert.False(YanportGhostCleanup.Due(null, beforeFour));

		// 03:05 UTC = 04:05 Paris -> due
		DateTime afterFour = new DateTime(2026, 11, 22, 3, 5, 0, DateTimeKind.Utc);
		Assert.True(YanportGhostCleanup.Due(null, afterFour));
		Assert.Equal(20261122, YanportGhostCleanup.DayKey(afterFour));
		Assert.False(YanportGhostCleanup.Due(20261122, afterFour));

		// Next day after 4am -> due again
		DateTime nextDay = new DateTime(2026, 11, 23, 3, 15, 0, DateTimeKind.Utc);
		Assert.True(YanportGhostCleanup.Due(20261122, nextDay));
		Assert.Equal(20261123, YanportGhostCleanup.DayKey(nextDay));
	}

	[Fact]
	public void BuildBatchUrl_formats_size_and_repeated_ids()
	{
		string url = YanportGhostCleanup.BuildBatchUrl(new[] { "aaa-111", "bbb-222" });
		Assert.Equal("https://api.yanport.com/properties?size=2&ids=aaa-111&ids=bbb-222", url);
	}

	[Fact]
	public void BuildBatchUrl_rejects_empty_input()
	{
		Assert.Throws<ArgumentException>(() => YanportGhostCleanup.BuildBatchUrl(Array.Empty<string>()));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("NotFound")]
	[InlineData("Error500")]
	[InlineData("{invalid")]
	[InlineData("{\"total\":0}")]
	public void TryParseBatchResponse_rejects_error_sentinels_and_malformed_json(string payload)
	{
		Assert.False(YanportGhostCleanup.TryParseBatchResponse(payload, out _));
	}

	[Fact]
	public void TryParseBatchResponse_parses_active_and_ended_hits()
	{
		string json = """
		{
		  "total": 2,
		  "hits": [
		    { "id": "p-active", "marketing": { "active": true } },
		    { "id": "p-ended", "marketing": { "active": false, "publicationEndDate": "2026-11-20T10:00:00Z" } }
		  ]
		}
		""";

		Assert.True(YanportGhostCleanup.TryParseBatchResponse(json, out Dictionary<string, YanportGhostCleanup.YanportStatus> found));
		Assert.Equal(2, found.Count);
		Assert.True(found["p-active"].Active);
		Assert.Null(found["p-active"].PublicationEndDateParis);
		Assert.False(found["p-ended"].Active);
		Assert.Equal(new DateTime(2026, 11, 20, 11, 0, 0), found["p-ended"].PublicationEndDateParis);
	}

	[Fact]
	public void ClassifyBatch_respects_48h_grace_period_and_separates_404_from_ended()
	{
		DateTime parisNow = new DateTime(2026, 11, 22, 4, 30, 0);
		YanportGhostCleanup.Candidate[] batch = new[]
		{
			new YanportGhostCleanup.Candidate("p-active", parisNow.AddDays(-10)),
			new YanportGhostCleanup.Candidate("p-recent-404", parisNow.AddHours(-12)),
			new YanportGhostCleanup.Candidate("p-old-404", parisNow.AddHours(-72)),
			new YanportGhostCleanup.Candidate("p-ended-with-date", parisNow.AddDays(-5)),
			new YanportGhostCleanup.Candidate("p-ended-no-date", parisNow.AddDays(-3)),
		};

		Dictionary<string, YanportGhostCleanup.YanportStatus> found = new Dictionary<string, YanportGhostCleanup.YanportStatus>
		{
			["p-active"] = new YanportGhostCleanup.YanportStatus(true, null),
			["p-ended-with-date"] = new YanportGhostCleanup.YanportStatus(false, new DateTime(2026, 11, 19, 15, 0, 0)),
			["p-ended-no-date"] = new YanportGhostCleanup.YanportStatus(false, null),
		};

		YanportGhostCleanup.BatchClassification res = YanportGhostCleanup.ClassifyBatch(batch, found, parisNow);

		Assert.Equal(1, res.StillActive);
		Assert.Equal(1, res.SkippedRecent404);
		Assert.Single(res.ToCloseNotFound);
		Assert.Equal("p-old-404", res.ToCloseNotFound[0].PropertyId);
		Assert.Equal(parisNow.AddHours(-72), res.ToCloseNotFound[0].DateFinParis);
		Assert.Equal("not_found_404", res.ToCloseNotFound[0].Reason);

		Assert.Equal(2, res.ToCloseEnded.Count);
		Assert.Equal("p-ended-with-date", res.ToCloseEnded[0].PropertyId);
		Assert.Equal(new DateTime(2026, 11, 19, 15, 0, 0), res.ToCloseEnded[0].DateFinParis);
		Assert.Equal("p-ended-no-date", res.ToCloseEnded[1].PropertyId);
		Assert.Equal(parisNow.AddDays(-3), res.ToCloseEnded[1].DateFinParis);
	}

	[Fact]
	public void Anomaly_circuit_breaker_aborts_if_over_35_percent_404_on_large_run()
	{
		// Small run under threshold: does not abort
		Assert.False(YanportGhostCleanup.ShouldAbortForAnomaly(100, 80));
		// Normal initial run (~14.6% 404): allowed
		Assert.False(YanportGhostCleanup.ShouldAbortForAnomaly(56000, 8240));
		// API outage returning empty hits (~50% 404): aborted
		Assert.True(YanportGhostCleanup.ShouldAbortForAnomaly(1000, 500));
	}

	[Fact]
	public void BatchSuccessRateAcceptable_requires_95_percent_success()
	{
		Assert.True(YanportGhostCleanup.BatchSuccessRateAcceptable(100, 5));
		Assert.False(YanportGhostCleanup.BatchSuccessRateAcceptable(100, 6));
	}

	[Fact]
	public void Sql_guards_preserve_portal410_and_only_close_open_properties()
	{
		Assert.Contains("P_State = 1", YanportGhostCleanup.SelectActiveSql, StringComparison.Ordinal);
		Assert.Contains("P_DateFin IS NULL OR P_DateFin < '1000-01-01'", YanportGhostCleanup.SelectActiveSql, StringComparison.Ordinal);
		Assert.Contains("IFNULL(P_EstRattrapage, 0) <> 4", YanportGhostCleanup.SelectActiveSql, StringComparison.Ordinal);

		Assert.Contains("P_State = 0", YanportGhostCleanup.CloseOneSql, StringComparison.Ordinal);
		Assert.Contains("P_EstRattrapage = 5", YanportGhostCleanup.CloseOneSql, StringComparison.Ordinal);
		Assert.Contains("IFNULL(P_EstRattrapage, 0) <> 4", YanportGhostCleanup.CloseOneSql, StringComparison.Ordinal);
		Assert.DoesNotContain("P_DateLastUpdate =", YanportGhostCleanup.CloseOneSql, StringComparison.Ordinal);
		Assert.DoesNotContain("DELETE", YanportGhostCleanup.CloseOneSql, StringComparison.OrdinalIgnoreCase);
	}
}
