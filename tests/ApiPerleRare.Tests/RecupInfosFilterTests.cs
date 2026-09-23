using System.Text.Json;
using System.Text.Json.Serialization;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using ApiPerleRare.RecupInfos;
using ApiPerleRare.RecupInfos.PropertyFilters;
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
	public void Surface_min_only_keeps_open_max_as_greater_or_equal()
	{
		Assert.Equal("P_Surface >= 90", RecupInfoSearcher.MakeSurfaceFilter(90, 0));
		Assert.Equal("P_Surface BETWEEN 90 AND 500", RecupInfoSearcher.MakeSurfaceFilter(90, 500));
		Assert.Null(RecupInfoSearcher.MakeSurfaceFilter(0, 0));
	}

	[Fact]
	public void Anciennete_treats_missing_DateDebut_as_zero_days()
	{
		Assert.Contains(", 0, ABS(DATEDIFF", RecupInfoSearcher.DateDebutDaysExpr);
		Assert.Equal(
			RecupInfoSearcher.DateDebutDaysExpr + " BETWEEN 0 AND 30",
			RecupInfoSearcher.MakeAncienneteFilter(0, 30));
	}

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

	[Fact]
	public void MakeTagsFilter_mixes_and_or_and_not()
	{
		int[] tags = [12, -9];
		int[] tagsOr = [18, 3];
		Assert.Equal(
			"P_ListeTags LIKE '%[12]%' AND (P_ListeTags LIKE '%[18]%' OR P_ListeTags LIKE '%[3]%') AND (P_ListeTags IS NULL OR (P_ListeTags NOT LIKE '%[9]%'))",
			RecupInfoSearcher.MakeTagsFilter(tags, tagsOr));
		Assert.Equal(
			"P_ListeTags LIKE '%[18]%' AND P_ListeTags LIKE '%[3]%' AND (P_ListeTags IS NULL OR (P_ListeTags NOT LIKE '%[9]%'))",
			RecupInfoSearcher.MakeTagsFilter([18, 3, -9]));
		Assert.Equal(
			"(P_ListeTags LIKE '%[18]%' OR P_ListeTags LIKE '%[3]%') AND (P_ListeTags IS NULL OR (P_ListeTags NOT LIKE '%[9]%'))",
			RecupInfoSearcher.MakeTagsFilter([18, 3, -9], null, "or"));
		Assert.Null(RecupInfoSearcher.MakeTagsFilter(null, null, "or"));
	}

	[Fact]
	public void Apply_inserts_missing_property_contacts_for_the_contact()
	{
		string sql = RecupInfoSearcher.ApplyPropertyContactsSql(47538, RecupInfoSearcher.ActivePropertyWhere);
		Assert.Contains("INSERT INTO `property_contact`", sql);
		Assert.Contains("PC_RefContact", sql);
		Assert.Contains("47538", sql);
		Assert.Contains("P_PropertyId", sql);
		Assert.Contains("LIMIT 500", sql);
		Assert.Contains(RecupInfoSearcher.ActivePropertyWhere, sql);
		Assert.Contains("SELECT p.`P_PropertyId`", RecupInfoSearcher.ApplyPropertyIdsSql(RecupInfoSearcher.ActivePropertyWhere));
	}

	[Fact]
	public void Filter_accepts_optional_tagsOr()
	{
		Filter mixed = JsonSerializer.Deserialize<Filter>("""{"tags":[12,-9],"tagsOr":[18,3]}""", Json);
		Assert.Equal(new[] { 12, -9 }, mixed.Tags);
		Assert.Equal(new[] { 18, 3 }, mixed.TagsOr);
		Filter orMode = JsonSerializer.Deserialize<Filter>("""{"tags":[18,3],"tagsMode":"or"}""", Json);
		Assert.Equal("or", orMode.TagsMode);
		Assert.True(RecupInfoSearcher.IsTagsModeOr(orMode.TagsMode));
	}

	[Fact]
	public void TagsPropertyFilter_mixes_required_anyof_and_exclude()
	{
		string phpAnd = PhpSerializer.FromJson("""{"18":"O","3":"O"}""");
		TagsPropertyFilter andFilter = new TagsPropertyFilter(phpAnd);
		Assert.Equal("Manque le tag 3", andFilter.GetNotMatchReason(new Property { PListeTags = "[18]" }));
		Assert.Null(andFilter.GetNotMatchReason(new Property { PListeTags = "[18][3]" }));

		string phpMixed = PhpSerializer.FromJson("""{"12":"O","18":"U","3":"U","9":"N"}""");
		TagsPropertyFilter mixed = new TagsPropertyFilter(phpMixed);
		Assert.Null(mixed.GetNotMatchReason(new Property { PListeTags = "[12][18]" }));
		Assert.Equal("Manque le tag 12", mixed.GetNotMatchReason(new Property { PListeTags = "[18]" }));
		Assert.Equal("Manque au moins un tag", mixed.GetNotMatchReason(new Property { PListeTags = "[12]" }));
		Assert.Equal("Tag 9 présent et ne devrait pas", mixed.GetNotMatchReason(new Property { PListeTags = "[12][18][9]" }));

		string phpOr = PhpSerializer.FromJson("""{"18":"O","3":"O","9":"N","_mode":"or"}""");
		TagsPropertyFilter orFilter = new TagsPropertyFilter(phpOr);
		Assert.Null(orFilter.GetNotMatchReason(new Property { PListeTags = "[18]" }));
		Assert.Equal("Manque au moins un tag", orFilter.GetNotMatchReason(new Property { PListeTags = "[7]" }));
	}
}
