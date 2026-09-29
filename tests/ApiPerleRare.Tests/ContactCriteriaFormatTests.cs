using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using ApiPerleRare.RecupInfos.PropertyFilters;
using Xunit;

namespace ApiPerleRare.Tests;

public class ContactCriteriaFormatTests
{
	[Fact]
	public void ToJson_converts_php_lists_and_keeps_values_already_in_json()
	{
		Assert.Equal("[\"2\",\"3\"]", PhpSerializer.ToJson("a:2:{i:0;s:1:\"2\";i:1;s:1:\"3\";}"));
		Assert.Equal("[\"2\",\"3\"]", PhpSerializer.ToJson("[\"2\",\"3\"]"));
		Assert.Equal("[]", PhpSerializer.ToJson(" [] "));
		Assert.Equal("{\"12\":\"O\"}", PhpSerializer.ToJson("{\"12\":\"O\"}"));
	}

	[Theory]
	[InlineData("a:2:{i:0;s:1:\"2\";i:1;s:1:\"3\";}")]
	[InlineData("[\"2\",\"3\"]")]
	public void Nombre_filter_reads_php_and_json_stored_criteria(string stored)
	{
		var filter = new GenericNombrePropertyFilter(GenericNombrePropertyFilter.Field.NbPieces, stored);

		Assert.Null(filter.GetNotMatchReason(new Property { PNbPieces = 3 }));
		Assert.NotNull(filter.GetNotMatchReason(new Property { PNbPieces = 5 }));
	}

	[Fact]
	public void Nombre_filter_accepts_empty_json_list()
	{
		var filter = new GenericNombrePropertyFilter(GenericNombrePropertyFilter.Field.Etage, "[]");

		Assert.Null(filter.GetNotMatchReason(new Property { PEtage = 2 }));
	}
}
