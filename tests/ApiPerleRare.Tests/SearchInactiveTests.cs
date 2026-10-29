using ApiPerleRare.Services;
using Xunit;

namespace ApiPerleRare.Tests;

public class SearchInactiveTests
{
	[Fact]
	public void Inactive_categories_are_queried_apart_from_the_active_ones()
	{
		string agent = SearchService.SelectSql("B32");
		string reseau = SearchService.SelectSql("B35");
		Assert.Contains("ci.C_Statut='Inactif'", agent);
		Assert.Contains("I2_Actif=0", reseau);
		Assert.Contains("ci.C_Statut='Actif'", SearchService.SelectSql("B3bis"));
		Assert.DoesNotContain("IsInactive", SearchService.SelectSql("B3bis"));
		Assert.Contains("C_Statut='Inactif'", SearchService.SelectSql("B34"));
		Assert.Contains("I2_Actif=0", SearchService.SelectSql("B36"));
	}

	[Fact]
	public void Conseiller_and_candidat_select_the_actif_flag_on_every_match_mode()
	{
		foreach (string id in new[] { "B1", "B11", "B21", "B7", "B17", "B27" })
		{
			Assert.Contains("CASE WHEN CP_Actif=1 THEN 0 ELSE 1 END", SearchService.SelectSql(id));
			Assert.True(SearchService.InactiveFieldIndexOf(id) > 0);
		}
		Assert.Equal(-1, SearchService.InactiveFieldIndexOf("B4"));
	}

	[Theory]
	[InlineData("Agence inactive", true)]
	[InlineData("Collaborateur inactif", true)]
	[InlineData("Réseau d'agences inactif", true)]
	[InlineData("Client inactif", true)]
	[InlineData("Conseiller", false)]
	[InlineData("Candidat", false)]
	[InlineData("Collaborateur actif", false)]
	public void Category_label_marks_inactive_objects(string category, bool inactive)
	{
		Assert.Equal(inactive, SearchService.CategoryMarksInactive(category));
	}

	[Fact]
	public void Inactive_conseiller_or_candidat_gets_an_inactif_label()
	{
		Assert.Equal("Conseiller inactif", SearchService.InactiveCategoryLabel("Conseiller", true));
		Assert.Equal("Candidat inactif", SearchService.InactiveCategoryLabel("Candidat", true));
		Assert.Equal("Conseiller", SearchService.InactiveCategoryLabel("Conseiller", false));
		Assert.Equal("Agence inactive", SearchService.InactiveCategoryLabel("Agence inactive", true));
		Assert.True(SearchService.IsInactiveFlag(1));
		Assert.True(SearchService.IsInactiveFlag(true));
		Assert.False(SearchService.IsInactiveFlag(0));
		Assert.False(SearchService.IsInactiveFlag(null));
	}
}
