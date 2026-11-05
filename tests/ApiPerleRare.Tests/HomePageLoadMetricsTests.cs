using System;
using System.Linq;
using System.Reflection;
using ApiPerleRare.Application.Monitoring;
using ApiPerleRare.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace ApiPerleRare.Tests;

public class HomePageLoadMetricsTests
{
	[Fact]
	public void ClampDurationMs_bounds()
	{
		Assert.Equal(0, HomePageLoadMetrics.ClampDurationMs(-10));
		Assert.Equal(1200, HomePageLoadMetrics.ClampDurationMs(1200));
		Assert.Equal(HomePageLoadMetrics.MaxDurationMs, HomePageLoadMetrics.ClampDurationMs(999_999));
	}

	[Fact]
	public void Average_ignores_negative_values()
	{
		Assert.Equal(1500, HomePageLoadMetrics.Average(new[] { 1000, 2000, -1 }));
		Assert.Equal(0, HomePageLoadMetrics.Average(Array.Empty<int>()));
	}

	[Fact]
	public void SegmentAggregate_splits_admin()
	{
		var rows = new (int, bool)[]
		{
			(1000, false),
			(3000, true),
			(2000, false),
		};
		var all = HomePageLoadMetrics.SegmentAggregate(rows, null);
		Assert.Equal(2000, all.AverageMs);
		Assert.Equal(3, all.Count);
		var nonAdmin = HomePageLoadMetrics.SegmentAggregate(rows, false);
		Assert.Equal(1500, nonAdmin.AverageMs);
		Assert.Equal(2, nonAdmin.Count);
		var admin = HomePageLoadMetrics.SegmentAggregate(rows, true);
		Assert.Equal(3000, admin.AverageMs);
		Assert.Equal(1, admin.Count);
	}

	[Fact]
	public void FormatPrenomNom_matches_front()
	{
		Assert.Equal("Julien DIEMER", HomePageLoadMetrics.FormatPrenomNom("Julien", "Diemer"));
		Assert.Equal("Diane DE MONTEYNARD", HomePageLoadMetrics.FormatPrenomNom("diane", "de monteynard"));
	}

	[Fact]
	public void SelectIdsToPrune_keeps_recent_rows()
	{
		long[] stale = HomePageLoadMetrics.SelectIdsToPrune(new long[] { 9, 8, 7, 6 }, 2);
		Assert.Equal(new long[] { 7, 6 }, stale);
	}

	[Fact]
	public void BuildDailyTrend_groups_by_paris_date_and_splits_admin()
	{
		DateTime day1Morning = new DateTime(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);
		DateTime day1Evening = new DateTime(2026, 11, 1, 22, 0, 0, DateTimeKind.Utc);
		DateTime day2 = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
		var rows = new (DateTime, int, bool)[]
		{
			(day1Morning, 1000, false),
			(day1Evening, 3000, true),
			(day2, 2000, false),
		};
		var daily = HomePageLoadMetrics.BuildDailyTrend(rows);
		Assert.Equal(2, daily.Count);
		Assert.Equal("2026-11-01", daily[0].Date);
		Assert.Equal(1000, daily[0].NonAdminAverageMs);
		Assert.Equal(3000, daily[0].AdminAverageMs);
		Assert.Equal(2000, daily[1].NonAdminAverageMs);
		Assert.Null(daily[1].AdminAverageMs);
	}
}

public class HomePageLoadControllerTests
{
	[Fact]
	public void CanRecordLoad_accepts_ref_conseiller_zero()
	{
		Assert.True(HomePageLoadMetrics.CanRecordLoad(true, 0));
		Assert.True(HomePageLoadMetrics.CanRecordLoad(true, 178));
		Assert.False(HomePageLoadMetrics.CanRecordLoad(false, 0));
		Assert.False(HomePageLoadMetrics.CanRecordLoad(true, -1));
	}

	[Fact]
	public void Cockpit_is_admin_only()
	{
		var method = typeof(HomePageLoadController).GetMethod("Cockpit");
		Assert.NotNull(method);
		var attrs = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>();
		Assert.Contains(attrs, attr => (attr.Roles ?? "").Split(',').Any(role => role.Trim() == "Admin"));
	}
}
