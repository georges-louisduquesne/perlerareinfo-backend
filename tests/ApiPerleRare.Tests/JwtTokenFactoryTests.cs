using System;
using System.IdentityModel.Tokens.Jwt;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Xunit;

namespace ApiPerleRare.Tests;

public class JwtTokenFactoryTests
{
	[Fact]
	public void Issue_lifetime_is_48_hours()
	{
		var user = new ConseillersPersonnels
		{
			CpRefConseiller = 7,
			CpLogin = "anna",
			CpAdmin = true,
			CpNegociateur = true
		};

		string token = JwtTokenFactory.Issue(user, ApiFactory.JwtSecret);
		JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

		double minutesLeft = (jwt.ValidTo - DateTime.UtcNow).TotalMinutes;
		Assert.InRange(minutesLeft, JwtTokenFactory.LifetimeMinutes - 2, JwtTokenFactory.LifetimeMinutes + 2);
		Assert.Contains(jwt.Claims, c => c.Value == "7");
		Assert.Contains(jwt.Claims, c => c.Value == "anna");
		Assert.Contains(jwt.Claims, c => c.Value == "Admin");
		Assert.Contains(jwt.Claims, c => c.Value == "Negociateur");
	}

	[Fact]
	public void Issue_rejects_empty_secret()
	{
		var user = new ConseillersPersonnels { CpRefConseiller = 1, CpLogin = "x" };
		Assert.Throws<ArgumentException>(() => JwtTokenFactory.Issue(user, ""));
	}
}
