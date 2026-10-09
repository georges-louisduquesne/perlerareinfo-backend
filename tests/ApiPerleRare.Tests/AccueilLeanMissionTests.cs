using ApiPerleRare.Application.Contacts;
using ApiPerleRare.Models;
using Xunit;

namespace ApiPerleRare.Tests;

public class AccueilLeanMissionTests
{
	[Fact]
	public void Lean_accueil_keeps_mission_type_for_the_icon()
	{
		var project = ContactsRechercheAccueilLean.Projection.Compile();
		ContactsRecherche row = project(new ContactsRecherche
		{
			CRefContact = 9,
			CNomFamille = "MARTIN",
			CIdTypeMission = 4,
			CBudget = "a:3:{i:1;s:6:\"870000\";}"
		});
		Assert.Equal(4, row.CIdTypeMission);
		Assert.Equal("MARTIN", row.CNomFamille);
		Assert.Null(row.CBudget);
	}
}
