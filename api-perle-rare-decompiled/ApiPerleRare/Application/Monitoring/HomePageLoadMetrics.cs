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
}
