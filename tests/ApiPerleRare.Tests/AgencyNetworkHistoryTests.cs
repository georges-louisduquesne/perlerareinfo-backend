using ApiPerleRare.Controllers;
using Xunit;

namespace ApiPerleRare.Tests;

public class AgencyNetworkHistoryTests
{
	[Fact]
	public void Same_network_is_not_a_change()
	{
		Assert.False(AgencyNetworkHistory.Changed(12, "ORPI", 12, " ORPI "));
	}

	[Fact]
	public void Missing_id_and_blank_name_count_as_empty()
	{
		Assert.False(AgencyNetworkHistory.Changed(0, "  ", null, ""));
		Assert.True(AgencyNetworkHistory.Changed(null, null, 34, "CENTURY 21"));
	}

	[Fact]
	public void Champs_json_matches_the_agency_history_shape()
	{
		string json = AgencyNetworkHistory.ChampsJson(null, null, 259, "ESPACES ATYPIQUES");
		Assert.Contains("\"I_RefGroupe\"", json);
		Assert.Contains("\"I_GroupeEventuel\"", json);
		Assert.Contains("\"apres\":\"259\"", json);
		Assert.Contains("\"apres\":\"ESPACES ATYPIQUES\"", json);
		Assert.Contains("\"avant\":null", json);
	}
}
