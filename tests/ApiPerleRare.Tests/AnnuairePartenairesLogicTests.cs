using System.Collections.Generic;
using System.Linq;
using ApiPerleRare.Application.Annuaire;
using Xunit;

namespace ApiPerleRare.Tests;

public class AnnuairePartenairesLogicTests
{
	[Fact]
	public void Clean_keeps_an_eighth_partner_and_drops_a_blank_row()
	{
		var rows = Seed();
		rows.Add(new AnnuairePartenaireRow
		{
			Id = "p-huitieme",
			Categorie = "Partenaire X",
			Nom = "Ada Test"
		});
		rows.Add(new AnnuairePartenaireRow
		{
			Id = "vide",
			Categorie = "   ",
			Nom = ""
		});

		List<AnnuairePartenaireRow> cleaned = AnnuairePartenairesLogic.Clean(rows);

		Assert.Equal(8, cleaned.Count);
		Assert.Equal("p-huitieme", cleaned[7].Id);
		Assert.Equal("Ada Test", cleaned[7].Nom);
		Assert.Equal("Partenaire X", cleaned[7].Categorie);
	}

	[Fact]
	public void Clean_rewrites_a_duplicate_id_so_a_new_partner_is_not_merged_away()
	{
		List<AnnuairePartenaireRow> cleaned = AnnuairePartenairesLogic.Clean(new[]
		{
			new AnnuairePartenaireRow { Id = "p-same", Nom = "Premier" },
			new AnnuairePartenaireRow { Id = "p-same", Nom = "Deuxième" }
		});

		Assert.Equal(2, cleaned.Count);
		Assert.Equal("Premier", cleaned[0].Nom);
		Assert.Equal("Deuxième", cleaned[1].Nom);
		Assert.NotEqual(cleaned[0].Id, cleaned[1].Id);
	}

	[Fact]
	public void Clean_rejects_an_empty_directory()
	{
		Assert.Empty(AnnuairePartenairesLogic.Clean(null));
		Assert.Empty(AnnuairePartenairesLogic.Clean(new[]
		{
			new AnnuairePartenaireRow { Id = "x", Nom = "  " }
		}));
	}

	private static List<AnnuairePartenaireRow> Seed()
	{
		return Enumerable.Range(1, 7)
			.Select(i => new AnnuairePartenaireRow { Id = "p-" + i, Nom = "Nom " + i })
			.ToList();
	}
}
