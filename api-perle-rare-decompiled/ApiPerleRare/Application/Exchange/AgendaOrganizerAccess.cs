using System.Globalization;
using System.Linq;
using System.Text;

namespace ApiPerleRare.Application.Exchange;

public static class AgendaOrganizerAccess
{
	public static bool IsCompanyApporteur(string login)
	{
		return Fold(login) == "perle rare";
	}

	public static bool LoginIsContactRole(
		string login,
		string apporteur,
		string secondApporteur,
		string negociateur,
		string secondNegociateur,
		string conseiller,
		string secondConseiller)
	{
		string key = Fold(login);
		if (key.Length == 0)
		{
			return false;
		}
		foreach (string candidate in new[]
		{
			IsCompanyApporteur(apporteur) ? null : apporteur,
			IsCompanyApporteur(secondApporteur) ? null : secondApporteur,
			negociateur,
			secondNegociateur,
			conseiller,
			secondConseiller
		})
		{
			if (Fold(candidate) == key)
			{
				return true;
			}
		}
		return false;
	}

	private static string Fold(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "";
		}
		string form = value.Trim().Normalize(NormalizationForm.FormD);
		char[] chars = form.Where((char c) => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
		return new string(chars).ToLowerInvariant();
	}
}
