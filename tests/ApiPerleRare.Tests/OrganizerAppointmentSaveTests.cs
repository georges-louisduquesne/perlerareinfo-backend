using ApiPerleRare.Application.Exchange;
using Xunit;

namespace ApiPerleRare.Tests;

public class OrganizerAppointmentSaveTests
{
	[Fact]
	public void Un_rv_avec_invites_est_cree_dans_le_calendrier_de_la_boite()
	{
		Assert.True(OrganizerAppointmentSave.SaveInCalendarFolder(hasAttendees: true));
		Assert.True(OrganizerAppointmentSave.SaveInCalendarFolder(hasAttendees: false));
	}
}
