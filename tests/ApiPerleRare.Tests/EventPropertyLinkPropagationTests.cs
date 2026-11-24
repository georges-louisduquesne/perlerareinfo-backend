using ApiPerleRare.Application.Events;
using ApiPerleRare.Models;
using Xunit;

namespace ApiPerleRare.Tests;

public class EventPropertyLinkPropagationTests
{
	[Fact]
	public void Saved_visit_link_fills_other_active_events_of_the_same_contact_and_bien()
	{
		Evenements source = Linked("OFFRE", 10, 700, 20, 800, "prop-1");
		Evenements cr = Empty("CR VISITE", 11, 700, 20, statut: 1);
		Evenements visite = Empty("RV VISITE CLIENT", 12, 700, 20, statut: 1);

		Assert.True(EventPropertyLinkPropagation.ShouldFill(source, cr));
		Assert.True(EventPropertyLinkPropagation.ShouldFill(source, visite));
	}

	[Fact]
	public void Does_not_fill_inactive_other_contact_other_bien_or_already_linked()
	{
		Evenements source = Linked("RV VISITE SEUL", 10, 700, 20, 800, "prop-1");

		Assert.False(EventPropertyLinkPropagation.ShouldFill(source, Empty("CR VISITE", 11, 700, 20, statut: 0)));
		Assert.False(EventPropertyLinkPropagation.ShouldFill(source, Empty("OFFRE", 12, 701, 20, statut: 1)));
		Assert.False(EventPropertyLinkPropagation.ShouldFill(source, Empty("OFFRE", 13, 700, 21, statut: 1)));
		Assert.False(EventPropertyLinkPropagation.ShouldFill(source, Linked("OFFRE ACCEPTEE", 14, 700, 20, 801, "prop-2")));
		Assert.False(EventPropertyLinkPropagation.ShouldFill(source, source));
		Assert.False(EventPropertyLinkPropagation.ShouldFill(source, Empty("TEL OUT", 15, 700, 20, statut: 1)));
	}

	[Fact]
	public void Event_without_a_resolved_fiche_does_not_propagate()
	{
		Evenements source = Linked("CR VISITE", 10, 700, 20, 0, "prop-1");
		Evenements target = Empty("RV COMPROMIS", 11, 700, 20, statut: 1);

		Assert.False(EventPropertyLinkPropagation.CarriesPropertyLink(source));
		Assert.False(EventPropertyLinkPropagation.ShouldFill(source, target));
	}

	[Fact]
	public void Acte_without_final_dot_is_part_of_the_same_family()
	{
		Evenements source = Linked("RV ACTE AUTHENT", 10, 700, 20, 800, "prop-1");
		Evenements target = Empty("RV ACTE AUTHENT.", 11, 700, 20, statut: 1);

		Assert.True(EventPropertyLinkPropagation.ShouldFill(source, target));
	}

	private static Evenements Linked(string type, int id, uint contact, uint bien, uint pcId, string propertyId)
	{
		return new Evenements
		{
			ERefEvenement = id,
			ETypeEvenement = type,
			EStatut = 1,
			ERefContact = contact,
			ERefBien = bien,
			EPcId = pcId == 0 ? null : pcId,
			EPropertyId = propertyId,
			ECr = "",
			ENomContact = "",
			ETexte = ""
		};
	}

	private static Evenements Empty(string type, int id, uint contact, uint bien, byte statut)
	{
		return new Evenements
		{
			ERefEvenement = id,
			ETypeEvenement = type,
			EStatut = statut,
			ERefContact = contact,
			ERefBien = bien,
			EPcId = null,
			EPropertyId = null,
			ECr = "",
			ENomContact = "",
			ETexte = ""
		};
	}
}
