using ApiPerleRare.YanportModels;
using Xunit;

namespace ApiPerleRare.Tests;

public class YanportAgencyApiTests
{
	[Fact]
	public void Agency_url_uses_yanport_agencies_endpoint()
	{
		Assert.Equal("https://api.yanport.com/agencies/5404190113267712", YanportAgencyApi.AgencyUrl(5404190113267712L));
	}

	[Theory]
	[InlineData("83709", 83709L)]
	[InlineData(" 5404190113267712 ", 5404190113267712L)]
	[InlineData("0", 0L)]
	[InlineData("", 0L)]
	[InlineData("abc", 0L)]
	public void Try_parse_id_accepts_positive_integers_only(string raw, long expected)
	{
		bool ok = YanportAgencyApi.TryParseId(raw, out long id);
		if (expected > 0)
		{
			Assert.True(ok);
			Assert.Equal(expected, id);
		}
		else
		{
			Assert.False(ok);
		}
	}
}
