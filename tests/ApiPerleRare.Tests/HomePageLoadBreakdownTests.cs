using ApiPerleRare.Application.Monitoring;
using Xunit;

namespace ApiPerleRare.Tests;

public class HomePageLoadBreakdownTests
{
	[Fact]
	public void NormalizeForStorage_accepts_v1_json()
	{
		string json = "{\"v\":1,\"admin\":true,\"candidatMs\":800,\"slowestTab\":9,\"slowestTabMs\":21000}";
		Assert.Equal(json, HomePageLoadBreakdown.NormalizeForStorage(json));
	}

	[Fact]
	public void NormalizeForStorage_rejects_wrong_version()
	{
		Assert.Null(HomePageLoadBreakdown.NormalizeForStorage("{\"v\":2,\"admin\":true}"));
	}

	[Fact]
	public void FormatSummary_describes_slowest_and_candidat()
	{
		string json = "{\"v\":1,\"admin\":true,\"candidatMs\":800,\"slowestTab\":9,\"slowestTabMs\":21000}";
		string summary = HomePageLoadBreakdown.FormatSummary(json);
		Assert.Contains("encaissements", summary);
		Assert.Contains("candidats", summary);
	}
}
