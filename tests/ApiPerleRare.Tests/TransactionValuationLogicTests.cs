using System;
using System.Collections.Generic;
using ApiPerleRare.Application.Transactions;
using ApiPerleRare.Models;
using Xunit;

namespace ApiPerleRare.Tests;

public class TransactionValuationLogicTests
{
	private static readonly IReadOnlyList<PrixMoyenPoint> Paris75001History = new[]
	{
		new PrixMoyenPoint(75001, 2021, "T1", 12500),
		new PrixMoyenPoint(75001, 2023, "T3", 11000),
		new PrixMoyenPoint(75001, 2026, "T1", 11920),
		new PrixMoyenPoint(75001, 2026, "T2", 11840),
	};

	[Theory]
	[InlineData("BD HAUSSMANN", "boulevard haussmann")]
	[InlineData("Boulevard Haussmann", "boulevard haussmann")]
	[InlineData("RUE ST HONORE", "rue saint honore")]
	[InlineData("Rue Saint-Honoré", "rue saint honore")]
	[InlineData("AV VICTOR HUGO", "avenue victor hugo")]
	[InlineData("ALL DES ACACIAS", "allee des acacias")]
	[InlineData("PL DE LA REPUBLIQUE", "place de la republique")]
	public void NormalizeStreet_MatchesDvfAndBanFormats(string input, string expected)
	{
		Assert.Equal(expected, TransactionValuationLogic.NormalizeStreet(input));
	}

	[Fact]
	public void HouseNumberVariants_HandlesBisTerAndSimpleNumbers()
	{
		IReadOnlyList<string> bisVariants = TransactionValuationLogic.HouseNumberVariants("12bis");
		Assert.Contains("12B", bisVariants);
		Assert.Contains("12 B", bisVariants);
		Assert.Contains("12bis", bisVariants);
		Assert.Contains("12 bis", bisVariants);

		IReadOnlyList<string> simpleVariants = TransactionValuationLogic.HouseNumberVariants("45");
		Assert.Contains("45", simpleVariants);
		Assert.Single(simpleVariants);
	}

	[Theory]
	[InlineData(2024, 1, 15, "T1", 1)]
	[InlineData(2024, 4, 1, "T2", 2)]
	[InlineData(2024, 9, 30, "T3", 3)]
	[InlineData(2024, 12, 31, "T4", 4)]
	public void QuarterFromDate_ReturnsExpectedQuarter(int year, int month, int day, string expectedTrim, int expectedQ)
	{
		var (y, trim, q) = TransactionValuationLogic.QuarterFromDate(new DateTime(year, month, day));
		Assert.Equal(year, y);
		Assert.Equal(expectedTrim, trim);
		Assert.Equal(expectedQ, q);
	}

	[Fact]
	public void ResolveSurface_PrioritizesNormeeThenCarrezThenBatie()
	{
		Assert.Equal((72.5m, "normee"), TransactionValuationLogic.ResolveSurface(72.5m, 70m, 68m));
		Assert.Equal((70m, "carrez"), TransactionValuationLogic.ResolveSurface(null, 70m, 68m));
		Assert.Equal((68m, "batie"), TransactionValuationLogic.ResolveSurface(null, null, 68m));
		Assert.Equal((null, null), TransactionValuationLogic.ResolveSurface(null, 0m, null));
	}

	[Fact]
	public void ComputeActualizedPricePerSqm_AppliesLatestToTxQuarterRatio()
	{
		ActualizedValuation result = TransactionValuationLogic.ComputeActualizedPricePerSqm(
			10000m,
			75001,
			new DateTime(2023, 8, 12), // T3 2023 -> 11000, latest T2 2026 -> 11840
			Paris75001History);

		// 10000 * (11840 / 11000) = 10763.636... -> 10764
		Assert.Equal(10764m, result.PrixM2Actualise);
		Assert.Equal(7.6m, result.EvolutionPct);
		Assert.Equal("T2 2026", result.ReferenceQuarter);
		Assert.Equal("T3 2023", result.TransactionQuarter);
		Assert.False(result.UsedLatestFallback);
	}

	[Fact]
	public void ComputeActualizedPricePerSqm_UsesIdentityWhenAfterLatestPublishedQuarter()
	{
		ActualizedValuation result = TransactionValuationLogic.ComputeActualizedPricePerSqm(
			12345m,
			75001,
			new DateTime(2026, 10, 5), // T4 2026 > T2 2026
			Paris75001History);

		Assert.Equal(12345m, result.PrixM2Actualise);
		Assert.Equal(0m, result.EvolutionPct);
		Assert.Equal("T2 2026", result.ReferenceQuarter);
		Assert.True(result.UsedLatestFallback);
	}

	[Fact]
	public void ComputeActualizedPricePerSqm_ReturnsNullWhenPostalCodeNotInPrixMoyens()
	{
		ActualizedValuation result = TransactionValuationLogic.ComputeActualizedPricePerSqm(
			8500m,
			92200,
			new DateTime(2023, 8, 12),
			Paris75001History);

		Assert.Null(result.PrixM2Actualise);
		Assert.Null(result.EvolutionPct);
		Assert.Null(result.ReferenceQuarter);
	}

	[Fact]
	public void MapToDto_ComputesPricePerSqmFromNormeeAndActualizesViaPrixMoyens()
	{
		var tx = new TransactionImmo
		{
			TRef = 42,
			TDateTransaction = new DateTime(2021, 2, 10), // T1 2021 -> 12500, latest T2 2026 -> 11840
			TNatureMutation = "Vente",
			TTypeBien = "Appartement",
			TAdresseNum = "10",
			TAdresseRue = "Rue de Rivoli",
			TAdresseRueNorm = "rue de rivoli",
			TCp = "75001",
			TNbPieces = 3,
			TNbChambres = 2,
			TEtage = 4,
			TPrix = 750000m,
			TSurfaceBatie = 58m,
			TSurfaceCarrez = 59m,
			TSurfaceNormee = 60m,
			TNbDependances = 1,
			TSource = "DVF",
		};

		TransactionListingItemDto dto = TransactionValuationLogic.MapToDto(tx, Paris75001History);

		Assert.Equal("2021-02-10", dto.DateTransaction);
		Assert.Equal(60m, dto.SurfaceRetenue);
		Assert.Equal("normee", dto.SurfaceSource);
		Assert.Equal(12500m, dto.PrixM2);
		// 12500 * (11840 / 12500) = 11840
		Assert.Equal(11840m, dto.PrixM2Actualise);
		Assert.Equal(-5.3m, dto.EvolutionPct);
		Assert.Equal(2, dto.NbChambres);
		Assert.Equal(4, dto.Etage);
	}
}
