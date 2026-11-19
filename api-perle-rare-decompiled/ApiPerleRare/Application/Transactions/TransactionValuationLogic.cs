using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ApiPerleRare.Models;

namespace ApiPerleRare.Application.Transactions;

public sealed record PrixMoyenPoint(int Cp, int Annee, string Trim, int PrixMoyen);

public sealed record ActualizedValuation(
	decimal? PrixM2Actualise,
	decimal? EvolutionPct,
	string ReferenceQuarter,
	string TransactionQuarter,
	bool UsedLatestFallback);

public sealed class TransactionListingItemDto
{
	public ulong Id { get; set; }

	public string DateTransaction { get; set; }

	public string NatureMutation { get; set; }

	public string TypeBien { get; set; }

	public string AdresseNum { get; set; }

	public string AdresseSuffixe { get; set; }

	public string AdresseRue { get; set; }

	public string Cp { get; set; }

	public string Commune { get; set; }

	public int? NbPieces { get; set; }

	public int? NbChambres { get; set; }

	public int? Etage { get; set; }

	public int? NbEtages { get; set; }

	public bool? Ascenseur { get; set; }

	public bool? DernierEtage { get; set; }

	public string Dpe { get; set; }

	public decimal Prix { get; set; }

	public decimal? SurfaceNormee { get; set; }

	public decimal? SurfaceCarrez { get; set; }

	public decimal? SurfaceBatie { get; set; }

	public decimal? SurfaceRetenue { get; set; }

	/// <summary>Source de la surface retenue : "normee" | "carrez" | "batie" | null.</summary>
	public string SurfaceSource { get; set; }

	public decimal? SurfaceTerrain { get; set; }

	public decimal? PrixM2 { get; set; }

	public decimal? PrixM2Actualise { get; set; }

	public decimal? EvolutionPct { get; set; }

	public string ReferenceQuarter { get; set; }

	public string TransactionQuarter { get; set; }

	public int? NbLots { get; set; }

	public int NbDependances { get; set; }

	public string IdParcelle { get; set; }

	public string PropertyId { get; set; }

	public string PhotoUrl { get; set; }

	public string Remarques { get; set; }

	public string Source { get; set; }
}

internal static class TransactionValuationLogic
{
	private static readonly Dictionary<string, string> LeadingStreetAbbrevs = new(StringComparer.Ordinal)
	{
		["r"] = "rue",
		["av"] = "avenue",
		["ave"] = "avenue",
		["bd"] = "boulevard",
		["bld"] = "boulevard",
		["boul"] = "boulevard",
		["pl"] = "place",
		["imp"] = "impasse",
		["all"] = "allee",
		["sq"] = "square",
		["fg"] = "faubourg",
		["fbg"] = "faubourg",
		["pass"] = "passage",
		["pas"] = "passage",
		["crs"] = "cours",
		["che"] = "chemin",
		["ch"] = "chemin",
		["rte"] = "route",
		["qu"] = "quai",
		["cite"] = "cite",
		["villa"] = "villa",
		["sent"] = "sentier",
		["res"] = "residence",
	};

	private static readonly Regex NonAlnum = new("[^a-z0-9]+", RegexOptions.Compiled);
	private static readonly Regex HouseNumSplit = new("^(\\d+)\\s*([a-zA-Z]+)?$", RegexOptions.Compiled);

	internal static string NormalizeStreet(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return string.Empty;
		}
		string decomposed = raw.Normalize(NormalizationForm.FormD);
		var sb = new StringBuilder(decomposed.Length);
		foreach (char ch in decomposed)
		{
			if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
			{
				sb.Append(char.ToLowerInvariant(ch));
			}
		}
		string cleaned = NonAlnum.Replace(sb.ToString(), " ").Trim();
		if (cleaned.Length == 0)
		{
			return string.Empty;
		}
		string[] tokens = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (tokens.Length > 1 && LeadingStreetAbbrevs.TryGetValue(tokens[0], out string expanded))
		{
			tokens[0] = expanded;
		}
		for (int i = 0; i < tokens.Length; i++)
		{
			if (tokens[i] == "st")
			{
				tokens[i] = "saint";
			}
			else if (tokens[i] == "ste")
			{
				tokens[i] = "sainte";
			}
		}
		return string.Join(' ', tokens);
	}

	internal static IReadOnlyList<string> HouseNumberVariants(string raw)
	{
		string trimmed = (raw ?? string.Empty).Trim();
		if (trimmed.Length == 0)
		{
			return Array.Empty<string>();
		}
		string compact = Regex.Replace(trimmed, "\\s+", string.Empty);
		var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { trimmed, compact };

		Match m = HouseNumSplit.Match(compact);
		if (m.Success)
		{
			string digits = m.Groups[1].Value;
			string suffix = m.Groups[2].Value.ToUpperInvariant();
			if (suffix.Length == 0)
			{
				set.Add(digits);
			}
			else
			{
				string shortSuf = suffix switch
				{
					"BIS" => "B",
					"TER" => "T",
					"QUATER" => "Q",
					_ => suffix,
				};
				string longSuf = shortSuf switch
				{
					"B" => "BIS",
					"T" => "TER",
					"Q" => "QUATER",
					_ => suffix,
				};
				set.Add(digits + shortSuf);
				set.Add(digits + " " + shortSuf);
				set.Add(digits + longSuf.ToLowerInvariant());
				set.Add(digits + " " + longSuf.ToLowerInvariant());
			}
		}
		return set.Where((v) => !string.IsNullOrWhiteSpace(v)).ToArray();
	}

	internal static int ParseQuarterNumber(string trim)
	{
		return (trim ?? string.Empty).Trim().ToUpperInvariant() switch
		{
			"T1" => 1,
			"T2" => 2,
			"T3" => 3,
			"T4" => 4,
			_ => 0,
		};
	}

	internal static (int Year, string Trim, int QuarterNumber) QuarterFromDate(DateTime date)
	{
		int q = ((date.Month - 1) / 3) + 1;
		return (date.Year, "T" + q, q);
	}

	internal static int QuarterOrdinal(int year, int quarterNumber) => (year * 4) + quarterNumber;

	internal static (decimal? Surface, string Source) ResolveSurface(decimal? surfaceNormee, decimal? surfaceCarrez, decimal? surfaceBatie)
	{
		if (surfaceNormee.HasValue && surfaceNormee.Value > 0m)
		{
			return (surfaceNormee.Value, "normee");
		}
		if (surfaceCarrez.HasValue && surfaceCarrez.Value > 0m)
		{
			return (surfaceCarrez.Value, "carrez");
		}
		if (surfaceBatie.HasValue && surfaceBatie.Value > 0m)
		{
			return (surfaceBatie.Value, "batie");
		}
		return (null, null);
	}

	internal static decimal? ComputePricePerSqm(decimal prix, decimal? surface, decimal? storedPrixM2)
	{
		if (prix > 0m && surface.HasValue && surface.Value > 0m)
		{
			return Math.Round(prix / surface.Value, 0, MidpointRounding.AwayFromZero);
		}
		if (storedPrixM2.HasValue && storedPrixM2.Value > 0m)
		{
			return Math.Round(storedPrixM2.Value, 0, MidpointRounding.AwayFromZero);
		}
		return null;
	}

	internal static ActualizedValuation ComputeActualizedPricePerSqm(
		decimal? prixM2,
		int cp,
		DateTime txDate,
		IReadOnlyList<PrixMoyenPoint> history)
	{
		var (txYear, txTrim, txQ) = QuarterFromDate(txDate);
		string txLabel = $"{txTrim} {txYear}";
		if (!prixM2.HasValue || prixM2.Value <= 0m || history == null || history.Count == 0)
		{
			return new ActualizedValuation(null, null, null, txLabel, false);
		}

		var cpRows = history
			.Where((h) => h.Cp == cp && h.PrixMoyen > 0 && ParseQuarterNumber(h.Trim) > 0)
			.OrderByDescending((h) => QuarterOrdinal(h.Annee, ParseQuarterNumber(h.Trim)))
			.ToList();
		if (cpRows.Count == 0)
		{
			return new ActualizedValuation(null, null, null, txLabel, false);
		}

		PrixMoyenPoint latest = cpRows[0];
		int latestQ = ParseQuarterNumber(latest.Trim);
		string refLabel = $"{latest.Trim.ToUpperInvariant()} {latest.Annee}";

		int txOrd = QuarterOrdinal(txYear, txQ);
		int latestOrd = QuarterOrdinal(latest.Annee, latestQ);

		if (txOrd >= latestOrd)
		{
			return new ActualizedValuation(
				Math.Round(prixM2.Value, 0, MidpointRounding.AwayFromZero),
				0m,
				refLabel,
				txLabel,
				UsedLatestFallback: txOrd > latestOrd);
		}

		PrixMoyenPoint atTx = cpRows.FirstOrDefault((h) => h.Annee == txYear && ParseQuarterNumber(h.Trim) == txQ);
		if (atTx == null || atTx.PrixMoyen <= 0)
		{
			return new ActualizedValuation(null, null, refLabel, txLabel, false);
		}

		decimal ratio = (decimal)latest.PrixMoyen / atTx.PrixMoyen;
		decimal actualized = Math.Round(prixM2.Value * ratio, 0, MidpointRounding.AwayFromZero);
		decimal evolutionPct = Math.Round((ratio - 1m) * 100m, 1, MidpointRounding.AwayFromZero);

		return new ActualizedValuation(actualized, evolutionPct, refLabel, txLabel, false);
	}

	internal static TransactionListingItemDto MapToDto(TransactionImmo tx, IReadOnlyList<PrixMoyenPoint> cpHistory)
	{
		var (surface, source) = ResolveSurface(tx.TSurfaceNormee, tx.TSurfaceCarrez, tx.TSurfaceBatie);
		decimal? prixM2 = ComputePricePerSqm(tx.TPrix, surface, tx.TPrixM2);
		int.TryParse(tx.TCp, NumberStyles.Integer, CultureInfo.InvariantCulture, out int cpInt);
		ActualizedValuation valuation = ComputeActualizedPricePerSqm(prixM2, cpInt, tx.TDateTransaction, cpHistory);

		return new TransactionListingItemDto
		{
			Id = tx.TRef,
			DateTransaction = tx.TDateTransaction.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
			NatureMutation = tx.TNatureMutation,
			TypeBien = tx.TTypeBien,
			AdresseNum = tx.TAdresseNum,
			AdresseSuffixe = tx.TAdresseSuffixe,
			AdresseRue = tx.TAdresseRue,
			Cp = tx.TCp,
			Commune = tx.TCommune,
			NbPieces = tx.TNbPieces,
			NbChambres = tx.TNbChambres,
			Etage = tx.TEtage,
			NbEtages = tx.TNbEtages,
			Ascenseur = tx.TAscenseur,
			DernierEtage = tx.TDernierEtage,
			Dpe = tx.TDpe,
			Prix = tx.TPrix,
			SurfaceNormee = tx.TSurfaceNormee,
			SurfaceCarrez = tx.TSurfaceCarrez,
			SurfaceBatie = tx.TSurfaceBatie,
			SurfaceRetenue = surface,
			SurfaceSource = source,
			SurfaceTerrain = tx.TSurfaceTerrain,
			PrixM2 = prixM2,
			PrixM2Actualise = valuation.PrixM2Actualise,
			EvolutionPct = valuation.EvolutionPct,
			ReferenceQuarter = valuation.ReferenceQuarter,
			TransactionQuarter = valuation.TransactionQuarter,
			NbLots = tx.TNbLots,
			NbDependances = tx.TNbDependances,
			IdParcelle = tx.TIdParcelle,
			PropertyId = tx.TPropertyId,
			PhotoUrl = tx.TPhotoUrl,
			Remarques = tx.TRemarques,
			Source = tx.TSource,
		};
	}
}
