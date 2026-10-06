using Xunit;

namespace ApiPerleRare.Tests;

public class ScheduledTasksSwitchTests
{
	[Theory]
	[InlineData(null, true)]
	[InlineData("", true)]
	[InlineData("on", true)]
	[InlineData("off", false)]
	[InlineData(" OFF ", false)]
	public void Scheduled_tasks_run_unless_explicitly_off(string value, bool expected)
	{
		Assert.Equal(expected, Startup.ScheduledTasksEnabled(value));
	}
}
