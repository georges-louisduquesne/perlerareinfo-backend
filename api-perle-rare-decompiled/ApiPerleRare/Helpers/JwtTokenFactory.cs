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
		byte[] key = Encoding.ASCII.GetBytes(secret);
		SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(claims),
			Expires = DateTime.UtcNow.AddMinutes(LifetimeMinutes),
			SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256")
		};
		SecurityToken secToken = tokenHandler.CreateToken(tokenDescriptor);
		return tokenHandler.WriteToken(secToken);
	}
}
