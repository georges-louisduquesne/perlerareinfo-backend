using ApiPerleRare.Application.Tasks;
using Xunit;

namespace ApiPerleRare.Tests;

public class HomeAccueilTachesQueriesTests
{
	[Fact]
	public void Prospect_statuts_match_front_accueil()
	{
		Assert.Equal("PROSPECT ACTIF", HomeAccueilTachesQueries.ProspectActif);
		Assert.Equal("PROSPECT MORT", HomeAccueilTachesQueries.ProspectMort);
	}

	[Fact]
	public void Client_statuts_match_front_accueil()
	{
		Assert.Equal("CLIENT ACTIF", HomeAccueilTachesQueries.ClientActif);
		Assert.Equal("CLIENT MORT", HomeAccueilTachesQueries.ClientMort);
	}
}
