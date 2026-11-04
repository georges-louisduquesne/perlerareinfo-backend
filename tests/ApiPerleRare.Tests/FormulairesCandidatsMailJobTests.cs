using ApiPerleRare.ScheduledTasks;
using Xunit;

namespace ApiPerleRare.Tests;

public class FormulairesCandidatsMailJobTests
{
	[Theory]
	[InlineData(null, true)]
	[InlineData("", true)]
	[InlineData("on", true)]
	[InlineData("off", false)]
	[InlineData(" OFF ", false)]
	public void Job_runs_unless_explicitly_off(string value, bool expected)
	{
		Assert.Equal(expected, FormulairesCandidatsMailJob.Enabled(value));
	}
}
