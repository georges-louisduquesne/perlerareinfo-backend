using System.Text.Json;
using System.Text.Json.Serialization;
using ApiPerleRare.RecupInfos;
using Xunit;

namespace ApiPerleRare.Tests;

public class RecupInfosFilterTests
{
	private static readonly JsonSerializerOptions Json = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters = { new JsonStringEnumConverter() }
	};

	[Fact]
	public void RecupInfos_volumes_count_active_yanport_property_rows()
	{
		Assert.Equal(
			"P_State = 1 AND (P_DateFin IS NULL OR P_DateFin < '1000-01-01')",
			RecupInfoSearcher.ActivePropertyWhere);
		Assert.Equal("P_PrixEvol = -1", RecupInfoSearcher.BaissePrixWhere);
		Assert.Equal("(P_PrixEvol IS NULL OR P_PrixEvol <> -1)", RecupInfoSearcher.NotBaissePrixWhere);
	}

	[Fact]
	public void Filter_accepts_typeTransaction_string_or_array()
	{
		Filter fromString = JsonSerializer.Deserialize<Filter>(
			"""{"typeTransaction":"A","typeBien":["Maison"]}""", Json);
		Assert.Equal(new[] { TypeTransaction.A }, fromString.TypeTransaction);
		Assert.Equal(new[] { TypeBien.Maison }, fromString.TypeBien);

		Filter fromArray = JsonSerializer.Deserialize<Filter>(
			"""{"typeTransaction":["A"],"typeBien":["Maison"]}""", Json);
		Assert.Equal(new[] { TypeTransaction.A }, fromArray.TypeTransaction);
	}

	[Fact]
	public void AdjustQuartiers_keeps_yanport_quartier_names()
	{
		RecupInfoResponse response = new RecupInfoResponse();
		response.Counts.Add(new RecupInfoCount
		{
			Field = "TypeTransaction",
			Value = "A",
			Nb = 47
		});
		response.Counts.Add(new RecupInfoCount
		{
			Field = "Quartiers",
			Value = "94120>Vincennes",
			Nb = 12
		});
		response.Counts.Add(new RecupInfoCount
		{
			Field = "Quartiers",
			Value = "94160>[94160001]",
			Nb = 3
		});
		response.AdjustQuartiers();
		Assert.Contains(response.Counts, c => c.Field == "TypeTransaction" && c.Nb == 47);
		Assert.Contains(response.Counts, c => c.Field == "Quartiers" && c.Value == "94120>Vincennes" && c.Nb == 12);
		Assert.Contains(response.Counts, c => c.Field == "Quartiers" && c.Value == "94160>94160001" && c.Nb == 3);
	}

	[Fact]
	public void AdjustTags_skips_non_bracket_yanport_values()
	{
		RecupInfoResponse response = new RecupInfoResponse();
		response.Counts.Add(new RecupInfoCount
		{
			Field = "TypeTransaction",
			Value = "A",
			Nb = 47
		});
		response.Counts.Add(new RecupInfoCount
		{
			Field = "Tag",
			Value = "piscine",
			Nb = 4
		});
		response.Counts.Add(new RecupInfoCount
		{
			Field = "Tag",
			Value = "[18][3]",
			Nb = 2
		});
		response.AdjustTags();
		Assert.Contains(response.Counts, c => c.Field == "TypeTransaction" && c.Nb == 47);
		Assert.DoesNotContain(response.Counts, c => c.Value == "piscine");
		Assert.Contains(response.Counts, c => c.Field == "Tag" && c.Value == "[18]" && c.Nb == 2);
		Assert.Contains(response.Counts, c => c.Field == "Tag" && c.Value == "[3]" && c.Nb == 2);
	}
}
