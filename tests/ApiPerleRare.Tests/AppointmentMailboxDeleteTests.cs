using ApiPerleRare.Application.Exchange;
using Xunit;

namespace ApiPerleRare.Tests;

public class AppointmentMailboxDeleteTests
{
	[Fact]
	public void Nettoyer_la_boite_du_connecte_laisse_le_rv_de_Laurence()
	{
		AppointmentMailboxDelete.Target target = AppointmentMailboxDelete.ForRequestedMailbox(0, 81, "AAMk-laurence-509312");
		Assert.Equal(0, target.MailboxId);
		Assert.Null(target.AppointmentId);
		Assert.False(target.ClearStoredAppointmentId);
	}

	[Fact]
	public void Supprimer_la_boite_organisatrice_efface_l_identifiant_stocke()
	{
		AppointmentMailboxDelete.Target target = AppointmentMailboxDelete.ForRequestedMailbox(81, 81, "AAMk-laurence-509312");
		Assert.Equal(81, target.MailboxId);
		Assert.Equal("AAMk-laurence-509312", target.AppointmentId);
		Assert.True(target.ClearStoredAppointmentId);
	}
}
