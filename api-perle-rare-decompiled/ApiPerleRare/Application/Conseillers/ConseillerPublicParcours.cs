using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace ApiPerleRare.Application.Conseillers;

/// <summary>
/// Met à jour une page conseiller WPBakery (perle-rare.com) et les cartes qui y renvoient.
/// Données maîtresses synchronisées depuis conseillers_personnels :
/// prénom/nom, fonction (page française), parcours FR/EN, numéro RSAC, e-mail, LinkedIn, fiche Google et portrait 555×555.
/// Un paragraphe du parcours dont le texte visible ne change pas garde son HTML (lien inclus).
/// </summary>
internal enum LinkedInCardUpdate
{
	Absent,
	Unchanged,
	Updated,
	Unsafe
}

internal sealed class ConseillerCardChanges
{
	public bool UpdateName { get; init; }

	public string FullName { get; init; }

	public bool UpdateEmail { get; init; }

	public string Email { get; init; }

	public bool UpdateLinkedIn { get; init; }

	public string LinkedIn { get; init; }

	public bool UpdateGoogle { get; init; }

	public string Google { get; init; }

	public bool Any => UpdateName || UpdateEmail || UpdateLinkedIn || UpdateGoogle;
}

internal static class ConseillerPublicParcours
{
	private static readonly Regex NameParagraph = new Regex(
		"<p class=\"nom-chasseur-immobilier-noir\">.*?</p>",
		RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex CardNameParagraph = new Regex(
		"<p class=\"nom-chasseur-immobilier\">.*?</p>",
		RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex NameStrongTag = new Regex(
		"(<(?:strong|b)\\b[^>]*>)(.*?)(</(?:strong|b)>)",
		RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex HtmlParagraph = new Regex(
		"<p\\b[^>]*>.*?</p>",
		RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex VcColumnTextBlock = new Regex(
		"\\[vc_column_text[^\\]]*\\].*?\\[/vc_column_text\\]",
		RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex PerleRareProfileHref = new Regex(
		"https?://perle-rare\\.com/(fr|en)/([a-z0-9]+(?:-[a-z0-9]+)*)/?(?=[\"'])",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex Tags = new Regex("<[^>]+>", RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex Breaks = new Regex("<br\\s*/?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex Whitespace = new Regex("\\s+", RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex VcSingleImage = new Regex(
		"\\[vc_single_image\\b[^\\]]*\\]",
		RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex VcSingleImageId = new Regex(
		"\\bimage\\s*=\\s*\"(\\d+)\"",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex PortraitUploadPath = new Regex(
		"^\\d{4}/\\d{2}/[^/\\\\]+\\.jpg$",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex SlugPattern = new Regex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex EmailPattern = new Regex("^[^@\\s<>'\"()]+@[^@\\s<>'\"()]+\\.[^@\\s<>'\"()]+$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex LinkedInHref = new Regex(
		"(<a\\b[^>]*\\bhref\\s*=\\s*[\"'])https?://(?:[\\w.-]+\\.)?linkedin\\.com/[^\"']*([\"'])",
		RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex LinkedInAnchor = new Regex(
		"\\s*<a\\b[^>]*\\bhref\\s*=\\s*[\"']https?://(?:[\\w.-]+\\.)?linkedin\\.com/[^\"']*[\"'][^>]*>.*?</a>",
		RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex OrphanLinkedInImg = new Regex(
		"\\s*<img\\b[^>]*linkedin-brands(?:-2)?\\.png[^>]*>",
		RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex GoogleHref = new Regex(
		"(<a\\b[^>]*\\bhref\\s*=\\s*[\"'])https?://(?:[\\w.-]+\\.)?(?:google\\.com|goo\\.gl|g\\.page)/[^\"']*([\"'])",
		RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex GoogleAnchor = new Regex(
		"\\s*<a\\b[^>]*\\bhref\\s*=\\s*[\"']https?://(?:[\\w.-]+\\.)?(?:google\\.com|goo\\.gl|g\\.page)/[^\"']*[\"'][^>]*>.*?</a>",
		RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex MailAnchor = new Regex(
		"<a\\b[^>]*\\bhref\\s*=\\s*[\"'](?:mail(?:to|o):[^\"']*|[^\"'\\s/<>]+@[^\"'\\s/<>]+)[\"'][^>]*>(.*?)</a>",
		RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex AnchorHrefAttr = new Regex(
		"(\\bhref\\s*=\\s*[\"'])[^\"']*([\"'])",
		RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex RsacBlock = new Regex(
		"(?:<p\\b[^>]*\\bclass\\s*=\\s*[\"'][^\"']*\\brsac-conseiller\\b[^\"']*[\"'][^>]*>.*?</p>"
		+ "|<p\\b[^>]*>(?:(?!</p>).)*?\\b(?:Agent commercial inscrit au RSAC|Commercial agent registered with the RSAC)\\b.*?</p>"
		+ "|<span\\b[^>]*>(?:(?!</span>).)*?\\b(?:Agent commercial inscrit au RSAC|Commercial agent registered with the RSAC)\\b.*?</span>"
		+ "|(?<=^|\\r?\\n)[ \\t]*(?:Agent commercial inscrit au RSAC|Commercial agent registered with the RSAC)[^\\r\\n]*)",
		RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private static readonly Regex OpenParagraphTag = new Regex(
		"<p\\b[^>]*>",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	private const string EnvelopeIcon = "<img class=\"alignnone size-full wp-image-4359\" src=\"https://perle-rare.com/wp-content/uploads/2021/02/envelope-solid.png\" alt=\"\" width=\"30\" height=\"30\" />";

	private const string CardEnvelopeIcon = "<img class=\"alignnone size-full wp-image-4375\" src=\"https://perle-rare.com/wp-content/uploads/2021/02/envelope-solid-2.png\" alt=\"\" width=\"30\" height=\"22\" />";

	private const string LinkedInIcon = "<img class=\"alignnone size-full wp-image-4358\" src=\"https://perle-rare.com/wp-content/uploads/2021/02/linkedin-brands.png\" alt=\"\" width=\"30\" height=\"30\" />";

	private const string CardLinkedInIcon = "<img class=\"alignnone size-full wp-image-4374\" src=\"https://perle-rare.com/wp-content/uploads/2021/02/linkedin-brands-2.png\" alt=\"\" width=\"25\" height=\"26\" />";

	private const string GoogleIcon = "<img class=\"alignnone size-full\" src=\"https://perle-rare.com/wp-content/uploads/2021/02/google-brands.png\" alt=\"Google\" width=\"30\" height=\"30\" />";

	private const string CardGoogleIcon = "<img class=\"alignnone size-full\" src=\"https://perle-rare.com/wp-content/uploads/2021/02/google-brands-2.png\" alt=\"Google\" width=\"25\" height=\"26\" />";

	internal static bool ShouldPublish(string previousUrl, string previousText, string nextUrl, string nextText)
	{
		return !string.Equals(previousUrl ?? "", nextUrl ?? "", StringComparison.Ordinal)
			|| !string.Equals(previousText ?? "", nextText ?? "", StringComparison.Ordinal);
	}

	internal static bool ShouldPublishLinkedIn(string previous, string next)
	{
		return ShouldPublishText(previous, next);
	}

	internal static bool ShouldPublishText(string previous, string next)
	{
		return !string.Equals((previous ?? "").Trim(), (next ?? "").Trim(), StringComparison.Ordinal);
	}

	/// <summary>
	/// URL maîtresse du portrait publié : https://perle-rare.com/wp-content/uploads/AAAA/MM/fichier.jpg
	/// </summary>
	internal static bool TryParseSitePortraitUrl(string url, out string relativePath)
	{
		relativePath = "";
		string raw = (url ?? "").Trim();
		if (raw.Length == 0 || raw.Length > 512)
		{
			return false;
		}
		if (!Uri.TryCreate(raw, UriKind.Absolute, out Uri uri))
		{
			return false;
		}
		if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (!string.Equals(uri.IdnHost, "perle-rare.com", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo))
		{
			return false;
		}
		const string prefix = "/wp-content/uploads/";
		string path = Uri.UnescapeDataString(uri.AbsolutePath);
		if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string relative = path.Substring(prefix.Length);
		if (relative.IndexOf("..", StringComparison.Ordinal) >= 0 || !PortraitUploadPath.IsMatch(relative))
		{
			return false;
		}
		relativePath = relative;
		return true;
	}

	internal static bool AttachmentMetadataIsSquare555(string serialized)
	{
		// La première paire width/height est la taille d'origine. Un recadrage
		// 555 imbriqué dans "sizes" ne doit pas valider un fichier plus grand.
		return TryPhpInt(serialized, "\"width\";i:", out int width)
			&& TryPhpInt(serialized, "\"height\";i:", out int height)
			&& width == 555
			&& height == 555;
	}

	private static bool TryPhpInt(string serialized, string marker, out int value)
	{
		value = 0;
		if (string.IsNullOrEmpty(serialized))
		{
			return false;
		}
		int at = serialized.IndexOf(marker, StringComparison.Ordinal);
		if (at < 0)
		{
			return false;
		}
		int start = at + marker.Length;
		int end = start;
		while (end < serialized.Length && char.IsDigit(serialized[end]))
		{
			end++;
		}
		if (end == start || end >= serialized.Length || serialized[end] != ';')
		{
			return false;
		}
		return int.TryParse(serialized.Substring(start, end - start), NumberStyles.None, CultureInfo.InvariantCulture, out value);
	}

	/// <summary>
	/// Remplace l'unique portrait WPBakery de la fiche. Les icônes e-mail et LinkedIn ne sont pas des shortcodes image.
	/// </summary>
	internal static bool TryReplacePortrait(string postContent, long attachmentId, out string updated)
	{
		updated = postContent ?? "";
		if (attachmentId <= 0 || updated.Length == 0)
		{
			return false;
		}
		MatchCollection blocks = VcSingleImage.Matches(updated);
		if (blocks.Count != 1)
		{
			return false;
		}
		string block = blocks[0].Value;
		MatchCollection ids = VcSingleImageId.Matches(block);
		if (ids.Count != 1)
		{
			return false;
		}
		string nextBlock = block.Remove(ids[0].Index, ids[0].Length).Insert(ids[0].Index, "image=\"" + attachmentId.ToString() + "\"");
		updated = updated.Remove(blocks[0].Index, blocks[0].Length).Insert(blocks[0].Index, nextBlock);
		return true;
	}

	internal static string BuildFullName(string prenom, string nom)
	{
		string p = Whitespace.Replace((prenom ?? "").Trim(), " ");
		string n = Whitespace.Replace((nom ?? "").Trim(), " ");
		if (p.Length == 0)
		{
			return n;
		}
		if (n.Length == 0)
		{
			return p;
		}
		return p + " " + n;
	}

	internal static bool TryFullName(string prenom, string nom, out string fullName)
	{
		fullName = BuildFullName(prenom, nom);
		if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 150 || fullName.IndexOfAny(new char[] { '<', '>', '\r', '\n' }) >= 0)
		{
			fullName = null;
			return false;
		}
		return true;
	}

	/// <summary>
	/// Retourne le numéro RSAC à publier : uniquement si CP_Statut est "Agent commercial"
	/// et CP_Numrsac est renseigné et sûr. Vide signifie "aucune mention RSAC".
	/// Null signifie "valeur non sûre, ne rien toucher".
	/// </summary>
	internal static string EffectiveRsac(string statut, string numRsac)
	{
		if (!string.Equals((statut ?? "").Trim(), "Agent commercial", StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}
		string raw = (numRsac ?? "").Trim();
		if (raw.Length == 0)
		{
			return "";
		}
		if (raw.Length > 60 || raw.IndexOfAny(new char[] { '<', '>', '"', '\'', '\r', '\n' }) >= 0)
		{
			return null;
		}
		return raw;
	}

	internal static bool TryEmail(string value, out string email)
	{
		email = null;
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		string raw = value.Trim();
		if (raw.Length > 254 || !EmailPattern.IsMatch(raw))
		{
			return false;
		}
		email = raw;
		return true;
	}

	internal static bool TryGoogleHref(string value, out string href)
	{
		href = null;
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		string raw = value.Trim();
		if (raw.Length > 512 || raw.IndexOfAny(new char[] { ' ', '<', '>', '"', '\'' }) >= 0)
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
		bool allowed = bare == "google.com" || bare.EndsWith(".google.com", StringComparison.Ordinal)
			|| bare == "goo.gl" || bare.EndsWith(".goo.gl", StringComparison.Ordinal)
			|| bare == "g.page" || bare.EndsWith(".g.page", StringComparison.Ordinal);
		if (!allowed)
		{
			return false;
		}
		href = raw;
		return true;
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
	/// Met à jour le prénom et le nom dans le bloc titre (nom-chasseur-immobilier-noir)
	/// sans modifier la ligne de fonction qui suit.
	/// </summary>
	internal static bool TryReplaceProfileName(string postContent, string fullName, out string updated)
	{
		updated = null;
		if (string.IsNullOrEmpty(postContent) || string.IsNullOrWhiteSpace(fullName) || fullName.Length > 150 || fullName.IndexOfAny(new char[] { '<', '>', '\r', '\n' }) >= 0)
		{
			return false;
		}
		Match namePara = NameParagraph.Match(postContent);
		if (!namePara.Success)
		{
			return false;
		}
		Match strong = NameStrongTag.Match(namePara.Value);
		if (!strong.Success)
		{
			return false;
		}
		string encoded = WebUtility.HtmlEncode(fullName.Trim());
		string replacedPara = namePara.Value.Substring(0, strong.Index)
			+ strong.Groups[1].Value + encoded + strong.Groups[3].Value
			+ namePara.Value.Substring(strong.Index + strong.Length);
		updated = postContent.Substring(0, namePara.Index) + replacedPara + postContent.Substring(namePara.Index + namePara.Length);
		return true;
	}

	/// <summary>
	/// Remplace la ligne de titre sous le nom.
	/// Un titre vide ne retire pas le titre déjà publié.
	/// La page française lit CP_Fonction, la page anglaise CP_FonctionEn.
	/// </summary>
	internal static bool TryReplaceJobTitle(string postContent, string jobTitle, out string updated)
	{
		updated = null;
		string title = (jobTitle ?? "").Trim();
		if (string.IsNullOrEmpty(postContent) || title.Length == 0 || title.Length > 120 || title.IndexOfAny(new char[] { '<', '>', '\r', '\n' }) >= 0)
		{
			return false;
		}
		Match namePara = NameParagraph.Match(postContent);
		if (!namePara.Success)
		{
			return false;
		}
		MatchCollection breaks = Regex.Matches(namePara.Value, "<br\\s*/?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (breaks.Count == 0)
		{
			return false;
		}
		Match br = breaks[breaks.Count - 1];
		int textStart = br.Index + br.Length;
		int close = namePara.Value.LastIndexOf("</p>", StringComparison.OrdinalIgnoreCase);
		if (close < textStart)
		{
			return false;
		}
		string current = namePara.Value.Substring(textStart, close - textStart);
		if (Regex.IsMatch(current, "<[^>]+>", RegexOptions.CultureInvariant))
		{
			return false;
		}
		string prefix = current.Substring(0, current.Length - current.TrimStart().Length);
		if (prefix.Length == 0)
		{
			prefix = " ";
		}
		string nextPara = namePara.Value.Substring(0, textStart) + prefix + WebUtility.HtmlEncode(title) + namePara.Value.Substring(close);
		updated = postContent.Substring(0, namePara.Index) + nextPara + postContent.Substring(namePara.Index + namePara.Length);
		return true;
	}

	/// <summary>
	/// Pose ou retire la mention légale RSAC entre le parcours et les icônes de contact.
	/// </summary>
	internal static bool TryReplaceRsac(string postContent, string language, string statut, string numRsac, out string updated)
	{
		updated = null;
		if (string.IsNullOrEmpty(postContent) || !TryProfileRegions(postContent, out ProfileRegions regions))
		{
			return false;
		}
		string effective = EffectiveRsac(statut, numRsac);
		if (effective == null)
		{
			return false;
		}
		string bodyRegion = postContent.Substring(regions.BodyStart, regions.ContactStart - regions.BodyStart);
		string withoutRsac = StripRsac(bodyRegion);
		string nextBody;
		if (effective.Length == 0)
		{
			nextBody = string.IsNullOrWhiteSpace(withoutRsac)
				? "\n"
				: "\n" + withoutRsac.Trim() + "\n";
		}
		else
		{
			string rsacLine = FormatRsacParagraph(language, effective);
			nextBody = string.IsNullOrWhiteSpace(withoutRsac)
				? "\n" + rsacLine + "\n"
				: "\n" + withoutRsac.Trim() + "\n" + rsacLine + "\n";
		}
		updated = postContent.Substring(0, regions.BodyStart) + nextBody + postContent.Substring(regions.ContactStart);
		return true;
	}

	/// <summary>
	/// Met à jour le lien e-mail (icône ou texte) sur la page dédiée du conseiller.
	/// Corrige les anciens liens cassés (mailo: ou sans schéma mailto:).
	/// Une valeur vide retire le lien e-mail.
	/// </summary>
	internal static bool TryReplaceEmail(string postContent, string emailValue, out string updated)
	{
		updated = null;
		if (string.IsNullOrEmpty(postContent) || !TryProfileRegions(postContent, out ProfileRegions regions))
		{
			return false;
		}
		bool wantsEmail = TryEmail(emailValue, out string email);
		if (!string.IsNullOrWhiteSpace(emailValue) && !wantsEmail)
		{
			return false;
		}
		string contact = postContent.Substring(regions.ContactInnerStart, regions.ContactInnerLength);
		if (!TryApplyEmail(contact, wantsEmail, email, EnvelopeIcon, out string nextContact))
		{
			return false;
		}
		updated = postContent.Substring(0, regions.ContactInnerStart) + nextContact + postContent.Substring(regions.ContactInnerStart + regions.ContactInnerLength);
		return true;
	}

	/// <summary>
	/// Pose l'URL LinkedIn de la base sur l'icône de contact.
	/// Une valeur vide retire l'icône. Une adresse qui n'est pas un profil LinkedIn est refusée.
	/// </summary>
	internal static bool TryReplaceLinkedIn(string postContent, string linkedInUrl, out string updated)
	{
		updated = null;
		if (string.IsNullOrEmpty(postContent) || !TryProfileRegions(postContent, out ProfileRegions regions))
		{
			return false;
		}
		bool wantsLink = TryLinkedInHref(linkedInUrl, out string href);
		if (!string.IsNullOrWhiteSpace(linkedInUrl) && !wantsLink)
		{
			return false;
		}
		string contact = postContent.Substring(regions.ContactInnerStart, regions.ContactInnerLength);
		if (!TryApplyLinkedIn(contact, wantsLink, href, LinkedInIcon, out string nextContact))
		{
			return false;
		}
		updated = postContent.Substring(0, regions.ContactInnerStart) + nextContact + postContent.Substring(regions.ContactInnerStart + regions.ContactInnerLength);
		return true;
	}

	/// <summary>
	/// Pose l'URL de fiche Google sur la page dédiée du conseiller.
	/// Une valeur vide retire l'icône. Une adresse hors Google est refusée.
	/// </summary>
	internal static bool TryReplaceGoogle(string postContent, string googleUrl, out string updated)
	{
		updated = null;
		if (string.IsNullOrEmpty(postContent) || !TryProfileRegions(postContent, out ProfileRegions regions))
		{
			return false;
		}
		bool wantsLink = TryGoogleHref(googleUrl, out string href);
		if (!string.IsNullOrWhiteSpace(googleUrl) && !wantsLink)
		{
			return false;
		}
		string contact = postContent.Substring(regions.ContactInnerStart, regions.ContactInnerLength);
		if (!TryApplyGoogle(contact, wantsLink, href, GoogleIcon, out string nextContact))
		{
			return false;
		}
		updated = postContent.Substring(0, regions.ContactInnerStart) + nextContact + postContent.Substring(regions.ContactInnerStart + regions.ContactInnerLength);
		return true;
	}

	/// <summary>
	/// Sur une page qui cite plusieurs conseillers, remplace uniquement le LinkedIn
	/// de la carte qui pointe vers la page du conseiller.
	/// </summary>
	internal static LinkedInCardUpdate ReplaceLinkedInNearProfile(string postContent, string profileUrl, string linkedInUrl, out string updated)
	{
		return ReplaceCardFieldsNearProfile(
			postContent,
			profileUrl,
			new ConseillerCardChanges { UpdateLinkedIn = true, LinkedIn = linkedInUrl },
			out updated);
	}

	/// <summary>
	/// Sur une page qui cite plusieurs conseillers (Nos conseillers, pages secteurs),
	/// applique en une passe les changements demandés (prénom/nom, e-mail, LinkedIn, Google)
	/// uniquement sur les cartes qui pointent vers la page du conseiller.
	/// Le texte court de la carte n'est jamais touché.
	/// </summary>
	internal static LinkedInCardUpdate ReplaceCardFieldsNearProfile(string postContent, string profileUrl, ConseillerCardChanges changes, out string updated)
	{
		updated = null;
		if (string.IsNullOrEmpty(postContent) || changes == null || !changes.Any || !TryPerleRarePage(profileUrl, out string language, out string slug))
		{
			return LinkedInCardUpdate.Absent;
		}
		bool wantsName = changes.UpdateName;
		if (wantsName && (string.IsNullOrWhiteSpace(changes.FullName) || changes.FullName.Length > 150 || changes.FullName.IndexOfAny(new char[] { '<', '>', '\r', '\n' }) >= 0))
		{
			return LinkedInCardUpdate.Unsafe;
		}
		bool wantsEmail = false;
		string email = null;
		if (changes.UpdateEmail)
		{
			wantsEmail = TryEmail(changes.Email, out email);
			if (!string.IsNullOrWhiteSpace(changes.Email) && !wantsEmail)
			{
				return LinkedInCardUpdate.Unsafe;
			}
		}
		bool wantsLinkedIn = false;
		string linkedInHref = null;
		if (changes.UpdateLinkedIn)
		{
			wantsLinkedIn = TryLinkedInHref(changes.LinkedIn, out linkedInHref);
			if (!string.IsNullOrWhiteSpace(changes.LinkedIn) && !wantsLinkedIn)
			{
				return LinkedInCardUpdate.Unsafe;
			}
		}
		bool wantsGoogle = false;
		string googleHref = null;
		if (changes.UpdateGoogle)
		{
			wantsGoogle = TryGoogleHref(changes.Google, out googleHref);
			if (!string.IsNullOrWhiteSpace(changes.Google) && !wantsGoogle)
			{
				return LinkedInCardUpdate.Unsafe;
			}
		}

		string marker = "/" + language + "/" + slug;
		List<(int Start, int End, string Next)> windows = new List<(int, int, string)>();
		List<(int Start, int End)> cardRanges = FindCardRangesForSlug(postContent, marker);
		if (cardRanges == null)
		{
			return LinkedInCardUpdate.Unsafe;
		}
		if (cardRanges.Count == 0)
		{
			return LinkedInCardUpdate.Absent;
		}

		foreach ((int start, int end) in cardRanges)
		{
			string window = postContent.Substring(start, end - start);
			if (!TryUpdateCardWindow(window, changes, wantsEmail, email, wantsLinkedIn, linkedInHref, wantsGoogle, googleHref, out string nextWindow))
			{
				return LinkedInCardUpdate.Unsafe;
			}
			windows.Add((start, end, nextWindow));
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
		if (string.IsNullOrEmpty(postContent) || !TryProfileRegions(postContent, out ProfileRegions regions))
		{
			return false;
		}
		string oldRegion = postContent.Substring(regions.BodyStart, regions.ContactStart - regions.BodyStart);
		Match existingRsac = RsacBlock.Match(oldRegion);
		string preservedRsac = existingRsac.Success ? existingRsac.Value.Trim() : null;
		string oldBioRegion = preservedRsac != null ? StripRsac(oldRegion) : oldRegion;

		List<string> oldParagraphs = ExtractExistingParagraphs(oldBioRegion);
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
		if (!string.IsNullOrEmpty(preservedRsac))
		{
			built.Add(preservedRsac);
		}
		string region = built.Count == 0 ? "\n" : "\n" + string.Join("\n", built) + "\n";
		updated = postContent.Substring(0, regions.BodyStart) + region + postContent.Substring(regions.ContactStart);
		return true;
	}

	private readonly struct ProfileRegions
	{
		public int BodyStart { get; init; }

		public int ContactStart { get; init; }

		public int ContactInnerStart { get; init; }

		public int ContactInnerLength { get; init; }
	}

	/// <summary>
	/// Découpe une page profil (nom-chasseur-immobilier-noir) en 3 zones :
	/// [0..BodyStart] nom+fonction, [BodyStart..ContactStart] parcours+RSAC,
	/// [ContactInnerStart..+ContactInnerLength] liens de contact (avant [/vc_column_text]).
	/// Supporte les pages en &lt;p&gt; explicites comme les pages wpautop (lignes séparées par ligne vide).
	/// </summary>
	private static bool TryProfileRegions(string postContent, out ProfileRegions regions)
	{
		regions = default;
		Match name = NameParagraph.Match(postContent);
		if (!name.Success)
		{
			return false;
		}
		int bodyStart = name.Index + name.Length;
		int vcEnd = postContent.IndexOf("[/vc_column_text]", bodyStart, StringComparison.OrdinalIgnoreCase);

		// 1. Format historique : <p>...</p> contenant mailto/mailo/@ ou [/vc_column_text]
		Match closing = HtmlParagraph.Match(postContent, bodyStart);
		while (closing.Success
			&& !HasContactMarker(closing.Value)
			&& closing.Value.IndexOf("[/vc_column_text]", StringComparison.OrdinalIgnoreCase) < 0)
		{
			closing = closing.NextMatch();
		}
		if (closing.Success)
		{
			Match openP = OpenParagraphTag.Match(closing.Value);
			int innerOffset = openP.Success ? openP.Length : 0;
			int innerStart = closing.Index + innerOffset;
			int vcInside = closing.Value.IndexOf("[/vc_column_text]", StringComparison.OrdinalIgnoreCase);
			int innerEnd = vcInside >= 0 ? closing.Index + vcInside : closing.Index + closing.Length - 4;
			if (innerEnd < innerStart)
			{
				return false;
			}
			regions = new ProfileRegions
			{
				BodyStart = bodyStart,
				ContactStart = closing.Index,
				ContactInnerStart = innerStart,
				ContactInnerLength = innerEnd - innerStart
			};
			return true;
		}

		// 2. Format wpautop (sans <p> autour du bloc contact, terminé par [/vc_column_text])
		if (vcEnd < 0)
		{
			return false;
		}
		string mid = postContent.Substring(bodyStart, vcEnd - bodyStart);
		int contactRelStart = FindRawContactStart(mid);
		if (contactRelStart < 0)
		{
			return false;
		}
		int contactStart = bodyStart + contactRelStart;
		regions = new ProfileRegions
		{
			BodyStart = bodyStart,
			ContactStart = contactStart,
			ContactInnerStart = contactStart,
			ContactInnerLength = vcEnd - contactStart
		};
		return true;
	}

	private static int FindRawContactStart(string mid)
	{
		// Cherche le dernier paragraphe avant [/vc_column_text] : après la dernière ligne vide ou le dernier </p>.
		int trimEnd = mid.Length;
		while (trimEnd > 0 && (mid[trimEnd - 1] == ' ' || mid[trimEnd - 1] == '\t'))
		{
			trimEnd--;
		}
		// Trouve la frontière de paragraphe juste avant la dernière ligne non vide.
		int scan = trimEnd;
		while (scan > 0 && (mid[scan - 1] == '\r' || mid[scan - 1] == '\n'))
		{
			scan--;
		}
		int lastLineBreak = mid.LastIndexOf('\n', Math.Max(0, scan - 1));
		int candidateStart = lastLineBreak >= 0 ? lastLineBreak + 1 : 0;
		string tail = mid.Substring(candidateStart);
		if (HasContactMarker(tail))
		{
			return candidateStart;
		}
		// Si tous les liens de contact avaient été effacés, accepte un bloc vide après une ligne vide.
		if (string.IsNullOrWhiteSpace(tail) && candidateStart > 0)
		{
			return mid.Length;
		}
		return -1;
	}

	private static bool HasContactMarker(string html)
	{
		return MailAnchor.IsMatch(html)
			|| LinkedInAnchor.IsMatch(html)
			|| GoogleAnchor.IsMatch(html)
			|| OrphanLinkedInImg.IsMatch(html);
	}

	private static List<string> ExtractExistingParagraphs(string oldRegion)
	{
		List<string> paragraphs = new List<string>();
		MatchCollection htmlMatches = HtmlParagraph.Matches(oldRegion);
		if (htmlMatches.Count > 0)
		{
			foreach (Match match in htmlMatches)
			{
				paragraphs.Add(match.Value);
			}
			return paragraphs;
		}
		// Format wpautop sans <p> : découpe par lignes vides et emballe dans <p> pour préserver les liens internes.
		string normalized = oldRegion.Replace("\r\n", "\n").Replace('\r', '\n');
		foreach (string chunk in Regex.Split(normalized, "\\n\\s*\\n"))
		{
			string trimmed = chunk.Trim();
			if (trimmed.Length > 0)
			{
				paragraphs.Add("<p>" + trimmed + "</p>");
			}
		}
		return paragraphs;
	}

	private static string StripRsac(string bodyRegion)
	{
		string stripped = RsacBlock.Replace(bodyRegion, "");
		stripped = Regex.Replace(stripped, "(?:\\r?\\n){3,}", "\n\n");
		return stripped;
	}

	private static string FormatRsacParagraph(string language, string numRsac)
	{
		string encoded = WebUtility.HtmlEncode(numRsac);
		if (string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
		{
			return "<p class=\"rsac-conseiller\">Commercial agent registered with the RSAC under number " + encoded + "</p>";
		}
		return "<p class=\"rsac-conseiller\">Agent commercial inscrit au RSAC sous le numéro " + encoded + "</p>";
	}

	private static bool TryApplyEmail(string contact, bool wantsEmail, string email, string envelopeIcon, out string updated)
	{
		updated = contact;
		MatchCollection matches = MailAnchor.Matches(contact);
		if (matches.Count > 1)
		{
			updated = null;
			return false;
		}
		if (matches.Count == 1)
		{
			Match match = matches[0];
			if (!wantsEmail)
			{
				string before = contact.Substring(0, match.Index);
				string after = contact.Substring(match.Index + match.Length);
				if (before.EndsWith(" ", StringComparison.Ordinal) && after.StartsWith(" ", StringComparison.Ordinal))
				{
					after = after.Substring(1);
				}
				updated = (before + after).TrimStart(' ');
				return true;
			}
			string encodedEmail = WebUtility.HtmlEncode(email);
			string inner = match.Groups[1].Value;
			string nextAnchor;
			if (inner.IndexOf("<img", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				nextAnchor = AnchorHrefAttr.Replace(match.Value, "$1mailto:" + encodedEmail + "$2", 1);
			}
			else
			{
				int openEnd = match.Value.IndexOf('>');
				string openTag = AnchorHrefAttr.Replace(match.Value.Substring(0, openEnd + 1), "$1mailto:" + encodedEmail + "$2", 1);
				nextAnchor = openTag + encodedEmail + "</a>";
			}
			updated = contact.Substring(0, match.Index) + nextAnchor + contact.Substring(match.Index + match.Length);
			return true;
		}
		if (!wantsEmail)
		{
			return true;
		}
		string inserted = "<a href=\"mailto:" + WebUtility.HtmlEncode(email) + "\">" + envelopeIcon + "</a>";
		if (string.IsNullOrWhiteSpace(contact))
		{
			updated = inserted;
			return true;
		}
		Match firstLink = Regex.Match(contact, "<a\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (firstLink.Success)
		{
			updated = contact.Insert(firstLink.Index, inserted + " ");
			return true;
		}
		updated = inserted + " " + contact.TrimStart();
		return true;
	}

	private static bool TryApplyLinkedIn(string contact, bool wantsLink, string href, string icon, out string updated)
	{
		updated = contact;
		MatchCollection matches = LinkedInAnchor.Matches(contact);
		if (matches.Count > 1)
		{
			updated = null;
			return false;
		}
		if (matches.Count == 1)
		{
			updated = wantsLink
				? LinkedInHref.Replace(contact, "$1" + WebUtility.HtmlEncode(href) + "$2", 1)
				: LinkedInAnchor.Replace(contact, "", 1).TrimStart(' ');
			return updated != null;
		}
		// Nettoie une éventuelle icône LinkedIn orpheline sans <a> (ex. fiche historique incomplète).
		string cleaned = OrphanLinkedInImg.Replace(contact, "");
		if (!wantsLink)
		{
			updated = cleaned;
			return true;
		}
		string anchor = "<a href=\"" + WebUtility.HtmlEncode(href) + "\">" + icon + "</a>";
		Match mail = MailAnchor.Match(cleaned);
		if (mail.Success)
		{
			updated = cleaned.Insert(mail.Index + mail.Length, " " + anchor);
			return true;
		}
		Match google = GoogleAnchor.Match(cleaned);
		if (google.Success)
		{
			updated = cleaned.Insert(google.Index, anchor + " ");
			return true;
		}
		if (string.IsNullOrWhiteSpace(cleaned))
		{
			updated = anchor;
			return true;
		}
		return false;
	}

	private static bool TryApplyGoogle(string contact, bool wantsLink, string href, string icon, out string updated)
	{
		updated = contact;
		MatchCollection matches = GoogleAnchor.Matches(contact);
		if (matches.Count > 1)
		{
			updated = null;
			return false;
		}
		if (matches.Count == 1)
		{
			updated = wantsLink
				? GoogleHref.Replace(contact, "$1" + WebUtility.HtmlEncode(href) + "$2", 1)
				: GoogleAnchor.Replace(contact, "", 1).TrimStart(' ');
			return updated != null;
		}
		if (!wantsLink)
		{
			return true;
		}
		string anchor = "<a href=\"" + WebUtility.HtmlEncode(href) + "\" target=\"_blank\" rel=\"noopener\">" + icon + "</a>";
		Match linkedIn = LinkedInAnchor.Match(contact);
		if (linkedIn.Success)
		{
			updated = contact.Insert(linkedIn.Index + linkedIn.Length, " " + anchor);
			return true;
		}
		Match mail = MailAnchor.Match(contact);
		if (mail.Success)
		{
			updated = contact.Insert(mail.Index + mail.Length, " " + anchor);
			return true;
		}
		if (string.IsNullOrWhiteSpace(contact))
		{
			updated = anchor;
			return true;
		}
		return false;
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

	private static List<(int Start, int End)> FindCardRangesForSlug(string postContent, string marker)
	{
		List<(int Start, int End)> ranges = new List<(int, int)>();
		MatchCollection vcBlocks = VcColumnTextBlock.Matches(postContent);
		if (vcBlocks.Count > 0)
		{
			foreach (Match block in vcBlocks)
			{
				if (IndexOfBoundedSlug(block.Value, marker, 0) < 0)
				{
					continue;
				}
				if (block.Value.IndexOf("nom-chasseur-immobilier\"", StringComparison.Ordinal) < 0)
				{
					continue;
				}
				if (!IsSingleAdvisorCard(block.Value))
				{
					return null;
				}
				ranges.Add((block.Index, block.Index + block.Length));
			}
			return ranges;
		}

		// Repli pour fragments sans [vc_column_text] : découpe entre deux <p class="nom-chasseur-immobilier">
		int search = 0;
		while (search < postContent.Length)
		{
			int at = IndexOfBoundedSlug(postContent, marker, search);
			if (at < 0)
			{
				break;
			}
			int start = PreviousCardBoundary(postContent, at);
			int end = NextCardBoundary(postContent, at + marker.Length);
			if (ranges.Count == 0 || ranges[ranges.Count - 1].Start != start)
			{
				string slice = postContent.Substring(start, end - start);
				if (!IsSingleAdvisorCard(slice))
				{
					return null;
				}
				ranges.Add((start, end));
			}
			search = end;
		}
		return ranges;
	}

	private static bool IsSingleAdvisorCard(string cardHtml)
	{
		if (CardNameParagraph.Matches(cardHtml).Count > 1)
		{
			return false;
		}
		HashSet<string> slugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Match m in PerleRareProfileHref.Matches(cardHtml))
		{
			slugs.Add(m.Groups[1].Value + "/" + m.Groups[2].Value);
		}
		return slugs.Count <= 1;
	}

	private static bool TryUpdateCardWindow(
		string window,
		ConseillerCardChanges changes,
		bool wantsEmail,
		string email,
		bool wantsLinkedIn,
		string linkedInHref,
		bool wantsGoogle,
		string googleHref,
		out string updated)
	{
		updated = window;
		if (changes.UpdateName)
		{
			Match namePara = CardNameParagraph.Match(updated);
			if (namePara.Success)
			{
				Match strong = NameStrongTag.Match(namePara.Value);
				if (!strong.Success)
				{
					updated = null;
					return false;
				}
				string existingText = WebUtility.HtmlDecode(strong.Groups[2].Value).Trim();
				string suffix = existingText.EndsWith(",", StringComparison.Ordinal) ? "," : "";
				string encoded = WebUtility.HtmlEncode(changes.FullName.Trim()) + suffix;
				string replacedPara = namePara.Value.Substring(0, strong.Index)
					+ strong.Groups[1].Value + encoded + strong.Groups[3].Value
					+ namePara.Value.Substring(strong.Index + strong.Length);
				updated = updated.Substring(0, namePara.Index) + replacedPara + updated.Substring(namePara.Index + namePara.Length);
			}
		}

		// Les liens de contact sur une carte se trouvent après le lien vers la fiche profil.
		int contactAnchorOffset = FindCardContactSectionOffset(updated);
		string head = updated.Substring(0, contactAnchorOffset);
		string tail = updated.Substring(contactAnchorOffset);

		// Sépare un éventuel [/vc_column_text] de fermeture pour insérer proprement avant lui.
		int closingVc = tail.IndexOf("[/vc_column_text]", StringComparison.OrdinalIgnoreCase);
		string tailCore = closingVc >= 0 ? tail.Substring(0, closingVc) : tail;
		string tailSuffix = closingVc >= 0 ? tail.Substring(closingVc) : "";

		if (changes.UpdateEmail)
		{
			if (!TryApplyEmail(tailCore, wantsEmail, email, CardEnvelopeIcon, out tailCore))
			{
				updated = null;
				return false;
			}
		}
		if (changes.UpdateLinkedIn)
		{
			if (LinkedInAnchor.Matches(tailCore).Count > 1)
			{
				updated = null;
				return false;
			}
			if (LinkedInAnchor.IsMatch(tailCore) || MailAnchor.IsMatch(tailCore) || GoogleAnchor.IsMatch(tailCore))
			{
				if (!TryApplyLinkedIn(tailCore, wantsLinkedIn, linkedInHref, CardLinkedInIcon, out tailCore))
				{
					updated = null;
					return false;
				}
			}
		}
		if (changes.UpdateGoogle)
		{
			if (GoogleAnchor.Matches(tailCore).Count > 1)
			{
				updated = null;
				return false;
			}
			if (GoogleAnchor.IsMatch(tailCore) || LinkedInAnchor.IsMatch(tailCore) || MailAnchor.IsMatch(tailCore))
			{
				if (!TryApplyGoogle(tailCore, wantsGoogle, googleHref, CardGoogleIcon, out tailCore))
				{
					updated = null;
					return false;
				}
			}
		}

		updated = head + tailCore + tailSuffix;
		return true;
	}

	private static int FindCardContactSectionOffset(string cardHtml)
	{
		Match profileLink = PerleRareProfileHref.Match(cardHtml);
		if (profileLink.Success)
		{
			int closeAnchor = cardHtml.IndexOf("</a>", profileLink.Index + profileLink.Length, StringComparison.OrdinalIgnoreCase);
			if (closeAnchor >= 0)
			{
				return closeAnchor + 4;
			}
		}
		return 0;
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

	private static int PreviousCardBoundary(string content, int beforeIndex)
	{
		int at = content.LastIndexOf("<p class=\"nom-chasseur-immobilier\"", beforeIndex, StringComparison.Ordinal);
		if (at < 0 || beforeIndex - at > 4000)
		{
			return beforeIndex;
		}
		return at;
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
