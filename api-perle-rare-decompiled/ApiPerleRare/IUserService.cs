using ApiPerleRare.Models;

namespace ApiPerleRare;

public interface IUserService
{
	ConseillersPersonnels Authenticate(string username, string password, out string token);

	bool SwitchDispo(int refConseiller, string remoteIpAddress);

	ConseillersPersonnels GetConseiller(string login);

	string GetInfo();
}
