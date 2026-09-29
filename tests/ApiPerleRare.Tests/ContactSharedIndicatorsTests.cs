using ApiPerleRare.Application.PropertyContacts;
using Xunit;

namespace ApiPerleRare.Tests;

public class ContactSharedIndicatorsTests
{
	[Fact]
	public void Request_accepts_empty_property_ids()
	{
		var req = new ContactSharedIndicatorsRequest { PropertyIds = null };
		Assert.Null(req.PropertyIds);
	}

	[Fact]
	public void Visit_types_keep_defaults_when_table_is_empty()
	{
		var types = ContactSharedIndicatorsQuery.SelectVisitTypes(null);
		Assert.Contains("RV VISITE SEUL", types);
		Assert.Contains("RV VISITE CLIENT", types);
		Assert.Equal(2, types.Count);
	}

	[Fact]
	public void Visit_types_come_from_refs_5_6_or_vis_category_only()
	{
		var types = ContactSharedIndicatorsQuery.SelectVisitTypes(new[]
		{
			(5, "RV VISITE SEUL", "VISITE"),
			(6, "RV VISITE CLIENT", "VISITE"),
			(12, "CR VISITE", "Visite"),
			(3, "OFFRE", "OFFRE"),
			(7, "RV PROSPECT", "PROSPECTION"),
			(9, "  ", "VISITE"),
		});
		Assert.Contains("CR VISITE", types);
		Assert.DoesNotContain("OFFRE", types);
		Assert.DoesNotContain("RV PROSPECT", types);
		Assert.Equal(3, types.Count);
	}

	[Fact]
	public void Own_visit_without_property_or_pc_matches_no_card()
	{
		Assert.False(ContactSharedIndicatorsQuery.OwnVisitMatchesPropertyContact(
			null, null, 62696, 763549, "9bbadbd0-b88d-11f1-b0db-054d6852b2e2", 62696));
		Assert.False(ContactSharedIndicatorsQuery.OwnVisitMatchesPropertyContact(
			"  ", null, 62696, 763549, "9bbadbd0-b88d-11f1-b0db-054d6852b2e2", 62696));
	}

	[Fact]
	public void Own_visit_matches_same_property_or_same_pc()
	{
		const string propertyId = "9bbadbd0-b88d-11f1-b0db-054d6852b2e2";
		Assert.True(ContactSharedIndicatorsQuery.OwnVisitMatchesPropertyContact(
			propertyId, null, 62696, 763549, propertyId, 62696));
		Assert.False(ContactSharedIndicatorsQuery.OwnVisitMatchesPropertyContact(
			propertyId, null, 99, 763549, propertyId, 62696));
		Assert.True(ContactSharedIndicatorsQuery.OwnVisitMatchesPropertyContact(
			null, 763549, 62696, 763549, propertyId, 62696));
		Assert.False(ContactSharedIndicatorsQuery.OwnVisitMatchesPropertyContact(
			null, 1, 62696, 763549, propertyId, 62696));
	}

	[Fact]
	public void Counselor_match_prefers_login_then_last_name()
	{
		var people = new[]
		{
			new ContactSharedIndicatorsQuery.CounselorMatch
			{
				Login = "GLDUQUESNE",
				Prenom = "Georges-Louis",
				Nom = "Duquesne",
				Photo = "gl.png",
				Tel = "0600000000",
			},
		};
		var byLogin = ContactSharedIndicatorsQuery.MatchCounselor(people, "glduquesne");
		Assert.Equal("0600000000", byLogin.Tel);
		var byNom = ContactSharedIndicatorsQuery.MatchCounselor(people, "duquesne");
		Assert.Equal("Georges-Louis", byNom.Prenom);
		Assert.Null(ContactSharedIndicatorsQuery.MatchCounselor(people, "autre"));
	}
}
