using System.Collections.Generic;
using ApiPerleRare.Models;

namespace ApiPerleRare.Tests;

internal class FakeUserService : IUserService
{
	public ConseillersPersonnels Authenticate(string username, string password, out string token)
	{
		if (username == "demo" && password == "demo")
		{
			token = "fake-front-token";
			return new ConseillersPersonnels
			{
				CpRefConseiller = 42,
				CpPrenom = "Jean",
				CpNomFamille = "Test",
				CpLogin = "demo",
				CpAdmin = true,
				CpNegociateur = false,
				CpAutoLogin = "0601001",
				CpDispo = 1,
				CpMel = "jean@test.local"
			};
		}
		token = null;
		return null;
	}

	public bool SwitchDispo(int refConseiller, string remoteIpAddress) => false;

	public ConseillersPersonnels GetConseiller(string login) => null;

	public void WarmConseillers(IEnumerable<string> logins)
	{
	}

	public string GetInfo() => "DBName=perle-rareinfo";
}
