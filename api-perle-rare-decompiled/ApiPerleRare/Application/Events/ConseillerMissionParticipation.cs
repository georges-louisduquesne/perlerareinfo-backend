using System;
using System.Collections.Generic;

namespace ApiPerleRare.Application.Events;

/// <summary>
/// Évènements de genre MISSION ou TRANSACTION auxquels un conseiller participe
/// comme apporteur, négociateur ou conseiller du contact (y compris le second titulaire).
/// </summary>
internal static class ConseillerMissionParticipation
{
	internal static readonly string[] Genres = { "MISSION", "TRANSACTION" };

	internal static bool SameLogin(string field, string login)
	{
		if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(field))
		{
			return false;
		}
		return string.Equals(field.Trim(), login.Trim(), StringComparison.OrdinalIgnoreCase);
	}

	internal static string[] Roles(string login, string apporteur, string apporteur2, string negociateur, string negociateur2, string conseiller, string conseiller2)
	{
		List<string> roles = new List<string>(3);
		if (SameLogin(apporteur, login) || SameLogin(apporteur2, login))
		{
			roles.Add("apporteur");
		}
		if (SameLogin(negociateur, login) || SameLogin(negociateur2, login))
		{
			roles.Add("negociateur");
		}
		if (SameLogin(conseiller, login) || SameLogin(conseiller2, login))
		{
			roles.Add("conseiller");
		}
		return roles.ToArray();
	}

	internal static bool Participates(string login, string apporteur, string apporteur2, string negociateur, string negociateur2, string conseiller, string conseiller2)
	{
		return Roles(login, apporteur, apporteur2, negociateur, negociateur2, conseiller, conseiller2).Length > 0;
	}
}
