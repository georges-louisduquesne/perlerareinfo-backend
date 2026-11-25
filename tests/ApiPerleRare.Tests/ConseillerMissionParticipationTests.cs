using ApiPerleRare.Application.Events;
using Xunit;

namespace ApiPerleRare.Tests;

public class ConseillerMissionParticipationTests
{
	[Fact]
	public void Genres_are_mission_and_transaction()
	{
		Assert.Equal(new[] { "MISSION", "TRANSACTION" }, ConseillerMissionParticipation.Genres);
	}

	[Fact]
	public void Participates_when_login_matches_any_title_including_the_second_holder()
	{
		Assert.Equal(new[] { "apporteur" }, ConseillerMissionParticipation.Roles("ADAVOLI", "adavoli", "", "", "", "", ""));
		Assert.Equal(new[] { "negociateur" }, ConseillerMissionParticipation.Roles("BPEYNET", "", "", "", "BPEYNET", "", ""));
		Assert.Equal(new[] { "conseiller" }, ConseillerMissionParticipation.Roles("FROUX", "Perle Rare", "", "AGAGNAT", "", "", "froux"));
		Assert.Equal(new[] { "apporteur", "negociateur", "conseiller" }, ConseillerMissionParticipation.Roles(
			"STRICHARD", "STRICHARD", "", "STRICHARD", "", "STRICHARD", ""));
	}

	[Fact]
	public void Does_not_participate_on_blank_login_or_another_conseiller()
	{
		Assert.False(ConseillerMissionParticipation.Participates("", "ADAVOLI", "", "", "", "", ""));
		Assert.False(ConseillerMissionParticipation.Participates("   ", "ADAVOLI", "", "", "", "", ""));
		Assert.False(ConseillerMissionParticipation.Participates("ADAVOLI", "Perle Rare", "", "BPEYNET", "", "FROUX", ""));
		Assert.Empty(ConseillerMissionParticipation.Roles("ADAVOLI", null, null, null, null, null, null));
	}
}
