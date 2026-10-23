using System;
using System.Globalization;
using System.Text;

namespace ApiPerleRare.Application.Modeles;

public sealed class ModeleCatalogueRow
{
	public int Ref { get; set; }

	public short Categorie { get; set; }

	public string CategorieNom { get; set; }

	public string Titre { get; set; }

	public string TitreAbr { get; set; }

	public bool HasDoc { get; set; }

	public bool HasPdf { get; set; }
}

public static class ModeleCatalogueQuery
{
	/// <summary>Présence des fichiers sans lire les blobs.</summary>
	public const string Sql =
		"SELECT m.MC_Ref, m.MC_Categorie, t.TM_Nom, m.MC_Titre, m.MC_Titre_abr, " +
		"(m.MC_Fichier_doc IS NOT NULL), (m.MC_Fichier_pdf IS NOT NULL) " +
		"FROM modeles_commerciaux m " +
		"INNER JOIN types_modeles t ON t.TM_Ref = m.MC_Categorie " +
		"WHERE m.MC_Categorie IS NOT NULL " +
		"ORDER BY m.MC_Categorie, m.MC_Titre";

	public static ModeleCatalogueRow FromValues(object refValue, object categorie, object nom, object titre, object abr, object hasDoc, object hasPdf)
	{
		return new ModeleCatalogueRow
		{
			Ref = Convert.ToInt32(refValue, CultureInfo.InvariantCulture),
			Categorie = Convert.ToInt16(categorie, CultureInfo.InvariantCulture),
			CategorieNom = Text(nom),
			Titre = Text(titre),
			TitreAbr = Text(abr),
			HasDoc = Flag(hasDoc),
			HasPdf = Flag(hasPdf)
		};
	}

	public static bool Flag(object value)
	{
		if (value == null || value is DBNull)
		{
			return false;
		}
		if (value is bool bit)
		{
			return bit;
		}
		return Convert.ToInt64(value, CultureInfo.InvariantCulture) != 0;
	}

	private static string Text(object value)
	{
		if (value == null || value is DBNull)
		{
			return null;
		}
		return Convert.ToString(value, CultureInfo.InvariantCulture);
	}
}

public static class ModeleFichierNames
{
	public static bool TryResolve(string kind, out bool pdf)
	{
		if (string.Equals(kind, "pdf", StringComparison.OrdinalIgnoreCase))
		{
			pdf = true;
			return true;
		}
		if (string.Equals(kind, "doc", StringComparison.OrdinalIgnoreCase))
		{
			pdf = false;
			return true;
		}
		pdf = false;
		return false;
	}

	public static string ContentType(bool pdf)
	{
		return pdf ? "application/pdf" : "application/msword";
	}

	public static string DownloadName(string abr, bool pdf)
	{
		string raw = (abr ?? "").Trim().Normalize(NormalizationForm.FormD);
		StringBuilder sb = new StringBuilder();
		foreach (char c in raw)
		{
			if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
			{
				continue;
			}
			if (char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_')
			{
				sb.Append(c);
			}
		}
		string name = sb.ToString().Trim();
		if (name.Length == 0)
		{
			name = "modele";
		}
		else if (name.Length > 80)
		{
			name = name.Substring(0, 80).Trim();
		}
		return name + (pdf ? ".pdf" : ".doc");
	}
}
