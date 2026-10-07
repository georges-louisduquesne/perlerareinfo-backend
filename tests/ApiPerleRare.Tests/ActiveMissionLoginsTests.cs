using System.Linq;
using ApiPerleRare.Application.Disponibilites;
using ApiPerleRare.Models;
using Xunit;

namespace ApiPerleRare.Tests;

public class ActiveMissionLoginsTests
{
	[Fact]
	public void Keeps_only_client_actif_searches_for_every_conseiller()
	{
		ContactsRecherche[] rows =
		{
			new ContactsRecherche { CStatut = "CLIENT ACTIF", CTypeRecherche = "0", CNomFamilleConseiller = "AGAGNAT" },
			new ContactsRecherche { CStatut = "CLIENT ACTIF", CTypeRecherche = "0", CNomFamilleConseiller = "agagnat" },
			new ContactsRecherche { CStatut = "CLIENT ACTIF", CTypeRecherche = "1", CNomFamilleConseiller = "AGAGNAT" },
			new ContactsRecherche { CStatut = "PROSPECT ACTIF", CTypeRecherche = "0", CNomFamilleConseiller = "CBLANCHARD" },
			new ContactsRecherche { CStatut = "CLIENT ACTIF", CTypeRecherche = "0", CNomFamilleConseiller = "CBLANCHARD" }
		};

		string[] logins = ActiveMissionLogins.Query(rows.AsQueryable())
			.Select((c) => c.CNomFamilleConseiller)
			.ToArray();

		Assert.Equal(new[] { "AGAGNAT", "agagnat", "CBLANCHARD" }, logins);
	}
}
