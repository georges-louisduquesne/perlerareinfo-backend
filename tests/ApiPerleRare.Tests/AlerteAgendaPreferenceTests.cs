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

	[Fact]
	public void Admin_can_change_the_open_conseiller_fiche()
	{
		Assert.Equal(408u, AlerteAgendaPreference.EditableRow(hasClaim: true, callerId: 12, isAdmin: true, requestedId: 408));
		Assert.Equal(0u, AlerteAgendaPreference.EditableRow(hasClaim: true, callerId: 12, isAdmin: true, requestedId: 0));
		Assert.Equal(12u, AlerteAgendaPreference.EditableRow(hasClaim: true, callerId: 12, isAdmin: false, requestedId: 12));
		Assert.Null(AlerteAgendaPreference.EditableRow(hasClaim: true, callerId: 12, isAdmin: false, requestedId: 408));
		Assert.Null(AlerteAgendaPreference.EditableRow(hasClaim: false, callerId: 0, isAdmin: true, requestedId: 0));
		Assert.Null(AlerteAgendaPreference.EditableRow(hasClaim: false, callerId: 0, isAdmin: true, requestedId: 408));
		Assert.Null(AlerteAgendaPreference.EditableRow(hasClaim: true, callerId: -1, isAdmin: true, requestedId: 408));
		Assert.Null(AlerteAgendaPreference.EditableRow(hasClaim: true, callerId: 12, isAdmin: true, requestedId: -1));
	}
}
