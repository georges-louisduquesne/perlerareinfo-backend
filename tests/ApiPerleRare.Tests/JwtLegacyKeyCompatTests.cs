using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace ApiPerleRare.Tests;

/// <summary>
/// The production secret is 17 ASCII bytes and the current production API (.NET 7) signs
/// HS256 with those raw bytes. The new API runs next to it (invoices still go through the
/// old one with the same Bearer token), so both must sign and validate with the same key.
/// </summary>
public class JwtLegacyKeyCompatTests
{
	private const string ShortSecret = "seventeen-chars!!";

	private static string B64Url(byte[] bytes) =>
		Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

	private static string LegacyToken(string secret)
	{
		long exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
		string header = B64Url(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
		string payload = B64Url(Encoding.UTF8.GetBytes(
			"{\"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name\":\"7\",\"exp\":" + exp + "}"));
		using var hmac = new HMACSHA256(Encoding.ASCII.GetBytes(secret));
		string signature = B64Url(hmac.ComputeHash(Encoding.ASCII.GetBytes(header + "." + payload)));
		return header + "." + payload + "." + signature;
	}

	private static TokenValidationParameters StartupParameters(string secret) => new TokenValidationParameters
	{
		ValidateIssuerSigningKey = true,
		IssuerSigningKey = JwtTokenFactory.SigningKey(secret),
		ValidateIssuer = false,
		ValidateAudience = false,
		ClockSkew = TimeSpan.Zero
	};

	private static ClaimsPrincipal ValidateLikeStartup(string token, string secret)
	{
		return new JwtSecurityTokenHandler().ValidateToken(token, StartupParameters(secret), out _);
	}

	[Fact]
	public async System.Threading.Tasks.Task Legacy_token_passes_the_aspnet_core_8_handler()
	{
		TokenValidationResult result = await new JsonWebTokenHandler()
			.ValidateTokenAsync(LegacyToken(ShortSecret), StartupParameters(ShortSecret));

		Assert.True(result.IsValid, result.Exception?.Message);
	}

	[Fact]
	public void Token_signed_with_another_secret_is_rejected()
	{
		Assert.ThrowsAny<SecurityTokenException>(() => ValidateLikeStartup(LegacyToken("another-secret-17"), ShortSecret));
	}

	[Fact]
	public void Short_secret_is_used_raw_like_the_legacy_api()
	{
		Assert.Equal(Encoding.ASCII.GetBytes(ShortSecret), JwtTokenFactory.SigningKeyBytes(ShortSecret));
	}

	[Fact]
	public void Token_signed_by_legacy_api_is_accepted()
	{
		ClaimsPrincipal principal = ValidateLikeStartup(LegacyToken(ShortSecret), ShortSecret);

		Assert.Contains(principal.Claims, c => c.Value == "7");
	}

	[Fact]
	public void Token_issued_here_verifies_with_the_raw_legacy_key()
	{
		var user = new ConseillersPersonnels { CpRefConseiller = 7, CpLogin = "anna" };
		string[] parts = JwtTokenFactory.Issue(user, ShortSecret).Split('.');

		using var hmac = new HMACSHA256(Encoding.ASCII.GetBytes(ShortSecret));
		string expected = B64Url(hmac.ComputeHash(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1])));

		Assert.Equal(expected, parts[2]);
	}
}
