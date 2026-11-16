using ApiPerleRare.Helpers;
using Xunit;

namespace ApiPerleRare.Tests;

public class AlerteAgendaPreferenceTests
{
	[Fact]
	public void Creation_stores_the_agenda_button_checked()
	{
		Assert.True(AlerteAgendaPreference.ValueOnCreate());
		Assert.Equal(1, AlerteAgendaPreference.ToStoredFlag(true));
		Assert.Equal(0, AlerteAgendaPreference.ToStoredFlag(false));
	}

	[Fact]
	public void Update_targets_only_the_signed_in_conseiller()
	{
		Assert.Equal(12u, AlerteAgendaPreference.OwnRow(hasClaim: true, userId: 12));
		Assert.Equal(0u, AlerteAgendaPreference.OwnRow(hasClaim: true, userId: 0));
		Assert.Null(AlerteAgendaPreference.OwnRow(hasClaim: false, userId: 0));
		Assert.Null(AlerteAgendaPreference.OwnRow(hasClaim: false, userId: 12));
		Assert.Null(AlerteAgendaPreference.OwnRow(hasClaim: true, userId: -1));
	}
}
