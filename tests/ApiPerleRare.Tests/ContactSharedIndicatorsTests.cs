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
	public void Property_pills_include_offre_offre_acceptee_and_visite()
	{
		Assert.True(ContactSharedIndicatorsQuery.IncludeAnyActiveEventType("OFFRE"));
		Assert.True(ContactSharedIndicatorsQuery.IncludeAnyActiveEventType("OFFRE ACCEPTEE"));
		Assert.True(ContactSharedIndicatorsQuery.IncludeAnyActiveEventType("RV VISITE CLIENT"));
		Assert.True(ContactSharedIndicatorsQuery.IncludeAnyActiveEventType("TEL OUT"));
		Assert.True(ContactSharedIndicatorsQuery.IncludeAnyActiveEventType(null));
		Assert.True(ContactSharedIndicatorsQuery.IncludeAnyActiveEventType(""));
	}

	[Fact]
	public void Own_pill_links_offre_and_visite_to_the_same_property()
	{
		const string propertyId = "8ba06a00-dd89-11f1-b6ff-9ddb9a4ff125";
		Assert.True(ContactSharedIndicatorsQuery.OwnVisitMatchesPropertyContact(
			propertyId, 826851, 63777, 826851, propertyId, 63777));
		Assert.False(ContactSharedIndicatorsQuery.OwnVisitMatchesPropertyContact(
			"autre-bien", null, 63777, 826851, propertyId, 63777));
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
