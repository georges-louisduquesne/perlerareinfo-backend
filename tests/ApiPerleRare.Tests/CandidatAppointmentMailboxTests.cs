using ApiPerleRare.Application.Exchange;
using Xunit;

namespace ApiPerleRare.Tests;

public class CandidatAppointmentMailboxTests
{
	[Fact]
	public void Existing_appointment_agenda_is_the_organizer_not_the_mailbox_that_held_a_copy()
	{
		Assert.Equal(
			"Marion.Normand@perle-rare.com",
			CandidatAppointmentMailbox.AgendaMailbox(
				"Marion.Normand@perle-rare.com",
				"georges@perle-rare.com"));
	}

	[Fact]
	public void Legacy_organizer_falls_back_to_the_mailbox()
	{
		const string dn = "/o=Org/ou=Exchange Administrative Group (FYDIBOHF23SPDLT)/cn=Recipients/cn=x";
		Assert.Equal(
			"marion.normand@perle-rare.com",
			CandidatAppointmentMailbox.AgendaMailbox(dn, " marion.normand@perle-rare.com "));
		Assert.Equal(string.Empty, CandidatAppointmentMailbox.AgendaMailbox(null, "  "));
	}
}
