using ApiPerleRare.Application.Contacts;
using Xunit;

namespace ApiPerleRare.Tests;

public class RegistreMandatsQueryTests
{
	[Fact]
	public void Sql_liste_tous_les_mandats_signes_sans_filtre_conseiller()
	{
		Assert.Contains("FROM contacts_recherche", RegistreMandatsQuery.Sql);
		Assert.Contains("C_Date <> '0000-00-00'", RegistreMandatsQuery.Sql);
		Assert.Contains("DATE_FORMAT(C_Date, '%Y-%m-%d')", RegistreMandatsQuery.Sql);
		Assert.Contains("ORDER BY C_NumeroMandat", RegistreMandatsQuery.Sql);
		Assert.Contains("C_PaysRegion", RegistreMandatsQuery.Sql);
		Assert.DoesNotContain("C_Apporteur", RegistreMandatsQuery.Sql);
		Assert.DoesNotContain("C_Negociateur", RegistreMandatsQuery.Sql);
	}

	[Fact]
	public void FromValues_garde_la_date_civile_et_le_pdf()
	{
		RegistreMandatRow row = RegistreMandatsQuery.FromValues(
			63121,
			5171,
			"mandats/MD_BOUDJELLAL_srh1kd21lv.pdf",
			"2026-10-21",
			"BOUDJELLAL",
			"Farouk",
			"12 rue de la Paix",
			"75002",
			"PARIS",
			"FRANCE");
		Assert.Equal(63121, row.Ref);
		Assert.Equal(5171, row.Numero);
		Assert.Equal("mandats/MD_BOUDJELLAL_srh1kd21lv.pdf", row.Mandat);
		Assert.Equal("2026-10-21", row.Date);
		Assert.Equal("BOUDJELLAL", row.Nom);
		Assert.Equal("Farouk", row.Prenom);
		Assert.Equal("12 rue de la Paix", row.Adresse);
		Assert.Equal("75002", row.Cp);
		Assert.Equal("PARIS", row.Ville);
		Assert.Equal("FRANCE", row.Pays);
	}

	[Fact]
	public void FromValues_accepte_une_date_annee_un_et_un_numero_absent()
	{
		RegistreMandatRow row = RegistreMandatsQuery.FromValues(
			63496, null, "", "0001-01-01", "SCHIEFFER", "AndrÃ©a", null, "", "PARIS", "FRANCE");
		Assert.Null(row.Numero);
		Assert.Equal("", row.Mandat);
		Assert.Equal("0001-01-01", row.Date);
		Assert.Equal("Andréa", row.Prenom);
		Assert.Equal("", row.Adresse);
	}
}
