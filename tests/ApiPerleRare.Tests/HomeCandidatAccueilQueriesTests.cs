using ApiPerleRare.Application.Candidats;
using Xunit;

namespace ApiPerleRare.Tests;

public class HomeCandidatAccueilQueriesTests
{
	[Fact]
	public void Candidat_statut_matches_front_indexCandidats()
	{
		Assert.Equal("Candidat", HomeCandidatAccueilQueries.CandidatStatut);
	}

	[Fact]
	public void Rdv_category_matches_home_candidat_logic()
	{
		Assert.Equal("RV", HomeCandidatAccueilQueries.RdvCategory);
	}
}
