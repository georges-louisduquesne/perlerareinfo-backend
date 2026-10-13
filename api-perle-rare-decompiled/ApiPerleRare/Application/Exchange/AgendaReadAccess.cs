using System;

namespace ApiPerleRare.Application.Exchange;

/// <summary>
/// L'agenda lit la boite de l'utilisateur connecte. Un admin peut en lire une autre.
/// </summary>
public static class AgendaReadAccess
{
	public static bool Allows(int callerId, bool callerIsAdmin, string key, string callerEmail)
	{
		if (callerIsAdmin)
		{
			return true;
		}
		string token = (key ?? string.Empty).Trim();
		if (token.Length == 0)
		{
			return false;
		}
		if (int.TryParse(token, out int id))
		{
			return id == callerId;
		}
		string email = (callerEmail ?? string.Empty).Trim();
		return email.Length > 0 && string.Equals(token, email, StringComparison.OrdinalIgnoreCase);
	}
}
