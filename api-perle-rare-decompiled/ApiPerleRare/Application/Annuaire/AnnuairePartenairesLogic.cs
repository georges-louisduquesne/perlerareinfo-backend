using System;
using System.Collections.Generic;

namespace ApiPerleRare.Application.Annuaire;

/// <summary>
/// Shared partner directory. One list for every signed-in user; only an admin replaces it.
/// </summary>
internal static class AnnuairePartenairesLogic
{
	public const int MaxRows = 80;

	public static List<AnnuairePartenaireRow> Clean(IEnumerable<AnnuairePartenaireRow> rows)
	{
		List<AnnuairePartenaireRow> result = new List<AnnuairePartenaireRow>();
		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
		if (rows == null)
		{
			return result;
		}
		foreach (AnnuairePartenaireRow raw in rows)
		{
			if (result.Count >= MaxRows)
			{
				break;
			}
			if (raw == null)
			{
				continue;
			}
			AnnuairePartenaireRow row = new AnnuairePartenaireRow
			{
				Id = Clip(raw.Id, 64),
				Categorie = Clip(raw.Categorie, 160),
				Nom = Clip(raw.Nom, 160),
				Societe = Clip(raw.Societe, 160),
				Site = Clip(raw.Site, 300),
				Telephone = Clip(raw.Telephone, 40),
				Email = Clip(raw.Email, 160),
				Adresse = Clip(raw.Adresse, 300),
				TelephoneFixe = Clip(raw.TelephoneFixe, 40)
			};
			if (IsBlank(row))
			{
				continue;
			}
			if (row.Id.Length == 0 || !seen.Add(row.Id))
			{
				row.Id = NewId(seen);
				seen.Add(row.Id);
			}
			result.Add(row);
		}
		return result;
	}

	private static bool IsBlank(AnnuairePartenaireRow row)
	{
		return row.Categorie.Length == 0
			&& row.Nom.Length == 0
			&& row.Societe.Length == 0
			&& row.Site.Length == 0
			&& row.Telephone.Length == 0
			&& row.Email.Length == 0
			&& row.Adresse.Length == 0
			&& row.TelephoneFixe.Length == 0;
	}

	private static string Clip(string value, int max)
	{
		string text = (value ?? "").Trim();
		if (text.Length <= max)
		{
			return text;
		}
		return text.Substring(0, max);
	}

	private static string NewId(HashSet<string> seen)
	{
		for (int i = 0; i < 8; i++)
		{
			string id = "p-" + Guid.NewGuid().ToString("N").Substring(0, 12);
			if (!seen.Contains(id))
			{
				return id;
			}
		}
		return "p-" + Guid.NewGuid().ToString("N");
	}
}
