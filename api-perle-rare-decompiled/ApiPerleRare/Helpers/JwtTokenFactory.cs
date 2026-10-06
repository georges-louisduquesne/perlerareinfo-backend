using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiPerleRare.Models;
using Microsoft.IdentityModel.Tokens;

namespace ApiPerleRare.Helpers;

/// <summary>
/// Issues the CRM Bearer JWT. Lifetime is 48 h; sliding renewal is <c>GET /api/User/refresh</c>
/// while the current token is still valid (inactivity window, not "password every 2 days").
/// </summary>
public static class JwtTokenFactory
{
	public const int LifetimeMinutes = 2880;

	/// <summary>
	/// Raw ASCII bytes of the secret, like the .NET 7 production API. The production secret is
	/// shorter than 256 bits: stretching it would make tokens unreadable by the API that still
	/// serves invoices (Aspose) and the mobile app with the same Bearer token.
	/// </summary>
	public static byte[] SigningKeyBytes(string secret)
	{
		return Encoding.ASCII.GetBytes(secret ?? "");
	}

	public static SymmetricSecurityKey SigningKey(string secret)
	{
		return LegacyHmacKey.Create(SigningKeyBytes(secret));
	}

	public static string Issue(ConseillersPersonnels user, string secret)
	{
		if (user == null)
		{
			throw new ArgumentNullException(nameof(user));
		}
		if (string.IsNullOrEmpty(secret))
		{
			throw new ArgumentException("JWT secret is required.", nameof(secret));
		}

		List<Claim> claims = new List<Claim>
		{
			new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", user.CpRefConseiller.ToString()),
			new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", user.CpLogin)
		};
		if (user.CpAdmin)
		{
			claims.Add(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "Admin"));
		}
		if (user.CpNegociateur)
		{
			claims.Add(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "Negociateur"));
		}

		JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler
		{
			TokenLifetimeInMinutes = LifetimeMinutes
		};
		SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(claims),
			Expires = DateTime.UtcNow.AddMinutes(LifetimeMinutes),
			SigningCredentials = new SigningCredentials(SigningKey(secret), "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256")
		};
		SecurityToken secToken = tokenHandler.CreateToken(tokenDescriptor);
		return tokenHandler.WriteToken(secToken);
	}
}
