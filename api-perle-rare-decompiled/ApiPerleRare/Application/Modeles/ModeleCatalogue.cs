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
