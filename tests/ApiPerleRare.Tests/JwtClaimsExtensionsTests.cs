using System.Security.Claims;
using ApiPerleRare;
using Xunit;

namespace ApiPerleRare.Tests;

public class JwtClaimsExtensionsTests
{
	[Fact]
	public void GetId_reads_unique_name_when_Identity_Name_is_null()
	{
		var identity = new ClaimsIdentity(new[]
		{
			new Claim("unique_name", "42"),
			new Claim("sub", "demo")
		}, "Bearer");
		Assert.Equal(42, new ClaimsPrincipal(identity).GetId());
	}

	[Fact]
	public void GetId_reads_Identity_Name()
	{
		var identity = new ClaimsIdentity(new[]
		{
			new Claim(ClaimTypes.Name, "7"),
			new Claim(ClaimTypes.NameIdentifier, "anna")
		}, "Bearer");
		Assert.Equal(7, new ClaimsPrincipal(identity).GetId());
		Assert.Equal("anna", new ClaimsPrincipal(identity).GetLogin());
	}

	[Fact]
	public void GetId_returns_zero_when_name_is_login_string()
	{
		var identity = new ClaimsIdentity(new[]
		{
			new Claim(ClaimTypes.Name, "CTHIERIET")
		}, "Bearer");
		Assert.Equal(0, new ClaimsPrincipal(identity).GetId());
	}
}
