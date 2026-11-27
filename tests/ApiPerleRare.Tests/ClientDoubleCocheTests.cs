using System.Collections.Generic;
using ApiPerleRare.Application.Contacts;
using ApiPerleRare.Controllers;
using Xunit;

namespace ApiPerleRare.Tests;

public class ClientDoubleCocheTests
{
	[Fact]
	public void Sql_selects_active_compromis_and_acte_linked_to_the_page_contacts()
	{
		string sql = ClientDoubleCoche.BuildSql("62424,61252");
		Assert.Contains("E_Statut = 1", sql);
		Assert.Contains("E_RefContact IN (62424,61252)", sql);
		Assert.Contains("'RV COMPROMIS'", sql);
		Assert.Contains("'RV ACTE AUTHENT.'", sql);
		Assert.Contains("'RV ACTE AUTHENT'", sql);
		Assert.DoesNotContain("OFFRE", sql);
	}

	[Fact]
	public void Apply_marks_only_contacts_with_an_active_closing_event()
	{
		List<ContactsRechercheAccueil> rows = new List<ContactsRechercheAccueil>
		{
			new ContactsRechercheAccueil { CRefContact = 62424, CTypeRecherche = "0" },
			new ContactsRechercheAccueil { CRefContact = 1, CTypeRecherche = "2" },
			new ContactsRechercheAccueil { CRefContact = 2, CTypeRecherche = "1" }
		};
		ClientDoubleCoche.Apply(rows, new HashSet<uint> { 62424 });
		Assert.True(rows[0]._DoubleCoche);
		Assert.False(rows[1]._DoubleCoche);
		Assert.Equal("2", rows[1].CTypeRecherche);
		Assert.False(rows[2]._DoubleCoche);
	}

	[Fact]
	public void Apply_clears_the_flag_when_no_closing_event_matches()
	{
		ContactsRechercheAccueil row = new ContactsRechercheAccueil { CRefContact = 62424, _DoubleCoche = true };
		ClientDoubleCoche.Apply(new[] { row }, new HashSet<uint>());
		Assert.False(row._DoubleCoche);
		ClientDoubleCoche.Apply(null, new HashSet<uint> { 62424 });
	}

	[Theory]
	[InlineData(62424u)]
	public void Reader_accepts_the_integer_types_the_driver_returns(uint expected)
	{
		Assert.True(ClientDoubleCoche.TryReadContactId(expected, out uint fromUint));
		Assert.Equal(expected, fromUint);
		Assert.True(ClientDoubleCoche.TryReadContactId(62424, out uint fromInt));
		Assert.Equal(62424u, fromInt);
		Assert.True(ClientDoubleCoche.TryReadContactId(62424L, out uint fromLong));
		Assert.Equal(62424u, fromLong);
		Assert.True(ClientDoubleCoche.TryReadContactId(62424UL, out uint fromUlong));
		Assert.Equal(62424u, fromUlong);
		Assert.False(ClientDoubleCoche.TryReadContactId(null, out _));
		Assert.False(ClientDoubleCoche.TryReadContactId(0, out _));
		Assert.False(ClientDoubleCoche.TryReadContactId("62424", out _));
	}
}
