using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace ApiPerleRare.Application.Conseillers;

/// <summary>
/// Met à jour une page conseiller WPBakery (perle-rare.com).
/// Le parcours remplace le texte entre le nom et les icônes, sans toucher au nom,
/// à la photo ni aux icônes. Le lien LinkedIn de la base remplace le href de l'icône.
/// Un paragraphe dont le texte visible ne change pas garde son HTML (lien inclus).
/// </summary>
internal enum LinkedInCardUpdate
{
	Absent,
	Unchanged,
	Updated,
	Unsafe
}

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

	private static readonly Regex LinkedInHref = new Regex(
		"(<a\\b[^>]*\\bhref\\s*=\\s*[\"'])https?://(?:[\\w.-]+\\.)?linkedin\\.com/[^\"']*([\"'])",
		RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex LinkedInAnchor = new Regex(
		"\\s*<a\\b[^>]*\\bhref\\s*=\\s*[\"']https?://(?:[\\w.-]+\\.)?linkedin\\.com/[^\"']*[\"'][^>]*>.*?</a>",
		RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex MailtoAnchor = new Regex(
		"<a\\b[^>]*\\bhref\\s*=\\s*[\"']mailto:[^\"']*[\"'][^>]*>.*?</a>",
		RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private const string LinkedInIcon = "<img class=\"alignnone size-full wp-image-4358\" src=\"https://perle-rare.com/wp-content/uploads/2021/02/linkedin-brands.png\" alt=\"\" width=\"30\" height=\"30\" />";

	private const string CardLinkedInIcon = "<img class=\"alignnone size-full wp-image-4374\" src=\"https://perle-rare.com/wp-content/uploads/2021/02/linkedin-brands-2.png\" alt=\"\" width=\"25\" height=\"26\" />";

	private static readonly Regex MailAnchor = new Regex(
		"<a\\b[^>]*\\bhref\\s*=\\s*[\"']mail(?:to|o):[^\"']*[\"'][^>]*>.*?</a>",
		RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	internal static bool ShouldPublish(string previousUrl, string previousText, string nextUrl, string nextText)
	{
		return !string.Equals(previousUrl ?? "", nextUrl ?? "", StringComparison.Ordinal)
			|| !string.Equals(previousText ?? "", nextText ?? "", StringComparison.Ordinal);
	}

	internal static bool ShouldPublishLinkedIn(string previous, string next)
	{
		return !string.Equals(previous ?? "", next ?? "", StringComparison.Ordinal);
	}

	internal static bool TryPerleRareSlug(string url, out string slug)
	{
		return TryPerleRarePage(url, out _, out slug);
	}

	internal static bool TryPerleRarePage(string url, out string language, out string slug)
	{
		language = null;
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
		language = parts[0];
		slug = parts[1];
		return true;
	}

	/// <summary>
	/// Pose l'URL LinkedIn de la base sur l'icône de contact.
	/// Une valeur vide retire l'icône. Une adresse qui n'est pas un profil LinkedIn est refusée.
	/// </summary>
	internal static bool TryReplaceLinkedIn(string postContent, string linkedInUrl, out string updated)
	{
		updated = null;
		if (string.IsNullOrEmpty(postContent) || !TryContactParagraph(postContent, out int index, out int length))
		{
			return false;
		}
		bool wantsLink = TryLinkedInHref(linkedInUrl, out string href);
		if (!string.IsNullOrWhiteSpace(linkedInUrl) && !wantsLink)
		{
			return false;
		}
		string paragraph = postContent.Substring(index, length);
		string nextParagraph = wantsLink ? ApplyLinkedInHref(paragraph, href) : LinkedInAnchor.Replace(paragraph, "");
		if (nextParagraph == null)
		{
			return false;
		}
		updated = postContent.Substring(0, index) + nextParagraph + postContent.Substring(index + length);
		return true;
	}

	/// <summary>
	/// Sur une page qui cite plusieurs conseillers, remplace uniquement le LinkedIn
	/// de la carte qui pointe vers la page du conseiller. Le texte court de la carte
	/// n'est pas le parcours de la fiche : il reste en place.
	/// </summary>
	internal static LinkedInCardUpdate ReplaceLinkedInNearProfile(string postContent, string profileUrl, string linkedInUrl, out string updated)
	{
		updated = null;
		if (string.IsNullOrEmpty(postContent) || !TryPerleRarePage(profileUrl, out string language, out string slug))
		{
			return LinkedInCardUpdate.Absent;
		}
		bool wantsLink = TryLinkedInHref(linkedInUrl, out string href);
		if (!string.IsNullOrWhiteSpace(linkedInUrl) && !wantsLink)
		{
			return LinkedInCardUpdate.Unsafe;
		}
		string marker = "/" + language + "/" + slug;
		List<(int Start, int End, string Next)> windows = new List<(int, int, string)>();
		int search = 0;
		while (search < postContent.Length)
		{
			int at = IndexOfBoundedSlug(postContent, marker, search);
			if (at < 0)
			{
				break;
			}
			int windowEnd = NextCardBoundary(postContent, at + marker.Length);
			string window = postContent.Substring(at, windowEnd - at);
			if (!TryReplaceCardWindow(window, wantsLink, href, out string nextWindow))
			{
				return LinkedInCardUpdate.Unsafe;
			}
			windows.Add((at, windowEnd, nextWindow));
			search = windowEnd;
		}
		if (windows.Count == 0)
		{
			return LinkedInCardUpdate.Absent;
		}
		string current = postContent;
		for (int i = windows.Count - 1; i >= 0; i--)
		{
			(int start, int end, string next) = windows[i];
			current = current.Substring(0, start) + next + current.Substring(end);
		}
		updated = current;
		return string.Equals(current, postContent, StringComparison.Ordinal)
			? LinkedInCardUpdate.Unchanged
			: LinkedInCardUpdate.Updated;
	}

	internal static bool TryReplaceBiography(string postContent, string parcours, out string updated)
	{
		updated = null;
		if (string.IsNullOrEmpty(postContent) || !TryContactParagraph(postContent, out int closingIndex, out _))
		{
			return false;
		}
		Match name = NameParagraph.Match(postContent);
		int biographyStart = name.Index + name.Length;
		string oldRegion = postContent.Substring(biographyStart, closingIndex - biographyStart);
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
		updated = postContent.Substring(0, biographyStart) + region + postContent.Substring(closingIndex);
		return true;
	}

	private static bool TryContactParagraph(string postContent, out int index, out int length)
	{
		index = 0;
		length = 0;
		Match name = NameParagraph.Match(postContent);
		if (!name.Success)
		{
			return false;
		}
		Match closing = HtmlParagraph.Match(postContent, name.Index + name.Length);
		while (closing.Success && closing.Value.IndexOf("mailto:", StringComparison.OrdinalIgnoreCase) < 0
			&& closing.Value.IndexOf("[/vc_column_text]", StringComparison.Ordinal) < 0)
		{
			closing = closing.NextMatch();
		}
		if (!closing.Success)
		{
			return false;
		}
		index = closing.Index;
		length = closing.Length;
		return true;
	}

	private static bool TryLinkedInHref(string value, out string href)
	{
		href = null;
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		string raw = value.Trim();
		if (raw.Length > 100 || raw.IndexOfAny(new char[] { ' ', '<', '>', '"', '\'' }) >= 0)
		{
			return false;
		}
		if (!Uri.TryCreate(raw, UriKind.Absolute, out Uri uri)
			|| (!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) && !uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
			|| !string.IsNullOrEmpty(uri.UserInfo))
		{
			return false;
		}
		string host = uri.Host.TrimEnd('.').ToLowerInvariant();
		string bare = host.StartsWith("www.", StringComparison.Ordinal) ? host.Substring(4) : host;
		if (bare != "linkedin.com" && !bare.EndsWith(".linkedin.com", StringComparison.Ordinal))
		{
			return false;
		}
		href = raw;
		return true;
	}

	private static int IndexOfBoundedSlug(string content, string marker, int start)
	{
		int at = content.IndexOf(marker, start, StringComparison.OrdinalIgnoreCase);
		while (at >= 0)
		{
			int after = at + marker.Length;
			if (after >= content.Length || !IsSlugChar(content[after]))
			{
				return at;
			}
			at = content.IndexOf(marker, after, StringComparison.OrdinalIgnoreCase);
		}
		return -1;
	}

	private static bool IsSlugChar(char value)
	{
		return (value >= 'a' && value <= 'z') || (value >= '0' && value <= '9') || value == '-';
	}

	private static int NextCardBoundary(string content, int start)
	{
		int at = content.IndexOf("<p class=\"nom-chasseur-immobilier\"", start, StringComparison.Ordinal);
		int cap = Math.Min(content.Length, start + 4000);
		if (at < 0 || at > cap)
		{
			return cap;
		}
		return at;
	}

	private static bool TryReplaceCardWindow(string window, bool wantsLink, string href, out string updated)
	{
		updated = window;
		int links = LinkedInAnchor.Matches(window).Count;
		if (links > 1)
		{
			updated = null;
			return false;
		}
		if (links == 1)
		{
			updated = wantsLink
				? LinkedInHref.Replace(window, "$1" + href + "$2", 1)
				: LinkedInAnchor.Replace(window, "", 1);
			return updated != null;
		}
		if (!wantsLink)
		{
			return true;
		}
		Match mail = MailAnchor.Match(window);
		if (!mail.Success)
		{
			return true;
		}
		updated = window.Insert(mail.Index + mail.Length, " <a href=\"" + href + "\">" + CardLinkedInIcon + "</a>");
		return true;
	}

	private static string ApplyLinkedInHref(string paragraph, string href)
	{
		if (LinkedInHref.IsMatch(paragraph))
		{
			return LinkedInHref.Replace(paragraph, "$1" + href + "$2");
		}
		Match mail = MailtoAnchor.Match(paragraph);
		if (!mail.Success)
		{
			return null;
		}
		string anchor = "<a href=\"" + href + "\">" + LinkedInIcon + "</a>";
		return paragraph.Insert(mail.Index + mail.Length, " " + anchor);
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
