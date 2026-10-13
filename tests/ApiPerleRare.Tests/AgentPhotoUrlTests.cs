using ApiPerleRare.Application.Files;
using Xunit;

namespace ApiPerleRare.Tests;

public class AgentPhotoUrlTests
{
	[Fact]
	public void Accepts_an_https_url_and_prefixes_a_bare_host()
	{
		Assert.True(AgentPhotoUrl.TryNormalize("https://cdn.example.com/a.jpg", out string url, out _));
		Assert.Equal("https://cdn.example.com/a.jpg", url);
		Assert.True(AgentPhotoUrl.TryNormalize("cdn.example.com/portrait.webp", out string prefixed, out _));
		Assert.Equal("https://cdn.example.com/portrait.webp", prefixed);
	}

	[Fact]
	public void Rejects_empty_ftp_and_overlong_urls()
	{
		Assert.False(AgentPhotoUrl.TryNormalize("", out _, out string empty));
		Assert.False(AgentPhotoUrl.TryNormalize("ftp://cdn.example.com/a.jpg", out _, out _));
		Assert.False(AgentPhotoUrl.TryNormalize("https://cdn.example.com/" + new string('a', 300), out _, out _));
		Assert.Contains("http", empty);
	}
}
