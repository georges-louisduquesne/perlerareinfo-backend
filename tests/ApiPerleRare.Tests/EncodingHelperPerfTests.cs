using ApiPerleRare.Helpers;
using Xunit;

namespace ApiPerleRare.Tests;

public class EncodingHelperPerfTests
{
	[Fact]
	public void FixEncoding_leaves_clean_utf8_unchanged()
	{
		string input = "Prospect Actif — rue de l'École";
		Assert.Same(input, EncodingHelper.FixEncoding(input));
	}

	[Fact]
	public void FixEncoding_still_repairs_mojibake()
	{
		Assert.Equal("café", EncodingHelper.FixEncoding("cafÃ©"));
	}
}
