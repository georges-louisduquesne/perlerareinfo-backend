using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ApiPerleRare.Tests;

internal static class TestJwt
{
	public static string Create(bool admin = false)
	{
		var claims = new List<Claim>
		{
			new("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "42"),
			new("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "demo")
		};
		if (admin)
		{
			claims.Add(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "Admin"));
		}
		var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(ApiFactory.JwtSecret));
		var token = new JwtSecurityTokenHandler().CreateToken(new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(claims),
			Expires = DateTime.UtcNow.AddHours(1),
			SigningCredentials = new SigningCredentials(key, "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256")
		});
		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}
