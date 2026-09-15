using System.Collections.Generic;
using ApiPerleRare.Models;

namespace ApiPerleRare;

public interface IUserService
{
	ConseillersPersonnels Authenticate(string username, string password, out string token);

	/// <summary>Re-issues a 48 h JWT if the conseiller is still active. No password.</summary>
	ConseillersPersonnels RefreshSession(int userId, out string token);

	bool SwitchDispo(int refConseiller, string remoteIpAddress);

	ConseillersPersonnels GetConseiller(string login);

	/// <summary>Prefetch missing conseillers in one query (same cache as GetConseiller).</summary>
	void WarmConseillers(IEnumerable<string> logins);

	string GetInfo();
}
