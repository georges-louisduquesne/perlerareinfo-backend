using ApiPerleRare.Application.Agents;
using Xunit;

namespace ApiPerleRare.Tests;

public class AgentHistoriqueEditTests
{
	[Fact]
	public void Admin_can_replace_or_clear_the_stored_text()
	{
		Assert.Equal("Note", AgentHistoriqueEdit.Resolve(true, "Note", null));
		Assert.Equal("", AgentHistoriqueEdit.Resolve(true, "", "ancien"));
		Assert.Null(AgentHistoriqueEdit.Resolve(true, null, "ancien"));
	}

	[Fact]
	public void Non_admin_keeps_the_stored_text()
	{
		Assert.Equal("ancien", AgentHistoriqueEdit.Resolve(false, "piraté", "ancien"));
		Assert.Equal("ancien", AgentHistoriqueEdit.Resolve(false, null, "ancien"));
		Assert.Null(AgentHistoriqueEdit.Resolve(false, "piraté", null));
	}
}
