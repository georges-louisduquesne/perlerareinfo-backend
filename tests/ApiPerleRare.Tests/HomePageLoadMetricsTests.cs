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
