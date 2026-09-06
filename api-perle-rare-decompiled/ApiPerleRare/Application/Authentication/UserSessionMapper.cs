using ApiPerleRare.Entities;
using ApiPerleRare.Models;

namespace ApiPerleRare.Application.Authentication;

/// <summary>
/// Maps the persistence user to the HTTP session the Angular front already consumes.
/// Property names on <see cref="UserModel"/> are the frozen contract (camelCase JSON).
/// </summary>
public static class UserSessionMapper
{
	public static UserModel ToSession(ConseillersPersonnels user, string token)
	{
		return new UserModel
		{
			FirstName = user.CpPrenom,
			LastName = user.CpNomFamille,
			Login = user.CpLogin,
			Id = user.CpRefConseiller,
			Token = token,
			IsAdmin = user.CpAdmin,
			IsNegociateur = user.CpNegociateur,
			AutoLogin = user.CpAutoLogin,
			Dispo = user.CpDispo,
			Filter = !user.CpAdmin,
			Email = user.CpMel
		};
	}
}
