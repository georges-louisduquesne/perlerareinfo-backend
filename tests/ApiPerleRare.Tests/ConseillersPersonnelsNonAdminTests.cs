using System;
using ApiPerleRare.Controllers;
using ApiPerleRare.Models;
using Xunit;

namespace ApiPerleRare.Tests;

public class ConseillersPersonnelsNonAdminTests
{
	private static ConseillersPersonnels FullRow() => new ConseillersPersonnels
	{
		CpRefConseiller = 12,
		CpPrenom = "Anna",
		CpMel = "anna@perle-rare.com",
		CpMotDePasse = "hash",
		CpMelMotDePasse = "exchange-secret",
		CpAutoLogin = "token",
		CpPieceIdentite = "id.pdf",
		CpDateNaissance = new DateOnly(1980, 1, 2),
		CpMelPerso = "anna@perso.fr",
		CpAdresse = "1 rue X",
		CpCp = 75001,
		CpVille = "PARIS",
		CpCommentaire = "rh",
		CpCv = "cv.pdf"
	};

	[Fact]
	public void Other_conseiller_keeps_directory_fields_only()
	{
		var cp = FullRow();
		ConseillersPersonnelsController.StripForNonAdmin(cp, isSelf: false);

		Assert.Equal("Anna", cp.CpPrenom);
		Assert.Equal("anna@perle-rare.com", cp.CpMel);
		Assert.Null(cp.CpMotDePasse);
		Assert.Null(cp.CpMelMotDePasse);
		Assert.Null(cp.CpAutoLogin);
		Assert.Null(cp.CpPieceIdentite);
		Assert.Null(cp.CpDateNaissance);
		Assert.Null(cp.CpMelPerso);
		Assert.Null(cp.CpAdresse);
		Assert.Equal(0, cp.CpCp);
		Assert.Null(cp.CpVille);
		Assert.Null(cp.CpCommentaire);
		Assert.Null(cp.CpCv);
	}

	[Fact]
	public void Self_keeps_personal_data_but_never_credentials()
	{
		var cp = FullRow();
		ConseillersPersonnelsController.StripForNonAdmin(cp, isSelf: true);

		Assert.Null(cp.CpMotDePasse);
		Assert.Null(cp.CpMelMotDePasse);
		Assert.Null(cp.CpAutoLogin);
		Assert.Equal("anna@perso.fr", cp.CpMelPerso);
		Assert.Equal("1 rue X", cp.CpAdresse);
		Assert.Equal(75001, cp.CpCp);
	}

	[Theory]
	[InlineData("CP_MotDePasse eq 'x'")]
	[InlineData("cp_mel_mot_de_passe ne null")]
	[InlineData("CpAutoLogin eq 'abc'")]
	[InlineData("CP_DateNaissance gt '1980-01-01'")]
	[InlineData("CP_MelPerso desc")]
	[InlineData("CP_Adresse eq 'x'")]
	[InlineData("CP_PieceIdentite ne null")]
	[InlineData("CP_Commentaire eq 'x'")]
	public void Filters_probing_restricted_fields_are_detected(string expression)
	{
		Assert.True(ConseillersPersonnelsController.UsesRestrictedField(expression));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("CP_Actif eq 1")]
	[InlineData("CP_Nom asc")]
	[InlineData("CP_RefConseiller eq 0")]
	public void Directory_filters_stay_allowed(string expression)
	{
		Assert.False(ConseillersPersonnelsController.UsesRestrictedField(expression));
	}
}
