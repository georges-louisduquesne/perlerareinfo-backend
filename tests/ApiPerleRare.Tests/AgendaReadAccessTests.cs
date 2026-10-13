using ApiPerleRare.Application.Exchange;
using Xunit;

namespace ApiPerleRare.Tests;

public class AgendaReadAccessTests
{
	[Fact]
	public void Laurence_lit_son_agenda_par_id_ou_email()
	{
		Assert.True(AgendaReadAccess.Allows(81, false, "81", "laurence.berry@perle-rare.com"));
		Assert.True(AgendaReadAccess.Allows(81, false, "Laurence.Berry@perle-rare.com", "laurence.berry@perle-rare.com"));
		Assert.False(AgendaReadAccess.Allows(81, false, "82", "laurence.berry@perle-rare.com"));
		Assert.False(AgendaReadAccess.Allows(81, false, "ada@perle-rare.com", "laurence.berry@perle-rare.com"));
	}

	[Fact]
	public void Un_admin_peut_lire_un_autre_agenda()
	{
		Assert.True(AgendaReadAccess.Allows(0, true, "81", "georges-louis.duquesne@perle-rare.com"));
	}
}
