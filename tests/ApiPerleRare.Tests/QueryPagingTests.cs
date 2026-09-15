using ApiPerleRare.Helpers;
using Xunit;

namespace ApiPerleRare.Tests;

public class QueryPagingTests
{
	[Fact]
	public void Normalize_clamps_negative_skip_and_take()
	{
		int skip = -10;
		int take = -3;
		QueryPaging.Normalize(ref skip, ref take);
		Assert.Equal(0, skip);
		Assert.Equal(0, take);
	}

	[Fact]
	public void Normalize_keeps_take_zero_as_unlimited()
	{
		int skip = 50;
		int take = 0;
		QueryPaging.Normalize(ref skip, ref take);
		Assert.Equal(50, skip);
		Assert.Equal(0, take);
	}

	[Fact]
	public void EnsureOrderBy_keeps_existing_sort()
	{
		Assert.Equal("cNomFamille asc", QueryPaging.EnsureOrderBy("cNomFamille asc", "CRefContact"));
	}

	[Fact]
	public void EnsureOrderBy_fills_fallback_when_empty()
	{
		Assert.Equal("CRefContact", QueryPaging.EnsureOrderBy(null, "CRefContact"));
		Assert.Equal("CRefContact", QueryPaging.EnsureOrderBy("  ", "CRefContact"));
	}
}
