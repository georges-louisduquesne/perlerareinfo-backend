using System;
using ApiPerleRare.ScheduledTasks;
using Xunit;

namespace ApiPerleRare.Tests;

public class TransactionEventMailTests
{
	[Theory]
	[InlineData(null, false)]
	[InlineData("", false)]
	[InlineData("off", false)]
	[InlineData(" ON ", true)]
	public void Job_runs_only_when_explicitly_on(string value, bool expected)
	{
		Assert.Equal(expected, TransactionEventMail.Enabled(value));
	}

	[Theory]
	[InlineData("OFFRE", true)]
	[InlineData("OFFRE ACCEPTEE", true)]
	[InlineData("OFFRE REFUSEE", true)]
	[InlineData("RV COMPROMIS", true)]
	[InlineData("RV ACTE AUTHENT.", true)]
	[InlineData("MAIL OUT", false)]
	[InlineData("offre", false)]
	[InlineData(null, false)]
	public void Only_the_five_transaction_types_are_watched(string eventType, bool expected)
	{
		Assert.Equal(expected, TransactionEventMail.Watches(eventType));
	}

	[Theory]
	[InlineData(1, "OFFRE", "OFFRE")]
	[InlineData(3, "OFFRE REFUSEE", "OFFRE REFUSEE")]
	[InlineData(0, "RV ACTE AUTHENT.", "RV ACTE AUTHENT.")]
	[InlineData(2, "OFFRE", "DEPOT DOSSIER")]
	[InlineData(4, "OFFRE", "DEPOT DOSSIER")]
	[InlineData(2, "OFFRE REFUSEE", "DOSSIER REFUSE")]
	[InlineData(4, "OFFRE ACCEPTEE", "DOSSIER ACCEPTE")]
	[InlineData(2, "RV ACTE AUTHENT.", "RV SIGNATURE BAIL")]
	[InlineData(4, "RV COMPROMIS", "RV COMPROMIS")]
	public void Rental_missions_rename_the_subject_label(int missionId, string eventType, string expected)
	{
		Assert.Equal(expected, TransactionEventMail.SubjectLabel(eventType, missionId));
	}

	[Fact]
	public void Subject_is_label_then_client_name()
	{
		Assert.Equal("OFFRE DAHAN", TransactionEventMail.Subject("OFFRE", 1, "DAHAN"));
		Assert.Equal("DEPOT DOSSIER BENMLIH", TransactionEventMail.Subject("OFFRE", 2, " BENMLIH "));
		Assert.Equal("RV COMPROMIS", TransactionEventMail.Subject("RV COMPROMIS", 1, "  "));
	}

	[Fact]
	public void Client_name_falls_back_to_the_family_name()
	{
		Assert.Equal("DUPONT", TransactionEventMail.ClientName("", " DUPONT "));
		Assert.Equal("RAVELEAU 2", TransactionEventMail.ClientName("RAVELEAU 2", "AUTRE"));
	}

	[Fact]
	public void Appointment_date_closes_the_subject_of_signing_meetings()
	{
		DateTime when = new DateTime(2026, 11, 6, 11, 0, 0);
		Assert.Equal("RV COMPROMIS DAHAN le 06/11/26 à 11:00", TransactionEventMail.Subject("RV COMPROMIS", 1, "DAHAN", when));
		Assert.Equal("RV SIGNATURE BAIL DAHAN le 06/11/26 à 11:00", TransactionEventMail.Subject("RV ACTE AUTHENT.", 2, "DAHAN", when));
		Assert.Equal("OFFRE DAHAN", TransactionEventMail.Subject("OFFRE", 1, "DAHAN", when));
		Assert.Equal("RV ACTE AUTHENT. le 23/10/26 à 00:00", TransactionEventMail.Subject("RV ACTE AUTHENT.", 1, "", new DateTime(2026, 10, 23, 0, 0, 0)));
	}

	[Fact]
	public void Person_name_is_first_name_then_uppercase_last_name()
	{
		Assert.Equal("Marie DUPONT", TransactionEventMail.PersonName(" Marie ", "dupont"));
		Assert.Equal("MARTIN", TransactionEventMail.PersonName(" ", "Martin"));
		Assert.Equal("", TransactionEventMail.PersonName(null, null));
	}

	[Fact]
	public void Body_is_the_comment_then_negotiator_and_advisor()
	{
		Assert.Equal("Offre 395000€ &lt; 430000€<br>suite<br>Négociateur : Marie DUPONT<br>Conseiller : Paul MARTIN", TransactionEventMail.HtmlBody("Offre 395000€ < 430000€\nsuite", "Marie DUPONT", "Paul MARTIN"));
		Assert.Equal("Négociateur : <br>Conseiller : ", TransactionEventMail.HtmlBody(null, null, "  "));
	}

	[Fact]
	public void A_failed_send_does_not_move_the_cursor()
	{
		Assert.Equal(10, TransactionEventMail.CursorAfterSend(10, 11, sent: false));
		Assert.Equal(11, TransactionEventMail.CursorAfterSend(10, 11, sent: true));
	}
}
