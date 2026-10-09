using System;
using ApiPerleRare.Application.Tasks;
using Xunit;

namespace ApiPerleRare.Tests;

public class AccueilTacheWindowTests
{
	[Fact]
	public void Afternoon_alert_today_is_still_due()
	{
		DateTime now = new DateTime(2026, 10, 9, 12, 7, 0);
		DateTime alertAtNoon = new DateTime(2026, 10, 9, 12, 0, 0);
		DateTime dueBefore = AccueilTacheWindow.DueBefore(now);
		Assert.True(alertAtNoon < dueBefore);
		Assert.False(alertAtNoon <= now.Date);
	}

	[Fact]
	public void Tomorrow_midnight_is_excluded()
	{
		DateTime now = new DateTime(2026, 10, 9, 12, 7, 0);
		DateTime tomorrow = new DateTime(2026, 10, 10, 0, 0, 0);
		Assert.False(tomorrow < AccueilTacheWindow.DueBefore(now));
	}
}
