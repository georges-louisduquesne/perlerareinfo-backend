using System;
using System.Globalization;
using ApiPerleRare.Helpers;

namespace ApiPerleRare.Application.Contacts;

public sealed class RegistreMandatRow
{
	public int Ref { get; set; }

	public int? Numero { get; set; }

	public string Mandat { get; set; }

	/// <summary>Date SQL <c>yyyy-MM-dd</c>, sans décalage d’heure.</summary>
	public string Date { get; set; }

	public string Nom { get; set; }

	public string Prenom { get; set; }

	public string Adresse { get; set; }

	public string Cp { get; set; }

	public string Ville { get; set; }

	public string Pays { get; set; }
}

/// <summary>
/// Même liste que <c>pages/reg_mandats.php</c> : tous les contacts signés,
/// sans le filtre « mes dossiers ».
/// </summary>
public static class RegistreMandatsQuery
{
	public const string Sql =
		"SELECT C_RefContact, C_NumeroMandat, C_Mandat, DATE_FORMAT(C_Date, '%Y-%m-%d'), " +
		"C_NomFamille, C_Prenom, C_Adresse, C_CodePostal, C_Ville, C_PaysRegion " +
		"FROM contacts_recherche " +
		"WHERE C_Date <> '0000-00-00' AND C_NumeroMandat > 0 " +
		"ORDER BY C_NumeroMandat";

	public static RegistreMandatRow FromValues(
		object refValue,
		object numero,
		object mandat,
		object date,
		object nom,
		object prenom,
		object adresse,
		object cp,
		object ville,
		object pays)
	{
		return new RegistreMandatRow
		{
			Ref = ToInt(refValue),
			Numero = PositiveNumero(numero),
			Mandat = Text(mandat),
			Date = DateText(date),
			Nom = Text(nom),
			Prenom = Text(prenom),
			Adresse = Text(adresse),
			Cp = Text(cp),
			Ville = Text(ville),
			Pays = Text(pays)
		};
	}

	public static string DateText(object value)
	{
		string text = Raw(value);
		if (text.Length >= 10 && text[4] == '-' && text[7] == '-')
		{
			return text.Substring(0, 10);
		}
		return "";
	}

	private static string Text(object value)
	{
		return EncodingHelper.FixEncoding(Raw(value)) ?? "";
	}

	private static string Raw(object value)
	{
		if (value == null || value is DBNull)
		{
			return "";
		}
		return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
	}

	private static int ToInt(object value)
	{
		int? n = ToIntOrNull(value);
		return n ?? 0;
	}

	private static int? PositiveNumero(object value)
	{
		int? n = ToIntOrNull(value);
		if (n == null || n <= 0)
		{
			return null;
		}
		return n;
	}

	private static int? ToIntOrNull(object value)
	{
		if (value == null || value is DBNull)
		{
			return null;
		}
		if (value is bool)
		{
			return null;
		}
		return Convert.ToInt32(value, CultureInfo.InvariantCulture);
	}
}
