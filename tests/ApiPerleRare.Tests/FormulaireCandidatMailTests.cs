using System;
using ApiPerleRare.Models;
using ApiPerleRare.ScheduledTasks;
using Xunit;

namespace ApiPerleRare.Tests;

public class FormulaireCandidatMailTests
{
	private const string PaulBody = """
Votre prénom ? Paul

Votre nom ? Renaud

Votre numtel ?

Votre email ?

Où habitez-vous ? Paris

Lien linkedIn :

Pourquoi rejoindre PR : Je souhaite rejoindre Perle Rare

Délai de remplissage du formulaire : 00:05:51
""";

	private const string JulienBody = """
Votre prénom ? Julien

Votre nom ? Diemer

Votre numtel ?  +33646087134

Votre email ? julien.diemer93@gmail.com

Où habitez-vous ? Paris 16

Lien linkedIn : https://www.linkedin.com/in/julien-diemer

Pourquoi rejoindre PR : Chez Engel & Völkers, j’ai compris.

Délai de remplissage du formulaire : 00:03:35
""";

	[Fact]
	public void Parse_reads_mail_columns_including_empty_phone()
	{
		FormulairesCandidats form = FormulaireCandidatMail.Parse("<id-1>", new DateTime(2026, 10, 21, 18, 48, 14), "from", FormulaireCandidatMail.SubjectMarker, PaulBody);
		Assert.NotNull(form);
		Assert.Equal("Paul", form.FcPrenom);
		Assert.Equal("Renaud", form.FcNom);
		Assert.Equal("", form.FcTel);
		Assert.Equal("", form.FcEmail);
		Assert.Equal("Paris", form.FcVille);
		Assert.Equal("", form.FcLinkedin);
		Assert.Equal("Je souhaite rejoindre Perle Rare", form.FcPourquoi);
		Assert.Equal("00:05:51", form.FcDelai);
		Assert.Equal(FormulaireCandidatMail.StatutAAttribuer, form.FcStatut);
	}

	[Fact]
	public void Parse_reads_phone_email_and_linkedin()
	{
		FormulairesCandidats form = FormulaireCandidatMail.Parse("<id-2>", new DateTime(2026, 10, 27, 9, 48, 52), "from", FormulaireCandidatMail.SubjectMarker, JulienBody);
		Assert.Equal("+33646087134", form.FcTel.Trim());
		Assert.Equal("julien.diemer93@gmail.com", form.FcEmail);
		Assert.Equal("Paris 16", form.FcVille);
		Assert.Equal("https://www.linkedin.com/in/julien-diemer", form.FcLinkedin);
		Assert.StartsWith("Chez Engel", form.FcPourquoi);
	}

	[Fact]
	public void Parse_accepts_html_body()
	{
		string html = "<pre>Votre prénom ? Paul <br>Votre nom ? Renaud <br>Votre numtel ?  <br></pre>";
		FormulairesCandidats form = FormulaireCandidatMail.Parse("<id>", DateTime.Now, "from", FormulaireCandidatMail.SubjectMarker, html);
		Assert.Equal("Paul", form.FcPrenom);
		Assert.Equal("Renaud", form.FcNom);
	}

	[Fact]
	public void Subject_ignores_replies()
	{
		Assert.True(FormulaireCandidatMail.IsRecruitmentSubject(FormulaireCandidatMail.SubjectMarker));
		Assert.False(FormulaireCandidatMail.IsRecruitmentSubject("Re: " + FormulaireCandidatMail.SubjectMarker));
		Assert.False(FormulaireCandidatMail.IsRecruitmentSubject("Autre sujet"));
	}

	[Fact]
	public void Cause_rejet_is_one_of_three()
	{
		Assert.Equal("Doublon", FormulaireCandidatMail.CanonicalCause("doublon"));
		Assert.Equal("Spam", FormulaireCandidatMail.CanonicalCause("Spam"));
		Assert.Equal("Autre", FormulaireCandidatMail.CanonicalCause("autre"));
		Assert.Null(FormulaireCandidatMail.CanonicalCause("Hors secteur"));
	}

	[Fact]
	public void Attribution_creates_candidat_mail_in_and_call_task()
	{
		FormulairesCandidats form = FormulaireCandidatMail.Parse("<id-2>", new DateTime(2026, 10, 27, 9, 48, 52), "from", FormulaireCandidatMail.SubjectMarker, JulienBody);
		ConseillersPersonnels created = FormulaireCandidatAttribution.CreateConseiller(form, new DateOnly(2026, 10, 27));
		Assert.Equal("Candidat", created.CpStatut);
		Assert.True(created.CpAdmin);
		Assert.Equal((sbyte)1, created.CpActif);
		Assert.Equal("Julien", created.CpPrenom);
		Assert.Equal("Diemer", created.CpNomFamille);
		Assert.Equal("0646087134", created.CpTelPersonnel);
		Assert.Equal("julien.diemer93@gmail.com", created.CpMelPerso);
		Assert.Equal("Paris 16", created.CpVille);
		Assert.StartsWith("https://www.linkedin.com/in/julien-diemer", created.CpCommentaire);
		Assert.Contains("Chez Engel", created.CpCommentaire);
		Assert.True(created.CpCommentaire.Length <= 2000);

		ConseillersPersonnelsEvenements ev = FormulaireCandidatAttribution.CreateEvent(form, 12);
		Assert.Equal("MAIL IN", ev.CpeTypeEvenement);
		Assert.Equal("formulaire réseaux sociaux", ev.CpeCommentaire);
		Assert.Equal(12u, ev.CpeRefConseiller);

		ConseillersPersonnelsTaches task = FormulaireCandidatAttribution.CreateTask(12, "CTHIERIET", new DateTime(2026, 10, 27, 10, 0, 0));
		Assert.Equal("A appeler", task.CptType);
		Assert.Equal("CANDIDAT", task.CptCom);
		Assert.Equal("CTHIERIET", task.CptQui);
		Assert.Equal("", task.CptEtat);
		Assert.Equal(new DateOnly(2026, 10, 27), task.CptDateRealisation);
	}

	[Fact]
	public void Call_date_skips_evening_weekend_and_holiday()
	{
		Assert.Equal(new DateOnly(2026, 4, 5), FormulaireCandidatAttribution.Easter(2026));
		Assert.Equal(new DateOnly(2026, 10, 28), FormulaireCandidatAttribution.CallDate(new DateTime(2026, 10, 27, 18, 0, 0)));
		Assert.Equal(new DateOnly(2026, 11, 2), FormulaireCandidatAttribution.CallDate(new DateTime(2026, 10, 31, 10, 0, 0)));
		Assert.True(FormulaireCandidatAttribution.IsClosed(new DateOnly(2026, 5, 1)));
	}

	[Fact]
	public void Recruteur_is_an_active_admin_who_is_not_a_candidate()
	{
		Assert.True(FormulaireCandidatAttribution.IsRecruteur(1, true, "Associé", "Céline", "Thieriet", "CTHIERIET"));
		Assert.True(FormulaireCandidatAttribution.IsRecruteur(1, true, "Qualité", "Marion", "Normand", "MNORMAND"));
		Assert.False(FormulaireCandidatAttribution.IsRecruteur(1, true, "Candidat", "Paul", "Renaud", "PRENAUD"));
		Assert.False(FormulaireCandidatAttribution.IsRecruteur(0, true, "Associé", "Nicolas", "Bavencoffe", "NBAVENCOFFE"));
		Assert.False(FormulaireCandidatAttribution.IsRecruteur(1, false, "Agent commercial", "Ada", "Martin", "AMARTIN"));
	}

	[Fact]
	public void Correspondance_matches_name_email_or_phone()
	{
		PersonSnapshot[] people = new PersonSnapshot[2]
		{
			new PersonSnapshot
			{
				Ref = 9,
				Kind = "conseiller",
				Prenom = "Paul",
				Nom = "Renaud",
				Statut = "Candidat",
				Emails = new string[1] { "autre@exemple.fr" },
				Tels = new string[1] { "0600000000" }
			},
			new PersonSnapshot
			{
				Ref = 4,
				Kind = "contact",
				Prenom = "Julien",
				Nom = "Autre",
				Statut = "PROSPECT ACTIF",
				Emails = new string[1] { "julien.diemer93@gmail.com" },
				Tels = new string[1] { "0646087134" }
			}
		};
		FormulaireCandidatCorrespondance[] byName = FormulaireCandidatMatches.Match("Renaud", "", "", people);
		Assert.Single(byName);
		Assert.Equal((uint?)9, byName[0].CRefConseiller);
		Assert.Null(byName[0].CRefContact);
		Assert.True(byName[0].NomIdentique);

		FormulaireCandidatCorrespondance[] byMail = FormulaireCandidatMatches.Match("Diemer", "julien.diemer93@gmail.com", "+33 6 46 08 71 34", people);
		Assert.Single(byMail);
		Assert.Equal((uint?)4, byMail[0].CRefContact);
		Assert.Null(byMail[0].CRefConseiller);
		Assert.True(byMail[0].EmailIdentique);
		Assert.True(byMail[0].TelIdentique);

		PersonSnapshot adminZero = new PersonSnapshot
		{
			Ref = 0,
			Kind = "conseiller",
			Prenom = "Georges-Louis",
			Nom = "Duquesne",
			Statut = "Associé"
		};
		FormulaireCandidatCorrespondance[] byAdmin = FormulaireCandidatMatches.Match("Duquesne", "", "", new PersonSnapshot[1] { adminZero });
		Assert.Equal((uint?)0, byAdmin[0].CRefConseiller);
		Assert.Null(byAdmin[0].CRefContact);
	}

	[Fact]
	public void Commentaire_fits_latin1_column()
	{
		FormulairesCandidats form = new FormulairesCandidats
		{
			FcPrenom = "Paul",
			FcNom = "Renaud",
			FcTel = "",
			FcEmail = "",
			FcVille = "Paris",
			FcLinkedin = "",
			FcPourquoi = "J\u2019ai envie \u00F6 " + new string('x', 2500)
		};
		ConseillersPersonnels created = FormulaireCandidatAttribution.CreateConseiller(form, new DateOnly(2026, 10, 27));
		Assert.StartsWith("J'ai envie", created.CpCommentaire);
		Assert.Equal(2000, created.CpCommentaire.Length);
		Assert.Contains("\u00F6", created.CpCommentaire);
	}
}
