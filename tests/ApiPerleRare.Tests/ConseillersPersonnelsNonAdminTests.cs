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
		CpLinkedIn = "https://www.linkedin.com/in/anna",
		CpAdresse = "1 rue X",
		CpCp = 75001,
		CpVille = "PARIS",
		CpCommentaire = "rh",
		CpOrigine = "LinkedIn",
		CpCv = "cv.pdf",
		CpDispoMission = true,
		CpCalendrierCal = "https://cal.com/anna"
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
		Assert.Null(cp.CpCalendrierCal);
		Assert.Null(cp.CpPieceIdentite);
		Assert.Null(cp.CpDateNaissance);
		Assert.Null(cp.CpMelPerso);
		Assert.Null(cp.CpLinkedIn);
		Assert.Null(cp.CpAdresse);
		Assert.Equal(0, cp.CpCp);
		Assert.Null(cp.CpVille);
		Assert.Null(cp.CpCommentaire);
		Assert.Null(cp.CpOrigine);
		Assert.Null(cp.CpCv);
		Assert.True(cp.CpDispoMission);
	}

	[Fact]
	public void Omitted_web_copy_keeps_stored_text_and_empty_string_clears()
	{
		var stored = new ConseillersPersonnels
		{
			CpPageWebFr = "https://perle-rare.com/fr/georges-louis-duquesne-director/",
			CpPageWebEn = "https://perle-rare.com/en/georges-louis-duquesne-director/",
			CpPhotoWeb = "https://perle-rare.com/wp-content/uploads/2023/01/chasseur-immobilier-perle-rare-georges-louis-duquesne-1.jpg",
			CpFonctionEn = "Founding co-director",
			CpParcoursFr = "Parcours français",
			CpParcoursEn = "English bio",
			CpLinkedIn = "https://www.linkedin.com/in/glduquesne/",
			CpGoogle = "https://www.google.com/maps/search/?api=1&query=Perle+Rare",
			CpCarteVirtuelle = "https://perle-rare.com/fr/carte-virtuelle-georges-louis-duquesne/"
		};
		var omitted = new ConseillersPersonnels();
		ConseillersPersonnelsController.KeepOmittedWebCopy(omitted, stored);
		Assert.Equal(stored.CpPageWebFr, omitted.CpPageWebFr);
		Assert.Equal(stored.CpPageWebEn, omitted.CpPageWebEn);
		Assert.Equal(stored.CpPhotoWeb, omitted.CpPhotoWeb);
		Assert.Equal(stored.CpFonctionEn, omitted.CpFonctionEn);
		Assert.Equal(stored.CpParcoursFr, omitted.CpParcoursFr);
		Assert.Equal(stored.CpParcoursEn, omitted.CpParcoursEn);
		Assert.Equal(stored.CpLinkedIn, omitted.CpLinkedIn);
		Assert.Equal(stored.CpGoogle, omitted.CpGoogle);
		Assert.Equal(stored.CpCarteVirtuelle, omitted.CpCarteVirtuelle);

		var cleared = new ConseillersPersonnels
		{
			CpPageWebFr = "",
			CpPageWebEn = "https://perle-rare.com/en/autre/",
			CpPhotoWeb = "",
			CpFonctionEn = "",
			CpParcoursFr = "",
			CpParcoursEn = "New bio",
			CpLinkedIn = "",
			CpGoogle = "",
			CpCarteVirtuelle = ""
		};
		ConseillersPersonnelsController.KeepOmittedWebCopy(cleared, stored);
		Assert.Equal("", cleared.CpPageWebFr);
		Assert.Equal("https://perle-rare.com/en/autre/", cleared.CpPageWebEn);
		Assert.Equal("", cleared.CpPhotoWeb);
		Assert.Equal("", cleared.CpFonctionEn);
		Assert.Equal("", cleared.CpParcoursFr);
		Assert.Equal("New bio", cleared.CpParcoursEn);
		Assert.Equal("", cleared.CpLinkedIn);
		Assert.Equal("", cleared.CpGoogle);
		Assert.Equal("", cleared.CpCarteVirtuelle);
	}

	[Fact]
	public void Self_keeps_personal_data_but_never_credentials()
	{
		var cp = FullRow();
		ConseillersPersonnelsController.StripForNonAdmin(cp, isSelf: true);

		Assert.Null(cp.CpMotDePasse);
		Assert.Null(cp.CpMelMotDePasse);
		Assert.Null(cp.CpAutoLogin);
		Assert.Null(cp.CpCalendrierCal);
		Assert.Equal("anna@perso.fr", cp.CpMelPerso);
		Assert.Equal("https://www.linkedin.com/in/anna", cp.CpLinkedIn);
		Assert.Equal("1 rue X", cp.CpAdresse);
		Assert.Equal("LinkedIn", cp.CpOrigine);
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
	[InlineData("CP_LinkedIn eq 'x'")]
	[InlineData("CP_googleBusinessProfile eq 'x'")]
	[InlineData("CP_Google eq 'x'")]
	[InlineData("CP_CalendrierCal eq 'x'")]
	[InlineData("CP_Origine eq 'LinkedIn'")]
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
