using System;
using System.Collections.Generic;

namespace ApiPerleRare.Application.Doublons;

/// <summary>
/// Logique pure de l'outil Admin de revue des doublons suspects (Lot 3) :
/// - Clé canonique ordonnée (A &lt; B) pour une paire de biens.
/// - Calcul du score de suspicion et des motifs lisibles (surface, prix, pièces, CP, type).
/// - Validation stricte des décisions (<c>confirmed</c> / <c>rejected</c>).
/// </summary>
internal static class DoublonReviewLogic
{
	internal const string DecisionConfirmed = "confirmed";

	internal const string DecisionRejected = "rejected";

	internal const double SurfaceDiffRatioThreshold = 0.15;

	internal const double PriceDiffRatioThreshold = 0.20;

	internal const int RoomsDiffThreshold = 2;

	internal const string SelectSuspiciousPairsBaseSql = @"
SELECT
  p.P_PropertyId AS dup_id,
  m.P_PropertyId AS master_id,
  p.P_Surface AS dup_surf,
  m.P_Surface AS master_surf,
  p.P_Prix AS dup_prix,
  m.P_Prix AS master_prix,
  p.P_NbPieces AS dup_pieces,
  m.P_NbPieces AS master_pieces,
  p.P_Cp AS dup_cp,
  m.P_Cp AS master_cp,
  p.P_Type AS dup_type,
  m.P_Type AS master_type
FROM property p
INNER JOIN property m ON m.P_PropertyId = p.P_DoublonPropertyId
WHERE p.P_State = 1 AND (p.P_DateFin IS NULL OR p.P_DateFin < '1000-01-01')
  AND m.P_State = 1 AND (m.P_DateFin IS NULL OR m.P_DateFin < '1000-01-01')
  AND (
      (p.P_Surface > 0 AND m.P_Surface > 0 AND ABS(CAST(p.P_Surface AS SIGNED) - CAST(m.P_Surface AS SIGNED)) / GREATEST(p.P_Surface, m.P_Surface) > 0.15)
      OR (p.P_Prix > 0 AND m.P_Prix > 0 AND ABS(CAST(p.P_Prix AS SIGNED) - CAST(m.P_Prix AS SIGNED)) / GREATEST(p.P_Prix, m.P_Prix) > 0.20)
      OR (p.P_Cp IS NOT NULL AND m.P_Cp IS NOT NULL AND p.P_Cp <> '' AND m.P_Cp <> '' AND p.P_Cp <> m.P_Cp)
      OR (p.P_NbPieces > 0 AND m.P_NbPieces > 0 AND ABS(CAST(p.P_NbPieces AS SIGNED) - CAST(m.P_NbPieces AS SIGNED)) >= 2)
      OR (p.P_Type IS NOT NULL AND m.P_Type IS NOT NULL AND p.P_Type <> '' AND m.P_Type <> '' AND p.P_Type <> m.P_Type)
  )";

	internal const string SelectAllReviewsSql =
		"SELECT PDR_PropertyIdA, PDR_PropertyIdB, PDR_Decision FROM property_doublon_review";

	internal const string SelectRejectedReviewsSql =
		"SELECT PDR_PropertyIdA, PDR_PropertyIdB FROM property_doublon_review WHERE PDR_Decision = 'rejected'";

	internal const string UpsertReviewSql = @"
INSERT INTO property_doublon_review
  (PDR_PropertyIdA, PDR_PropertyIdB, PDR_Decision, PDR_Reason, PDR_ReviewedBy, PDR_ReviewedAt)
VALUES
  (@idA, @idB, @decision, @reason, @reviewedBy, @reviewedAt)
ON DUPLICATE KEY UPDATE
  PDR_Decision = VALUES(PDR_Decision),
  PDR_Reason = VALUES(PDR_Reason),
  PDR_ReviewedBy = VALUES(PDR_ReviewedBy),
  PDR_ReviewedAt = VALUES(PDR_ReviewedAt)";

	internal const string UnlinkRejectedPairSql = @"
UPDATE property
SET P_DoublonPropertyId = NULL
WHERE (P_PropertyId = @idA AND P_DoublonPropertyId = @idB)
   OR (P_PropertyId = @idB AND P_DoublonPropertyId = @idA)";

	internal sealed record SuspiciousCandidate(
		string DupId,
		string MasterId,
		double? DupSurface,
		double? MasterSurface,
		double? DupPrix,
		double? MasterPrix,
		int? DupPieces,
		int? MasterPieces,
		string DupCp,
		string MasterCp,
		string DupType,
		string MasterType);

	internal sealed record ScoredPair(
		string MasterId,
		string DupId,
		string CanonicalIdA,
		string CanonicalIdB,
		int Score,
		IReadOnlyList<string> Reasons);

	internal static bool TryCanonicalizePair(string id1, string id2, out string idA, out string idB)
	{
		idA = null;
		idB = null;
		string a = id1?.Trim();
		string b = id2?.Trim();
		if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || string.Equals(a, b, StringComparison.Ordinal))
		{
			return false;
		}
		if (string.CompareOrdinal(a, b) < 0)
		{
			idA = a;
			idB = b;
		}
		else
		{
			idA = b;
			idB = a;
		}
		return true;
	}

	internal static string CanonicalPairKey(string id1, string id2)
	{
		return TryCanonicalizePair(id1, id2, out string a, out string b)
			? a + "|" + b
			: string.Empty;
	}

	internal static bool TryNormalizeDecision(string raw, out string normalized)
	{
		normalized = null;
		if (string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}
		string trimmed = raw.Trim().ToLowerInvariant();
		if (trimmed == DecisionConfirmed || trimmed == DecisionRejected)
		{
			normalized = trimmed;
			return true;
		}
		return false;
	}

	internal static ScoredPair ScoreCandidate(SuspiciousCandidate c)
	{
		if (c == null || !TryCanonicalizePair(c.MasterId, c.DupId, out string idA, out string idB))
		{
			return null;
		}

		int score = 0;
		List<string> reasons = new List<string>();

		string cp1 = c.MasterCp?.Trim();
		string cp2 = c.DupCp?.Trim();
		if (!string.IsNullOrEmpty(cp1) && !string.IsNullOrEmpty(cp2) && !string.Equals(cp1, cp2, StringComparison.OrdinalIgnoreCase))
		{
			score += 100;
			reasons.Add($"CP différents ({cp1} vs {cp2})");
		}

		string t1 = c.MasterType?.Trim();
		string t2 = c.DupType?.Trim();
		if (!string.IsNullOrEmpty(t1) && !string.IsNullOrEmpty(t2) && !string.Equals(t1, t2, StringComparison.OrdinalIgnoreCase))
		{
			score += 90;
			reasons.Add($"Types différents ({t1} vs {t2})");
		}

		if (c.MasterSurface.HasValue && c.DupSurface.HasValue && c.MasterSurface.Value > 0 && c.DupSurface.Value > 0)
		{
			double maxS = Math.Max(c.MasterSurface.Value, c.DupSurface.Value);
			double ratio = Math.Abs(c.MasterSurface.Value - c.DupSurface.Value) / maxS;
			if (ratio > SurfaceDiffRatioThreshold)
			{
				int pct = (int)Math.Round(ratio * 100.0);
				score += pct;
				reasons.Add($"Surface ±{pct} % ({Math.Round(c.MasterSurface.Value)} vs {Math.Round(c.DupSurface.Value)} m²)");
			}
		}

		if (c.MasterPrix.HasValue && c.DupPrix.HasValue && c.MasterPrix.Value > 0 && c.DupPrix.Value > 0)
		{
			double maxP = Math.Max(c.MasterPrix.Value, c.DupPrix.Value);
			double ratio = Math.Abs(c.MasterPrix.Value - c.DupPrix.Value) / maxP;
			if (ratio > PriceDiffRatioThreshold)
			{
				int pct = (int)Math.Round(ratio * 100.0);
				score += pct;
				reasons.Add($"Prix ±{pct} %");
			}
		}

		if (c.MasterPieces.HasValue && c.DupPieces.HasValue && c.MasterPieces.Value > 0 && c.DupPieces.Value > 0)
		{
			int diff = Math.Abs(c.MasterPieces.Value - c.DupPieces.Value);
			if (diff >= RoomsDiffThreshold)
			{
				score += diff * 15;
				reasons.Add($"Pièces ±{diff} ({c.MasterPieces.Value} vs {c.DupPieces.Value} p.)");
			}
		}

		if (reasons.Count == 0)
		{
			return null;
		}

		return new ScoredPair(c.MasterId.Trim(), c.DupId.Trim(), idA, idB, score, reasons);
	}

	internal static List<ScoredPair> BuildReviewQueue(
		IEnumerable<SuspiciousCandidate> candidates,
		IReadOnlySet<string> alreadyReviewedKeys)
	{
		List<ScoredPair> list = new List<ScoredPair>();
		if (candidates == null)
		{
			return list;
		}
		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
		foreach (SuspiciousCandidate c in candidates)
		{
			ScoredPair scored = ScoreCandidate(c);
			if (scored == null)
			{
				continue;
			}
			string key = scored.CanonicalIdA + "|" + scored.CanonicalIdB;
			if (alreadyReviewedKeys != null && alreadyReviewedKeys.Contains(key))
			{
				continue;
			}
			if (!seen.Add(key))
			{
				continue;
			}
			list.Add(scored);
		}
		list.Sort((ScoredPair a, ScoredPair b) =>
		{
			int cmp = b.Score.CompareTo(a.Score);
			if (cmp != 0)
			{
				return cmp;
			}
			cmp = string.CompareOrdinal(a.CanonicalIdA, b.CanonicalIdA);
			return cmp != 0 ? cmp : string.CompareOrdinal(a.CanonicalIdB, b.CanonicalIdB);
		});
		return list;
	}
}
