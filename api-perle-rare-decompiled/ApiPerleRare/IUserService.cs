using System.Collections.Generic;
using ApiPerleRare.Models;

namespace ApiPerleRare;

public interface IUserService
{
	ConseillersPersonnels Authenticate(string username, string password, out string token);

	bool SwitchDispo(int refConseiller, string remoteIpAddress);

	ConseillersPersonnels GetConseiller(string login);

	/// <summary>Prefetch missing conseillers in one query (same cache as GetConseiller).</summary>
	void WarmConseillers(IEnumerable<string> logins);

	string GetInfo();
}
