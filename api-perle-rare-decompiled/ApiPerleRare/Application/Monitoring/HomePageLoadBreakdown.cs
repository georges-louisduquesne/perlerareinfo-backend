using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ApiPerleRare.Application.Monitoring;

/// <summary>JSON compact v1 posté par l’accueil Angular (télémétrie fine).</summary>
public static class HomePageLoadBreakdown
{
	public const int MaxJsonLength = 2048;

	public static string NormalizeForStorage(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return null;
		}
		string trimmed = raw.Trim();
		if (trimmed.Length > MaxJsonLength)
		{
			return null;
		}
		try
		{
			using JsonDocument doc = JsonDocument.Parse(trimmed);
			JsonElement root = doc.RootElement;
			if (root.ValueKind != JsonValueKind.Object)
			{
				return null;
			}
			if (!root.TryGetProperty("v", out JsonElement ver) || ver.GetInt32() != 1)
			{
				return null;
			}
			return trimmed;
		}
		catch (JsonException)
		{
			return null;
		}
	}

	public static string FormatSummary(string storedJson)
	{
		if (string.IsNullOrWhiteSpace(storedJson))
		{
			return "";
		}
		try
		{
			using JsonDocument doc = JsonDocument.Parse(storedJson);
			JsonElement root = doc.RootElement;
			if (!root.TryGetProperty("v", out JsonElement ver) || ver.GetInt32() != 1)
			{
				return "";
			}
			List<string> parts = new List<string>();
			if (root.TryGetProperty("slowestTab", out JsonElement tabEl)
				&& root.TryGetProperty("slowestTabMs", out JsonElement tabMsEl)
				&& tabEl.TryGetInt32(out int tab)
				&& tabMsEl.TryGetInt32(out int tabMs))
			{
				parts.Add($"Plus lent : {TabLabel(tab)} ({FormatMsShort(tabMs)})");
			}
			if (root.TryGetProperty("admin", out JsonElement adminEl)
				&& adminEl.ValueKind == JsonValueKind.True
				&& root.TryGetProperty("candidatMs", out JsonElement cMs)
				&& cMs.TryGetInt32(out int candidatMs))
			{
				parts.Add($"Bloc candidats {FormatMsShort(candidatMs)}");
			}
			return string.Join(" · ", parts);
		}
		catch (JsonException)
		{
			return "";
		}
	}

	private static string TabLabel(int tab)
	{
		return tab switch
		{
			0 => "prospects actifs",
			1 => "clients actifs",
			2 => "alertes prospects",
			3 => "alertes clients",
			4 => "rdv prospect",
			5 => "visites prévues",
			6 => "dernières visites",
			7 => "transactions",
			8 => "offres",
			9 => "encaissements",
			10 => "alertes candidats",
			11 => "rdv candidats",
			12 => "candidats actifs",
			_ => $"onglet {tab}",
		};
	}

	private static string FormatMsShort(int ms)
	{
		if (ms < 0)
		{
			return "—";
		}
		if (ms < 1000)
		{
			return $"{ms} ms";
		}
		double s = Math.Round(ms / 100.0) / 10.0;
		return s.ToString("0.#", System.Globalization.CultureInfo.GetCultureInfo("fr-FR")) + " s";
	}
}
