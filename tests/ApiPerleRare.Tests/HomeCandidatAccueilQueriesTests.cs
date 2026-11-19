using System;
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

	[Fact]
	public void Events_tab_lists_every_active_event_from_today_including_mail_in_511()
	{
		DateTime today = new DateTime(2026, 11, 19);

		Assert.True(HomeCandidatAccueilQueries.ListedOnCandidatEventsTab(
			"Candidat", "MAIL IN", 1, new DateTime(2026, 11, 19, 8, 50, 0), today));
		Assert.True(HomeCandidatAccueilQueries.ListedOnCandidatEventsTab(
			"Candidat", "RV TEL CANDIDAT", 1, new DateTime(2026, 11, 20, 15, 30, 0), today));
		Assert.True(HomeCandidatAccueilQueries.ListedOnCandidatEventsTab(
			"Candidat", "CANDIDAT ACTIF > CANDIDAT MORT", 1, new DateTime(2026, 11, 19, 9, 0, 0), today));
		Assert.False(HomeCandidatAccueilQueries.ListedOnCandidatEventsTab(
			"Candidat", "MAIL IN", 0, new DateTime(2026, 11, 19, 8, 50, 0), today));
		Assert.False(HomeCandidatAccueilQueries.ListedOnCandidatEventsTab(
			"Candidat", "MAIL IN", 1, new DateTime(2026, 11, 18, 18, 0, 0), today));
		Assert.False(HomeCandidatAccueilQueries.ListedOnCandidatEventsTab(
			"Candidat", "MAIL IN", 1, null, today));
		Assert.False(HomeCandidatAccueilQueries.ListedOnCandidatEventsTab(
			"Agent commercial", "RV AGENT CO", 1, new DateTime(2026, 11, 19, 11, 0, 0), today));
	}

	[Fact]
	public void Life_event_stays_listed_after_the_person_leaves_candidat()
	{
		DateTime today = new DateTime(2026, 11, 19);
		DateTime now = new DateTime(2026, 11, 19, 10, 0, 0);

		Assert.True(HomeCandidatAccueilQueries.ListedOnCandidatEventsTab(
			"Agent commercial", "CANDIDAT ACTIF > CONSEILLER ACTIF", 1, now, today));
		Assert.True(HomeCandidatAccueilQueries.ListedOnCandidatEventsTab(
			"Agent commercial", "CONSEILLER ACTIF > CANDIDAT ACTIF", 1, now, today));
		Assert.False(HomeCandidatAccueilQueries.ListedOnCandidatEventsTab(
			"Agent commercial", "CONSEILLER ACTIF > CONSEILLER MORT", 1, now, today));
		Assert.False(HomeCandidatAccueilQueries.IsCandidatLifeEventType(null));
		Assert.False(HomeCandidatAccueilQueries.IsCandidatLifeEventType("MAIL IN"));
	}
}
