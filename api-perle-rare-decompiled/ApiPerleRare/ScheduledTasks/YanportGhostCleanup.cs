using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace ApiPerleRare.ScheduledTasks;

/// <summary>
/// Lot 1 — Nettoyage quotidien des biens actifs en base qui ont disparu (HTTP 404, fusionnés/supprimés)
/// ou ont déjà été fermés côté Yanport (<c>active = false</c> ou <c>publicationEndDate</c>).
/// Ne supprime jamais de ligne <c>property</c> et ne modifie pas <c>P_DateLastUpdate</c>.
/// </summary>
internal static class YanportGhostCleanup
{
	internal const string EnvVar = "PR_YANPORT_GHOST_CLEANUP";

	internal const string CursorParam = "YanportGhostCleanupDay";

	internal const string TaskLogName = "YanportGhostCleanup";

	internal const int RunHourParis = 4;

	internal const int BatchSize = 150;

	internal const int GracePeriodHours = 48;

	internal const int RattrapageCode = 5;

	internal const int AnomalyMinChecked = 500;

	internal const double MaxNotFoundRatioAbort = 0.35;

	internal const double MinBatchSuccessRatio = 0.95;

	internal const string SelectActiveSql =
		"SELECT P_PropertyId, P_DateLastUpdate "
		+ "FROM property "
		+ "WHERE P_State = 1 "
		+ "AND (P_DateFin IS NULL OR P_DateFin < '1000-01-01') "
		+ "AND IFNULL(P_EstRattrapage, 0) <> 4";

	internal const string CloseOneSql =
		"UPDATE property "
		+ "SET P_State = 0, "
		+ "    P_DateFin = @dateFin, "
		+ "    P_EstRattrapage = 5 "
		+ "WHERE P_PropertyId = @id "
		+ "  AND P_State = 1 "
		+ "  AND (P_DateFin IS NULL OR P_DateFin < '1000-01-01') "
		+ "  AND IFNULL(P_EstRattrapage, 0) <> 4";

	internal sealed record Candidate(string PropertyId, DateTime? DateLastUpdateParis);

	internal sealed record YanportStatus(bool Active, DateTime? PublicationEndDateParis);

	internal sealed record CloseDecision(string PropertyId, DateTime DateFinParis, string Reason);

	internal sealed class BatchClassification
	{
		public List<CloseDecision> ToCloseNotFound { get; } = new List<CloseDecision>();

		public List<CloseDecision> ToCloseEnded { get; } = new List<CloseDecision>();

		public int StillActive { get; set; }

		public int SkippedRecent404 { get; set; }
	}

	internal static bool Enabled(string value)
	{
		return string.Equals(value?.Trim(), "on", StringComparison.OrdinalIgnoreCase);
	}

	internal static TimeZoneInfo ParisZone()
	{
		return RecrawlStockMail.ParisZone();
	}

	internal static DateTime ToParisTime(DateTime utc)
	{
		DateTime utcKind = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
		return TimeZoneInfo.ConvertTimeFromUtc(utcKind, ParisZone());
	}

	internal static int DayKey(DateTime utc)
	{
		DateTime paris = ToParisTime(utc);
		return paris.Year * 10000 + paris.Month * 100 + paris.Day;
	}

	/// <summary>Une fois par jour calendaire de Paris, à partir de 4 h.</summary>
	internal static bool Due(int? lastDay, DateTime utc)
	{
		DateTime paris = ToParisTime(utc);
		if (paris.Hour < RunHourParis)
		{
			return false;
		}
		return lastDay != paris.Year * 10000 + paris.Month * 100 + paris.Day;
	}

	internal static string BuildBatchUrl(IReadOnlyList<string> propertyIds)
	{
		if (propertyIds == null || propertyIds.Count == 0)
		{
			throw new ArgumentException("propertyIds required", nameof(propertyIds));
		}
		string[] clean = propertyIds
			.Where((string id) => !string.IsNullOrWhiteSpace(id))
			.Select((string id) => Uri.EscapeDataString(id.Trim()))
			.ToArray();
		if (clean.Length == 0)
		{
			throw new ArgumentException("propertyIds required", nameof(propertyIds));
		}
		return "https://api.yanport.com/properties?size=" + clean.Length + "&" + string.Join("&", clean.Select((string id) => "ids=" + id));
	}

	/// <summary>
	/// Parse une réponse JSON de <c>GET /properties?ids=...</c>.
	/// Retourne <c>false</c> si la réponse est vide, une sentinelle d'erreur (<c>NotFound</c>/<c>Error500</c>),
	/// ou un JSON sans tableau <c>hits</c> — afin de ne jamais fermer un lot sur une erreur API.
	/// </summary>
	internal static bool TryParseBatchResponse(string json, out Dictionary<string, YanportStatus> found)
	{
		found = null;
		if (string.IsNullOrWhiteSpace(json)
			|| string.Equals(json, YanportServiceConstant.NotFound, StringComparison.Ordinal)
			|| string.Equals(json, YanportServiceConstant.Error500, StringComparison.Ordinal))
		{
			return false;
		}
		try
		{
			using JsonDocument doc = JsonDocument.Parse(json);
			if (doc.RootElement.ValueKind != JsonValueKind.Object)
			{
				return false;
			}
			if (!doc.RootElement.TryGetProperty("hits", out JsonElement hitsEl) || hitsEl.ValueKind != JsonValueKind.Array)
			{
				return false;
			}
			Dictionary<string, YanportStatus> map = new Dictionary<string, YanportStatus>(StringComparer.OrdinalIgnoreCase);
			foreach (JsonElement hit in hitsEl.EnumerateArray())
			{
				if (hit.ValueKind != JsonValueKind.Object)
				{
					continue;
				}
				if (!hit.TryGetProperty("id", out JsonElement idEl) || idEl.ValueKind != JsonValueKind.String)
				{
					continue;
				}
				string id = idEl.GetString();
				if (string.IsNullOrWhiteSpace(id))
				{
					continue;
				}
				bool active = false;
				DateTime? pubEnd = null;
				if (hit.TryGetProperty("marketing", out JsonElement marketingEl) && marketingEl.ValueKind == JsonValueKind.Object)
				{
					if (marketingEl.TryGetProperty("active", out JsonElement activeEl) && (activeEl.ValueKind == JsonValueKind.True || activeEl.ValueKind == JsonValueKind.False))
					{
						active = activeEl.GetBoolean();
					}
					if (marketingEl.TryGetProperty("publicationEndDate", out JsonElement endEl) && endEl.ValueKind == JsonValueKind.String)
					{
						pubEnd = ParseYanportDateToParis(endEl.GetString());
					}
				}
				map[id.Trim()] = new YanportStatus(active, pubEnd);
			}
			found = map;
			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}

	internal static DateTime? ParseYanportDateToParis(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return null;
		}
		if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset dto))
		{
			return ToParisTime(dto.UtcDateTime);
		}
		return null;
	}

	internal static BatchClassification ClassifyBatch(
		IReadOnlyList<Candidate> batch,
		IReadOnlyDictionary<string, YanportStatus> found,
		DateTime parisNow)
	{
		BatchClassification res = new BatchClassification();
		if (batch == null || batch.Count == 0)
		{
			return res;
		}
		DateTime graceCutoff = parisNow.AddHours(-GracePeriodHours);
		foreach (Candidate c in batch)
		{
			if (string.IsNullOrWhiteSpace(c.PropertyId))
			{
				continue;
			}
			if (found != null && found.TryGetValue(c.PropertyId, out YanportStatus status))
			{
				if (status.Active && !status.PublicationEndDateParis.HasValue)
				{
					res.StillActive++;
				}
				else
				{
					DateTime dateFin = status.PublicationEndDateParis
						?? ValidDateOrFallback(c.DateLastUpdateParis, parisNow);
					res.ToCloseEnded.Add(new CloseDecision(c.PropertyId, dateFin, "closed_yanport"));
				}
				continue;
			}
			if (c.DateLastUpdateParis.HasValue && c.DateLastUpdateParis.Value >= graceCutoff)
			{
				res.SkippedRecent404++;
				continue;
			}
			DateTime fallbackDateFin = ValidDateOrFallback(c.DateLastUpdateParis, parisNow);
			res.ToCloseNotFound.Add(new CloseDecision(c.PropertyId, fallbackDateFin, "not_found_404"));
		}
		return res;
	}

	internal static bool ShouldAbortForAnomaly(int checkedCount, int notFoundCount)
	{
		if (checkedCount < AnomalyMinChecked)
		{
			return false;
		}
		return (double)notFoundCount / checkedCount > MaxNotFoundRatioAbort;
	}

	internal static bool BatchSuccessRateAcceptable(int totalBatches, int failedBatches)
	{
		if (totalBatches <= 0)
		{
			return true;
		}
		return (double)(totalBatches - failedBatches) / totalBatches >= MinBatchSuccessRatio;
	}

	private static DateTime ValidDateOrFallback(DateTime? dt, DateTime fallback)
	{
		if (dt.HasValue && dt.Value.Year >= 1000)
		{
			return dt.Value;
		}
		return fallback;
	}
}
