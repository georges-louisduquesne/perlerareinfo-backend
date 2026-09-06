using System;
using ApiPerleRare.Helpers;
using Xunit;

namespace ApiPerleRare.Tests;

public class SqlSafetyTests
{
	[Fact]
	public void Quote_escapes_apostrophe_without_changing_plain_values()
	{
		Assert.Equal("'studio'", SqlSafety.Quote("studio"));
		Assert.Equal("'O''Hara'", SqlSafety.Quote("O'Hara"));
	}

	[Fact]
	public void Identifier_accepts_annonces_table_and_rejects_injection()
	{
		Assert.Equal("annonces_refcontact_12", SqlSafety.Identifier("annonces_refcontact_12"));
		Assert.Throws<ArgumentException>(() => SqlSafety.Identifier("annonces_refcontact_12; DROP TABLE x"));
		Assert.Throws<ArgumentException>(() => SqlSafety.Identifier("AG_Prix OR 1=1"));
	}

	[Theory]
	[InlineData("10", true)]
	[InlineData("10.5", true)]
	[InlineData("-3", true)]
	[InlineData("1;SELECT", false)]
	[InlineData("", false)]
	public void IsNumber_only_allows_numeric_literals(string value, bool expected)
	{
		Assert.Equal(expected, SqlSafety.IsNumber(value));
	}
}
