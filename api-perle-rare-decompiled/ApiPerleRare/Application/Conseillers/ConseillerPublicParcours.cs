using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace ApiPerleRare.Application.Conseillers;

/// <summary>
/// Remplace le texte de parcours d'une page conseiller WPBakery (perle-rare.com)
/// sans toucher au nom, a la photo ni aux icones de contact.
/// Un paragraphe dont le texte visible ne change pas garde son HTML (lien inclus).
/// </summary>
internal static class ConseillerPublicParcours
{
	private static readonly Regex NameParagraph = new Regex(
		"<p class=\"nom-chasseur-immobilier-noir\">.*?</p>",
		RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex HtmlParagraph = new Regex(
		"<p\\b[^>]*>.*?</p>",
		RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex Tags = new Regex("<[^>]+>", RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex Breaks = new Regex("<br\\s*/?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex Whitespace = new Regex("\\s+", RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex SlugPattern = new Regex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

	/// <summary>CP_RefConseiller de Georges-Louis Duquesne. Seule fiche publiée pour l'instant.</summary>
	internal const uint GeorgesLouisRef = 0;

	private static readonly HashSet<string> GeorgesLouisSlugs = new HashSet<string>(StringComparer.Ordinal)
	{
		"georges-louis-duquesne-dirigeant-co-fondateur-perle-rare-chasseur-immobilier",
		"georges-louis-duquesne-director"
	};

	internal static bool IsGeorgesLouis(uint conseillerId)
	{
		return conseillerId == GeorgesLouisRef;
	}

	internal static bool ShouldPublish(string previousUrl, string previousText, string nextUrl, string nextText)
	{
		return !string.Equals(previousUrl ?? "", nextUrl ?? "", StringComparison.Ordinal)
			|| !string.Equals(previousText ?? "", nextText ?? "", StringComparison.Ordinal);
	}

	internal static bool TryPerleRareSlug(string url, out string slug)
	{
		slug = null;
		if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri uri))
		{
			return false;
		}
		if (!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
			|| !uri.Host.Equals("perle-rare.com", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string[] parts = uri.AbsolutePath.Trim('/').Split('/');
		if (parts.Length != 2 || (parts[0] != "fr" && parts[0] != "en") || !SlugPattern.IsMatch(parts[1]))
		{
			return false;
		}
		if (!GeorgesLouisSlugs.Contains(parts[1]))
		{
			return false;
		}
		slug = parts[1];
		return true;
	}

	internal static bool TryReplaceBiography(string postContent, string parcours, out string updated)
	{
		updated = null;
		if (string.IsNullOrEmpty(postContent))
		{
			return false;
		}
		Match name = NameParagraph.Match(postContent);
		if (!name.Success)
		{
			return false;
		}
		int biographyStart = name.Index + name.Length;
		Match closing = HtmlParagraph.Match(postContent, biographyStart);
		while (closing.Success && closing.Value.IndexOf("mailto:", StringComparison.OrdinalIgnoreCase) < 0
			&& closing.Value.IndexOf("[/vc_column_text]", StringComparison.Ordinal) < 0)
		{
			closing = closing.NextMatch();
		}
		if (!closing.Success)
		{
			return false;
		}
		string oldRegion = postContent.Substring(biographyStart, closing.Index - biographyStart);
		List<string> oldParagraphs = new List<string>();
		foreach (Match paragraph in HtmlParagraph.Matches(oldRegion))
		{
			oldParagraphs.Add(paragraph.Value);
		}
		List<string> built = new List<string>();
		foreach (string paragraph in SplitParagraphs(parcours))
		{
			string visible = Normalize(paragraph);
			int reuse = oldParagraphs.FindIndex((string html) => NormalizeHtml(html) == visible);
			if (reuse >= 0)
			{
				built.Add(oldParagraphs[reuse]);
				oldParagraphs.RemoveAt(reuse);
			}
			else
			{
				built.Add(ToParagraph(paragraph));
			}
		}
		string region = built.Count == 0 ? "\n" : "\n" + string.Join("\n", built) + "\n";
		updated = postContent.Substring(0, biographyStart) + region + postContent.Substring(closing.Index);
		return true;
	}

	private static IEnumerable<string> SplitParagraphs(string parcours)
	{
		if (string.IsNullOrWhiteSpace(parcours))
		{
			yield break;
		}
		string[] parts = parcours.Replace("\r\n", "\n").Replace('\r', '\n').Split("\n\n");
		foreach (string part in parts)
		{
			string trimmed = part.Trim();
			if (trimmed.Length > 0)
			{
				yield return trimmed;
			}
		}
	}

	private static string ToParagraph(string paragraph)
	{
		string[] lines = paragraph.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		for (int i = 0; i < lines.Length; i++)
		{
			lines[i] = WebUtility.HtmlEncode(lines[i].Trim());
		}
		return "<p>" + string.Join("<br />\n", lines) + "</p>";
	}

	private static string NormalizeHtml(string html)
	{
		return Normalize(WebUtility.HtmlDecode(Tags.Replace(Breaks.Replace(html, " "), "")));
	}

	private static string Normalize(string text)
	{
		return Whitespace.Replace(text ?? "", " ").Trim();
	}
}
