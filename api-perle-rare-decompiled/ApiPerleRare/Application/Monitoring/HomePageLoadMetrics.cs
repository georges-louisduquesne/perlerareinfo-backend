using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ApiPerleRare.Application.Monitoring;

public static class HomePageLoadMetrics
{
	public const int MaxDurationMs = 600_000;

	public const int CockpitRecentCount = 100;

	public const int RetentionCount = 5_000;

	private static readonly Regex PrenomWord = new("(^|[\\s'-])(\\S)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	public static int ClampDurationMs(int durationMs)
	{
		if (durationMs < 0)
		{
			return 0;
		}
		if (durationMs > MaxDurationMs)
		{
			return MaxDurationMs;
		}
		return durationMs;
	}

	public static double Average(IEnumerable<int> durations)
	{
		if (durations == null)
		{
			return 0;
		}
		int count = 0;
		long sum = 0;
		foreach (int value in durations)
		{
			if (value < 0)
			{
				continue;
			}
			sum += value;
			count++;
		}
		if (count == 0)
		{
			return 0;
		}
		return (double)sum / count;
	}

	/// <param name="adminFilter">null = tous, true = admin seulement, false = hors admin.</param>
	public static (double AverageMs, int Count) SegmentAggregate(
		IEnumerable<(int DurationMs, bool IsAdmin)> rows,
		bool? adminFilter)
	{
		if (rows == null)
		{
			return (0, 0);
		}
		List<int> durations = new();
		foreach ((int durationMs, bool isAdmin) in rows)
		{
			if (adminFilter == true && !isAdmin)
			{
				continue;
			}
			if (adminFilter == false && isAdmin)
			{
				continue;
			}
			if (durationMs >= 0)
			{
				durations.Add(durationMs);
			}
		}
		return (Average(durations), durations.Count);
	}

	public static string FormatPrenomNom(string prenom, string nom)
	{
		string last = (nom ?? string.Empty).Trim().ToUpper(CultureInfo.GetCultureInfo("fr-FR"));
		string raw = (prenom ?? string.Empty).Trim();
		if (string.IsNullOrEmpty(raw))
		{
			return last;
		}
		string lower = raw.ToLower(CultureInfo.GetCultureInfo("fr-FR"));
		string first = PrenomWord.Replace(lower, (Match m) => m.Groups[1].Value + m.Groups[2].Value.ToUpper(CultureInfo.GetCultureInfo("fr-FR")));
		return string.Join(" ", new[] { first, last }.Where((part) => !string.IsNullOrWhiteSpace(part)));
	}

	public static long[] SelectIdsToPrune(IReadOnlyList<long> idsDescending, int keep)
	{
		if (idsDescending == null || idsDescending.Count <= keep)
		{
			return Array.Empty<long>();
		}
		return idsDescending.Skip(keep).ToArray();
	}

	/// <summary>CP_RefConseiller 0 (compte admin) est un identifiant valide.</summary>
	public static bool CanRecordLoad(bool hasConseillerId, int conseillerId)
	{
		return hasConseillerId && conseillerId >= 0;
	}

	public sealed record HomePageLoadDailyPoint(
		string Date,
		double? NonAdminAverageMs,
		int NonAdminCount,
		double? AdminAverageMs,
		int AdminCount);

	public static IReadOnlyList<HomePageLoadDailyPoint> BuildDailyTrend(
		IEnumerable<(DateTime MeasuredAtUtc, int DurationMs, bool IsAdmin)> rows)
	{
		if (rows == null)
		{
			return Array.Empty<HomePageLoadDailyPoint>();
		}
		Dictionary<string, DailyBucket> buckets = new(StringComparer.Ordinal);
		foreach ((DateTime measuredAtUtc, int durationMs, bool isAdmin) in rows)
		{
			if (durationMs < 0)
			{
				continue;
			}
			string dateKey = ParisDateKey(measuredAtUtc);
			if (!buckets.TryGetValue(dateKey, out DailyBucket? bucket))
			{
				bucket = new DailyBucket();
				buckets[dateKey] = bucket;
			}
			if (isAdmin)
			{
				bucket.Admin.Add(durationMs);
			}
			else
			{
				bucket.NonAdmin.Add(durationMs);
			}
		}
		return buckets
			.OrderBy((pair) => pair.Key, StringComparer.Ordinal)
			.Select((pair) => new HomePageLoadDailyPoint(
				pair.Key,
				pair.Value.NonAdmin.Count > 0 ? Math.Round(Average(pair.Value.NonAdmin), 1) : null,
				pair.Value.NonAdmin.Count,
				pair.Value.Admin.Count > 0 ? Math.Round(Average(pair.Value.Admin), 1) : null,
				pair.Value.Admin.Count))
			.ToList();
	}

	private sealed class DailyBucket
	{
		public List<int> NonAdmin { get; } = new();

		public List<int> Admin { get; } = new();
	}

	private static string ParisDateKey(DateTime measuredAtUtc)
	{
		DateTime utc = measuredAtUtc.Kind == DateTimeKind.Utc
			? measuredAtUtc
			: DateTime.SpecifyKind(measuredAtUtc, DateTimeKind.Utc);
		DateTime paris = TimeZoneInfo.ConvertTimeFromUtc(utc, ParisTimeZone());
		return paris.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
	}

	private static TimeZoneInfo ParisTimeZone()
	{
		if (TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Paris", out TimeZoneInfo? paris))
		{
			return paris;
		}
		return TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
	}
}
