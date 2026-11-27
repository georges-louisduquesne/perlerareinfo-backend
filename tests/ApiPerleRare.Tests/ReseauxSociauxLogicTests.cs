using System;
using System.Collections.Generic;
using System.Linq;
using ApiPerleRare.Application.Reseaux;
using Xunit;

namespace ApiPerleRare.Tests;

public class ReseauxSociauxLogicTests
{
	[Fact]
	public void Template_replaces_conseiller_and_url()
	{
		string message = ReseauxSociauxLogic.ApplyTemplate(
			"Bonjour {prenom} {nom}, {url} — {prenom}",
			"Diane",
			"de Monteynard",
			"https://www.linkedin.com/posts/perle-rare");
		Assert.Equal("Bonjour Diane de Monteynard, https://www.linkedin.com/posts/perle-rare — Diane", message);
		Assert.Equal(string.Empty, ReseauxSociauxLogic.ApplyTemplate(null, null, null, null));
	}

	[Fact]
	public void First_quota_fills_the_week_balance()
	{
		Assert.Equal((3, 3), ReseauxSociauxLogic.ApplyQuotaEdit(0, 0, 3, 0));
		Assert.Equal((4, 1), ReseauxSociauxLogic.ApplyQuotaEdit(3, 1, 4, 1));
		Assert.Equal((2, 2), ReseauxSociauxLogic.ApplyQuotaEdit(4, 3, 2, 3));
		Assert.Equal((100, 100), ReseauxSociauxLogic.ApplyQuotaEdit(0, 0, 500, 0));
		Assert.Equal((0, 0), ReseauxSociauxLogic.ApplyQuotaEdit(2, 1, -4, -1));
	}

	[Fact]
	public void Eligible_conseillers_keep_a_balance_and_skip_an_existing_pair()
	{
		var rows = new[]
		{
			new ReseauxConseillerPick { Ref = 1, Prenom = "Zoé", Nom = "Martin", Solde = 1 },
			new ReseauxConseillerPick { Ref = 2, Prenom = "Anne", Nom = "Bernard", Solde = 4 },
			new ReseauxConseillerPick { Ref = 3, Prenom = "Anne", Nom = "Albert", Solde = 4 },
			new ReseauxConseillerPick { Ref = 4, Prenom = "Paul", Nom = "Durand", Solde = 0 },
		};
		var pairs = new[] { ("post-1", ReseauxSociauxLogic.LinkedIn, 2u) };
		var eligible = ReseauxSociauxLogic.Eligible(rows, "post-1", ReseauxSociauxLogic.LinkedIn, pairs);
		Assert.Equal(new uint[] { 3, 1 }, eligible.Select((ReseauxConseillerPick c) => c.Ref).ToArray());
		Assert.True(ReseauxSociauxLogic.CanSolicit(1, alreadySolicited: false));
		Assert.False(ReseauxSociauxLogic.CanSolicit(1, alreadySolicited: true));
		Assert.False(ReseauxSociauxLogic.CanSolicit(0, alreadySolicited: false));
	}

	[Fact]
	public void Week_resets_on_monday_and_the_first_sight_only_records()
	{
		Assert.Equal(202601, ReseauxSociauxLogic.IsoWeekKey(new DateTime(2026, 1, 1)));
		Assert.Equal(202601, ReseauxSociauxLogic.IsoWeekKey(new DateTime(2025, 12, 29)));
		Assert.Equal(202602, ReseauxSociauxLogic.IsoWeekKey(new DateTime(2026, 1, 5)));
		int monday = ReseauxSociauxLogic.IsoWeekKey(new DateTime(2026, 11, 23));
		int sunday = ReseauxSociauxLogic.IsoWeekKey(new DateTime(2026, 11, 22, 23, 59, 0));
		Assert.NotEqual(sunday, monday);
		Assert.Equal(ReseauxResetDecision.RecordOnly, ReseauxSociauxLogic.DecideReset(0, monday));
		Assert.Equal(ReseauxResetDecision.None, ReseauxSociauxLogic.DecideReset(monday, monday));
		Assert.Equal(ReseauxResetDecision.Reset, ReseauxSociauxLogic.DecideReset(sunday, monday));
	}

	[Fact]
	public void Sync_is_stale_until_a_recent_stamp_exists()
	{
		Assert.True(ReseauxSociauxLogic.SyncIsStale(0, 1_700_000_000));
		Assert.True(ReseauxSociauxLogic.SyncIsStale(1_700_000_000, 1_700_000_000 + ReseauxSociauxLogic.SyncStaleSeconds));
		Assert.False(ReseauxSociauxLogic.SyncIsStale(1_700_000_000, 1_700_000_000 + 60));
	}

	[Fact]
	public void Parsers_keep_the_six_latest_posts_from_both_networks()
	{
		string linkedIn = """
		[
		  {"id":"7531696806913568768","linkedinUrl":"https://www.linkedin.com/posts/perle-rare_hiring","content":"Nous recrutons","postedAt":{"date":"2026-11-26T12:34:10.928Z"},"postImages":[]},
		  {"id":"2","linkedinUrl":"https://www.linkedin.com/posts/deux","content":"Portrait","postedAt":{"date":"2026-11-23T07:02:14.622Z"},"postImages":[{"url":"https://media.licdn.com/image.jpg","width":940,"height":788}]}
		]
		""";
		string instagram = """
		[
		  {"id":"4038447322246164554","url":"https://www.instagram.com/p/DgLcnFmnDBK/","timestamp":"2026-11-25T21:01:22.000Z","displayUrl":"https://cdn.example/ig.jpg","caption":"L'art du détail"}
		]
		""";
		var linked = ReseauxSociauxLogic.ParseLinkedIn(linkedIn);
		var insta = ReseauxSociauxLogic.ParseInstagram(instagram);
		Assert.Equal("https://media.licdn.com/image.jpg", linked[1].Vignette);
		Assert.Equal(string.Empty, linked[0].Vignette);
		Assert.Equal("https://cdn.example/ig.jpg", insta[0].Vignette);
		var latest = ReseauxSociauxLogic.Latest(linked.Concat(insta), 2);
		Assert.Equal(new[] { "7531696806913568768", "4038447322246164554" }, latest.Select((ReseauPost p) => p.Id).ToArray());
		Assert.Empty(ReseauxSociauxLogic.ParseLinkedIn("not-json"));
		Assert.Empty(ReseauxSociauxLogic.ParseInstagram(null));
	}

	[Fact]
	public void Latest_sollicitations_are_the_newest_fifty()
	{
		var rows = new[]
		{
			new ReseauSollicitationPick { Id = 1, DateEnvoi = new DateTime(2026, 11, 20, 9, 0, 0) },
			new ReseauSollicitationPick { Id = 3, DateEnvoi = new DateTime(2026, 11, 26, 18, 5, 0) },
			new ReseauSollicitationPick { Id = 2, DateEnvoi = new DateTime(2026, 11, 26, 18, 5, 0) },
			new ReseauSollicitationPick { Id = 4, DateEnvoi = new DateTime(2026, 11, 24, 11, 30, 0) },
		};
		var latest = ReseauxSociauxLogic.LatestSollicitations(rows, 2);
		Assert.Equal(new uint[] { 3, 2 }, latest.Select((ReseauSollicitationPick row) => row.Id).ToArray());
		var many = Enumerable.Range(1, 51).Select((int i) => new ReseauSollicitationPick
		{
			Id = (uint)i,
			DateEnvoi = new DateTime(2026, 1, 1).AddMinutes(i)
		}).ToArray();
		var capped = ReseauxSociauxLogic.LatestSollicitations(many, 0);
		Assert.Equal(50, capped.Count);
		Assert.Equal(51u, capped[0].Id);
		Assert.Empty(ReseauxSociauxLogic.LatestSollicitations(null, 50));
	}

	[Fact]
	public void A_candidate_is_not_an_active_advisor()
	{
		Assert.False(ReseauxSociauxLogic.IsConseillerReseaux(1, "Candidat"));
		Assert.False(ReseauxSociauxLogic.IsConseillerReseaux(1, " candidat "));
		Assert.False(ReseauxSociauxLogic.IsConseillerReseaux(1, "CANDIDAT"));
		Assert.True(ReseauxSociauxLogic.IsConseillerReseaux(1, "Agent commercial"));
		Assert.True(ReseauxSociauxLogic.IsConseillerReseaux(1, "Associé"));
		Assert.True(ReseauxSociauxLogic.IsConseillerReseaux(1, "Qualité"));
		Assert.True(ReseauxSociauxLogic.IsConseillerReseaux(1, null));
		Assert.False(ReseauxSociauxLogic.IsConseillerReseaux(0, "Agent commercial"));
		Assert.True(ReseauxSociauxLogic.IsConseillerId(0));
		Assert.True(ReseauxSociauxLogic.IsConseillerId(12));
		Assert.False(ReseauxSociauxLogic.IsConseillerId(-1));
		Assert.Contains("<> 'candidat'", ReseauxSociauxLogic.ConseillerActifPredicate, StringComparison.Ordinal);
	}

	[Fact]
	public void Daily_volumes_cover_today_through_six_days_ago()
	{
		DateTime today = new DateTime(2026, 11, 27, 12, 30, 0);
		var envois = new[]
		{
			new ReseauEnvoiJour { Ref = 0, DateEnvoi = new DateTime(2026, 11, 27, 9, 0, 0) },
			new ReseauEnvoiJour { Ref = 0, DateEnvoi = new DateTime(2026, 11, 27, 18, 0, 0) },
			new ReseauEnvoiJour { Ref = 0, DateEnvoi = new DateTime(2026, 11, 26, 23, 59, 0) },
			new ReseauEnvoiJour { Ref = 0, DateEnvoi = new DateTime(2026, 11, 21, 0, 0, 0) },
			new ReseauEnvoiJour { Ref = 0, DateEnvoi = new DateTime(2026, 11, 20, 23, 59, 0) },
			new ReseauEnvoiJour { Ref = 0, DateEnvoi = new DateTime(2026, 11, 28, 0, 1, 0) },
			new ReseauEnvoiJour { Ref = 4, DateEnvoi = new DateTime(2026, 11, 25, 8, 0, 0) },
			null,
		};
		Dictionary<uint, int[]> volumes = ReseauxSociauxLogic.VolumesParJour(envois, today);
		Assert.Equal(new[] { 2, 1, 0, 0, 0, 0, 1 }, volumes[0]);
		Assert.Equal(new[] { 0, 0, 1, 0, 0, 0, 0 }, ReseauxSociauxLogic.JoursDe(volumes, 4));
		Assert.Equal(new int[ReseauxSociauxLogic.JoursVolume], ReseauxSociauxLogic.JoursDe(volumes, 9));
		Assert.Empty(ReseauxSociauxLogic.VolumesParJour(null, today));
	}

	[Fact]
	public void Solicitation_window_covers_today_and_the_fifty_nine_days_before()
	{
		Assert.Equal(60, ReseauxSociauxLogic.SollicitationsFenetreJours);
		Assert.Equal(new DateTime(2026, 9, 29), ReseauxSociauxLogic.FenetreDepuis(new DateTime(2026, 11, 27, 14, 30, 0)));
		Assert.Equal(new DateTime(2026, 9, 29), ReseauxSociauxLogic.FenetreDepuis(new DateTime(2026, 11, 27, 0, 0, 0)));
	}

	[Fact]
	public void Remaining_balance_sums_positive_soldes_only()
	{
		Assert.Equal(9, ReseauxSociauxLogic.SoldeRestant(new[] { 4, 5, 0 }));
		Assert.Equal(3, ReseauxSociauxLogic.SoldeRestant(new[] { 2, -3, 1 }));
		Assert.Equal(0, ReseauxSociauxLogic.SoldeRestant(null));
	}

	[Fact]
	public void Quota_reset_stays_on_unless_explicitly_off()
	{
		Assert.True(ReseauxQuotaReset.Enabled(null));
		Assert.True(ReseauxQuotaReset.Enabled("on"));
		Assert.False(ReseauxQuotaReset.Enabled("off"));
		Assert.False(ReseauxQuotaReset.Enabled(" OFF "));
	}
}
