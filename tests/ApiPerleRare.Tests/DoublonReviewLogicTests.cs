using System.Collections.Generic;
using ApiPerleRare.Application.Doublons;
using Xunit;

namespace ApiPerleRare.Tests;

public class DoublonReviewLogicTests
{
	[Fact]
	public void CanonicalPairKey_is_order_independent_and_rejects_self_or_empty()
	{
		Assert.Equal("aaa|bbb", DoublonReviewLogic.CanonicalPairKey("aaa", "bbb"));
		Assert.Equal("aaa|bbb", DoublonReviewLogic.CanonicalPairKey("bbb", "aaa"));
		Assert.Equal("aaa|bbb", DoublonReviewLogic.CanonicalPairKey(" bbb ", " aaa "));
		Assert.Equal(string.Empty, DoublonReviewLogic.CanonicalPairKey("aaa", "aaa"));
		Assert.Equal(string.Empty, DoublonReviewLogic.CanonicalPairKey("", "bbb"));
		Assert.Equal(string.Empty, DoublonReviewLogic.CanonicalPairKey(null, "bbb"));
	}

	[Theory]
	[InlineData("confirmed", true, "confirmed")]
	[InlineData(" CONFIRMED ", true, "confirmed")]
	[InlineData("rejected", true, "rejected")]
	[InlineData("Rejected", true, "rejected")]
	[InlineData("skip", false, null)]
	[InlineData("", false, null)]
	[InlineData(null, false, null)]
	public void TryNormalizeDecision_only_accepts_confirmed_or_rejected(string raw, bool ok, string expected)
	{
		Assert.Equal(ok, DoublonReviewLogic.TryNormalizeDecision(raw, out string normalized));
		Assert.Equal(expected, normalized);
	}

	[Fact]
	public void ScoreCandidate_ignores_normal_multi_agency_pair_and_flags_suspicious_differences()
	{
		// Paire normale (même CP, surface 52 vs 54 m² = 3.7%, prix 665k vs 680k = 2.2%, 2p vs 2p)
		DoublonReviewLogic.SuspiciousCandidate normal = new(
			"dup1", "master1", 52, 54, 665000, 680000, 2, 2, "75001", "75001", "Appartement", "Appartement");
		Assert.Null(DoublonReviewLogic.ScoreCandidate(normal));

		// Paire suspecte : surface 173 vs 309 m² (44%) + prix 3.78M vs 5.595M (32%) + 5 vs 8 pièces
		DoublonReviewLogic.SuspiciousCandidate suspect = new(
			"dup2", "master2", 309, 173, 5595000, 3780000, 8, 5, "75001", "75001", "Appartement", "Appartement");
		DoublonReviewLogic.ScoredPair scored = DoublonReviewLogic.ScoreCandidate(suspect);
		Assert.NotNull(scored);
		Assert.True(scored.Score >= 100);
		Assert.Contains(scored.Reasons, (string r) => r.Contains("Surface"));
		Assert.Contains(scored.Reasons, (string r) => r.Contains("Prix"));
		Assert.Contains(scored.Reasons, (string r) => r.Contains("Pièces"));
	}

	[Fact]
	public void BuildReviewQueue_sorts_most_suspicious_first_and_excludes_reviewed_pairs()
	{
		List<DoublonReviewLogic.SuspiciousCandidate> candidates = new()
		{
			// Uniquement 2 pièces d'écart -> score 30
			new("d1", "m1", 80, 82, 900000, 910000, 5, 3, "75008", "75008", "Appartement", "Appartement"),
			// CP différents -> score 100
			new("d2", "m2", 80, 80, 900000, 900000, 3, 3, "75008", "75017", "Appartement", "Appartement"),
			// Déjà revue -> doit être exclue
			new("d3", "m3", 120, 60, 1200000, 600000, 5, 2, "75001", "75002", "Appartement", "Maison"),
		};

		HashSet<string> reviewed = new() { DoublonReviewLogic.CanonicalPairKey("d3", "m3") };
		List<DoublonReviewLogic.ScoredPair> queue = DoublonReviewLogic.BuildReviewQueue(candidates, reviewed);

		Assert.Equal(2, queue.Count);
		Assert.Equal("m2", queue[0].MasterId);
		Assert.Equal("m1", queue[1].MasterId);
	}

	[Fact]
	public void Sql_constants_target_active_properties_and_safe_unlink()
	{
		Assert.Contains("p.P_State = 1", DoublonReviewLogic.SelectSuspiciousPairsBaseSql);
		Assert.Contains("m.P_State = 1", DoublonReviewLogic.SelectSuspiciousPairsBaseSql);
		Assert.Contains("SET P_DoublonPropertyId = NULL", DoublonReviewLogic.UnlinkRejectedPairSql);
		Assert.DoesNotContain("DELETE", DoublonReviewLogic.UnlinkRejectedPairSql);
	}
}
