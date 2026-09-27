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
}
