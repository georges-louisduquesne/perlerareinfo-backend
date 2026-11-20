using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ApiPerleRare.Helpers;

public class PublicProfile
{
	public string Name { get; set; }

	public string Headline { get; set; }

	public string About { get; set; }

	public string Url { get; set; }

	public string Photo { get; set; }

	public string Location { get; set; }

	public string Company { get; set; }

	public string School { get; set; }
}

public class PublicWebHit
{
	public string Title { get; set; }

	public string Url { get; set; }

	public string Description { get; set; }

	public string Site { get; set; }

	public string Date { get; set; }
}

public class PublicPhoto
{
	public string ThumbnailUrl { get; set; }

	public string ImageUrl { get; set; }

	public string Title { get; set; }

	public string Source { get; set; }

	public string Domain { get; set; }

	public string Link { get; set; }

	public int Width { get; set; }

	public int Height { get; set; }

	public bool IsLinkedInPortrait { get; set; }
}

public class AiSummaryPoint
{
	public string Label { get; set; }

	public string Text { get; set; }
}

public class ParsedAiSummary
{
	public string Intro { get; set; }

	public List<AiSummaryPoint> Points { get; set; } = new List<AiSummaryPoint>();

	public string Text { get; set; }
}

/// <summary>
/// Lit les réponses Apify (Google Search, AI Overview, Google Images, LinkedIn)
/// et filtre les résultats pour ne retenir que la personne exacte (prénom + nom).
/// </summary>
public static class PersonLookupReader
{
	private static readonly HashSet<string> SkipTokens = new HashSet<string>(StringComparer.Ordinal)
	{
		"de", "des", "du", "la", "le", "les", "d", "au", "aux",
		"mr", "mme", "mlle", "dr", "docteur", "pr", "professeur", "me", "maitre",
	};

	private static readonly string[] FollowUpPrefixes =
	{
		"souhaitez-vous",
		"voulez-vous",
		"precisez ",
		"n'hesitez pas",
		"n hesiter pas",
		"n hesitez pas",
		"si vous souhaitez",
		"avez-vous besoin",
		"dites-moi",
		"pourriez-vous preciser",
		"would you like",
		"do you want",
		"let me know",
		"please specify",
		"feel free to ask",
	};

	private static readonly string[] TrailingLinksSectionHeadings =
	{
		"profils et liens publics",
		"liens publics",
		"profils publics",
		"public profiles",
		"public links",
		"sources principales",
	};

	public static object Unavailable()
	{
		return new
		{
			available = false,
			aiSummary = (string)null,
			aiSummaryIntro = (string)null,
			aiSummaryPoints = Array.Empty<AiSummaryPoint>(),
			linkedin = Array.Empty<PublicProfile>(),
			web = Array.Empty<PublicWebHit>(),
			photos = Array.Empty<PublicPhoto>(),
			cached = false,
		};
	}

	public static bool IsExactPersonName(string prenom, string nom, string candidate)
	{
		var target = NameTokens((prenom ?? "") + " " + (nom ?? ""));
		var found = NameTokens(candidate);
		if (target.Count < 2 || found.Count != target.Count)
		{
			return false;
		}
		var left = target.OrderBy(token => token, StringComparer.Ordinal).ToArray();
		var right = found.OrderBy(token => token, StringComparer.Ordinal).ToArray();
		for (var i = 0; i < left.Length; i++)
		{
			if (left[i] != right[i])
			{
				return false;
			}
		}
		return true;
	}

	public static List<string> NameTokens(string value)
	{
		var tokens = new List<string>();
		foreach (var token in Fold(value).Split(' '))
		{
			if (token.Length == 0 || SkipTokens.Contains(token) || IsAllDigits(token))
			{
				continue;
			}
			tokens.Add(token);
		}
		return tokens;
	}

	public static List<PublicProfile> ReadLinkedIn(string json)
	{
		var people = new List<PublicProfile>();
		if (string.IsNullOrWhiteSpace(json))
		{
			return people;
		}
		using var doc = JsonDocument.Parse(json);
		if (doc.RootElement.ValueKind != JsonValueKind.Array)
		{
			return people;
		}
		foreach (var item in doc.RootElement.EnumerateArray())
		{
			if (item.ValueKind != JsonValueKind.Object)
			{
				continue;
			}
			var profile = new PublicProfile
			{
				Name = Text(item, "name"),
				Headline = Text(item, "headline"),
				About = Text(item, "about"),
				Url = Text(item, "linkedinUrl"),
				Photo = Text(item, "photo"),
				Location = Text(item, "location"),
				Company = FirstText(item, "company", "currentCompany"),
				School = Text(item, "school"),
			};
			if (string.IsNullOrWhiteSpace(profile.Name) && string.IsNullOrWhiteSpace(profile.Url))
			{
				continue;
			}
			people.Add(profile);
		}
		return people;
	}

	public static List<PublicProfile> Merge(List<PublicProfile> light, List<PublicProfile> full)
	{
		if (full == null || full.Count == 0)
		{
			return light ?? new List<PublicProfile>();
		}
		var richer = new Dictionary<string, PublicProfile>(StringComparer.OrdinalIgnoreCase);
		foreach (var item in full)
		{
			if (!string.IsNullOrWhiteSpace(item.Url))
			{
				richer[item.Url] = item;
			}
		}
		var merged = new List<PublicProfile>();
		var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var item in light ?? new List<PublicProfile>())
		{
			if (!string.IsNullOrWhiteSpace(item.Url) && richer.TryGetValue(item.Url, out var detailed))
			{
				merged.Add(Prefer(item, detailed));
				used.Add(item.Url);
			}
			else
			{
				merged.Add(item);
			}
		}
		foreach (var item in full)
		{
			if (string.IsNullOrWhiteSpace(item.Url) || !used.Contains(item.Url))
			{
				merged.Add(item);
			}
		}
		return merged;
	}

	public static List<PublicProfile> ReadLinkedInFromGoogle(string json, string prenom, string nom)
	{
		var bySlug = new Dictionary<string, PublicProfile>(StringComparer.OrdinalIgnoreCase);
		var order = new List<string>();
		if (string.IsNullOrWhiteSpace(json))
		{
			return new List<PublicProfile>();
		}
		using var doc = JsonDocument.Parse(json);
		if (doc.RootElement.ValueKind != JsonValueKind.Array)
		{
			return new List<PublicProfile>();
		}
		foreach (var page in doc.RootElement.EnumerateArray())
		{
			if (page.ValueKind != JsonValueKind.Object || !page.TryGetProperty("organicResults", out var organic) || organic.ValueKind != JsonValueKind.Array)
			{
				continue;
			}
			foreach (var item in organic.EnumerateArray())
			{
				if (item.ValueKind != JsonValueKind.Object)
				{
					continue;
				}
				var url = Text(item, "url");
				if (!TryLinkedInProfileSlug(url, out var slug))
				{
					continue;
				}
				var title = StripLinkedInSuffix(Text(item, "title"));
				var websiteTitle = Text(item, "websiteTitle");
				var description = Cut(Text(item, "description"), 400);
				var (name, headline, company) = ParseLinkedInTitle(title, websiteTitle);
				if (string.IsNullOrWhiteSpace(name))
				{
					name = NameFromSlug(slug);
				}
				if (!IsExactPersonName(prenom, nom, name))
				{
					continue;
				}
				var location = "";
				if (item.TryGetProperty("personalInfo", out var info) && info.ValueKind == JsonValueKind.Object)
				{
					location = Text(info, "location");
					var infoCompany = Text(info, "companyName");
					var infoJob = Text(info, "jobTitle");
					if (!string.IsNullOrWhiteSpace(infoCompany))
					{
						company = infoCompany;
					}
					if (string.IsNullOrWhiteSpace(headline) && !string.IsNullOrWhiteSpace(infoJob))
					{
						headline = string.IsNullOrWhiteSpace(company) ? infoJob : $"{infoJob} chez {company}";
					}
				}
				var candidate = new PublicProfile
				{
					Name = name,
					Headline = headline,
					About = description,
					Url = CleanLinkedInProfileUrl(url),
					Photo = "",
					Location = location,
					Company = company,
					School = "",
				};
				if (bySlug.TryGetValue(slug, out var existing))
				{
					bySlug[slug] = MergeLinkedInProfile(existing, candidate);
				}
				else
				{
					bySlug[slug] = candidate;
					order.Add(slug);
				}
			}
		}
		return order.Select(slug => bySlug[slug]).ToList();
	}

	public static List<PublicWebHit> ReadGoogle(string json)
	{
		var hits = new List<PublicWebHit>();
		if (string.IsNullOrWhiteSpace(json))
		{
			return hits;
		}
		using var doc = JsonDocument.Parse(json);
		if (doc.RootElement.ValueKind != JsonValueKind.Array)
		{
			return hits;
		}
		foreach (var page in doc.RootElement.EnumerateArray())
		{
			if (page.ValueKind != JsonValueKind.Object || !page.TryGetProperty("organicResults", out var organic) || organic.ValueKind != JsonValueKind.Array)
			{
				continue;
			}
			foreach (var item in organic.EnumerateArray())
			{
				if (item.ValueKind != JsonValueKind.Object)
				{
					continue;
				}
				var url = Text(item, "url");
				if (string.IsNullOrWhiteSpace(url))
				{
					continue;
				}
				hits.Add(new PublicWebHit
				{
					Title = Text(item, "title"),
					Url = url,
					Description = Cut(Text(item, "description"), 400),
					Site = Text(item, "websiteTitle"),
					Date = Text(item, "date"),
				});
			}
		}
		return hits;
	}

	public static List<PublicWebHit> ReadGoogleForPerson(string json, string prenom, string nom)
	{
		var raw = ReadGoogle(json);
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var filtered = new List<PublicWebHit>();
		foreach (var hit in raw)
		{
			if (string.IsNullOrWhiteSpace(hit.Url) || TryLinkedInProfileSlug(hit.Url, out _))
			{
				continue;
			}
			if (IsLinkedInHost(hit.Url))
			{
				continue;
			}
			var key = CanonicalUrl(hit.Url);
			if (string.IsNullOrEmpty(key) || !seen.Add(key))
			{
				continue;
			}
			if (!MentionsPerson(prenom, nom, hit.Title, hit.Url, hit.Description))
			{
				continue;
			}
			filtered.Add(hit);
			if (filtered.Count >= 10)
			{
				break;
			}
		}
		return filtered;
	}

	public static string ReadAiSummary(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}
		using var doc = JsonDocument.Parse(json);
		if (doc.RootElement.ValueKind != JsonValueKind.Array)
		{
			return null;
		}
		foreach (var page in doc.RootElement.EnumerateArray())
		{
			var text = AiText(page);
			if (!string.IsNullOrWhiteSpace(text))
			{
				return Cut(text.Trim(), 2500);
			}
		}
		return null;
	}

	public static ParsedAiSummary ParseAiSummary(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return null;
		}
		var normalized = raw.Replace("\r\n", "\n").Replace('\r', '\n');
		var lines = normalized.Split('\n');
		var introParts = new List<string>();
		var points = new List<AiSummaryPoint>();
		var hasContent = false;

		foreach (var rawLine in lines)
		{
			var line = StripMarkdown(rawLine).Trim();
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}
			var folded = Fold(line);
			if (IsFollowUpPrompt(folded))
			{
				break;
			}
			if (hasContent && IsTrailingLinksHeading(folded))
			{
				break;
			}
			var bulletMatch = Regex.Match(line, "^(?:[-•*]|\\d+[.)])\\s+(.+)$");
			if (bulletMatch.Success)
			{
				var body = bulletMatch.Groups[1].Value.Trim();
				if (IsFollowUpPrompt(Fold(body)))
				{
					break;
				}
				var colonIdx = body.IndexOf(':');
				if (colonIdx > 1 && colonIdx <= 48)
				{
					var label = body.Substring(0, colonIdx).Trim();
					var text = body.Substring(colonIdx + 1).Trim();
					if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(text))
					{
						points.Add(new AiSummaryPoint { Label = label, Text = text });
						hasContent = true;
						continue;
					}
				}
				points.Add(new AiSummaryPoint { Label = "", Text = body });
				hasContent = true;
			}
			else
			{
				introParts.Add(line);
				hasContent = true;
			}
		}

		if (!hasContent)
		{
			return null;
		}
		var intro = string.Join(" ", introParts).Trim();
		var textLines = new List<string>();
		if (!string.IsNullOrWhiteSpace(intro))
		{
			textLines.Add(intro);
		}
		foreach (var point in points)
		{
			textLines.Add(string.IsNullOrWhiteSpace(point.Label)
				? "- " + point.Text
				: $"- {point.Label} : {point.Text}");
		}
		return new ParsedAiSummary
		{
			Intro = intro,
			Points = points,
			Text = string.Join("\n", textLines),
		};
	}

	public static List<PublicPhoto> ReadGoogleImages(string json, string prenom, string nom, IEnumerable<string> knownUrls = null)
	{
		var results = new List<(int Rank, int Index, PublicPhoto Photo)>();
		if (string.IsNullOrWhiteSpace(json))
		{
			return new List<PublicPhoto>();
		}
		using var doc = JsonDocument.Parse(json);
		if (doc.RootElement.ValueKind != JsonValueKind.Array)
		{
			return new List<PublicPhoto>();
		}
		var knownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (knownUrls != null)
		{
			foreach (var u in knownUrls)
			{
				var key = CanonicalUrl(u);
				if (!string.IsNullOrEmpty(key))
				{
					knownKeys.Add(key);
				}
			}
		}
		var seenMedia = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var seenLinkedInPortrait = false;
		var index = 0;

		foreach (var item in doc.RootElement.EnumerateArray())
		{
			index++;
			if (item.ValueKind != JsonValueKind.Object)
			{
				continue;
			}
			var thumb = Text(item, "thumbnailUrl");
			var image = Text(item, "imageUrl");
			var effectiveThumb = !string.IsNullOrWhiteSpace(thumb) ? thumb : image;
			if (string.IsNullOrWhiteSpace(effectiveThumb))
			{
				continue;
			}
			var width = IntVal(item, "imageWidth", "thumbnailWidth");
			var height = IntVal(item, "imageHeight", "thumbnailHeight");
			if ((width > 0 && width < 60) || (height > 0 && height < 60))
			{
				continue;
			}
			var title = Text(item, "title");
			var source = Text(item, "source");
			var domain = Text(item, "domain");
			var link = FirstText(item, "link", "contentUrl");
			if (string.IsNullOrWhiteSpace(domain) && !string.IsNullOrWhiteSpace(link))
			{
				domain = HostOf(link);
			}
			var mentions = MentionsPerson(prenom, nom, title, link, image);
			var matchesKnown = !string.IsNullOrEmpty(link) && knownKeys.Contains(CanonicalUrl(link));
			if (!mentions && !matchesKnown)
			{
				continue;
			}
			var isLinkedIn = domain.Contains("linkedin.", StringComparison.OrdinalIgnoreCase) || IsLinkedInHost(link);
			var isPortrait = isLinkedIn && (
				image.Contains("profile-displayphoto", StringComparison.OrdinalIgnoreCase)
				|| TryLinkedInProfileSlug(link, out _)
			);
			var mediaKey = isPortrait ? "li-portrait:" + CanonicalUrl(link) : CanonicalMediaKey(image, effectiveThumb);
			if (!seenMedia.Add(mediaKey))
			{
				continue;
			}
			if (isPortrait && seenLinkedInPortrait && string.IsNullOrEmpty(link))
			{
				continue;
			}
			if (isPortrait)
			{
				seenLinkedInPortrait = true;
			}

			var rank = isPortrait ? 0 : (mentions ? 1 : 2);
			results.Add((rank, index, new PublicPhoto
			{
				ThumbnailUrl = effectiveThumb,
				ImageUrl = !string.IsNullOrWhiteSpace(image) ? image : effectiveThumb,
				Title = title,
				Source = string.IsNullOrWhiteSpace(source) ? domain : source,
				Domain = domain,
				Link = link,
				Width = width,
				Height = height,
				IsLinkedInPortrait = isPortrait,
			}));
		}

		return results
			.OrderBy(entry => entry.Rank)
			.ThenBy(entry => entry.Index)
			.Take(8)
			.Select(entry => entry.Photo)
			.ToList();
	}

	public static void AttachLinkedInPhotos(IEnumerable<PublicProfile> profiles, IEnumerable<PublicPhoto> photos)
	{
		if (profiles == null || photos == null)
		{
			return;
		}
		var profileList = profiles.ToList();
		var photoList = photos.ToList();
		if (profileList.Count == 0 || photoList.Count == 0)
		{
			return;
		}
		foreach (var profile in profileList)
		{
			if (!string.IsNullOrWhiteSpace(profile.Photo))
			{
				continue;
			}
			if (TryLinkedInProfileSlug(profile.Url, out var slug))
			{
				var matched = photoList.FirstOrDefault(photo =>
					photo.IsLinkedInPortrait && TryLinkedInProfileSlug(photo.Link, out var photoSlug) && string.Equals(slug, photoSlug, StringComparison.OrdinalIgnoreCase))
					?? photoList.FirstOrDefault(photo =>
						TryLinkedInProfileSlug(photo.Link, out var photoSlug) && string.Equals(slug, photoSlug, StringComparison.OrdinalIgnoreCase));
				if (matched != null)
				{
					profile.Photo = Pick(matched.ThumbnailUrl, matched.ImageUrl);
					continue;
				}
			}
		}
		if (profileList.Count == 1 && string.IsNullOrWhiteSpace(profileList[0].Photo))
		{
			var singlePortrait = photoList.FirstOrDefault(photo => photo.IsLinkedInPortrait);
			if (singlePortrait != null)
			{
				profileList[0].Photo = Pick(singlePortrait.ThumbnailUrl, singlePortrait.ImageUrl);
			}
		}
	}

	public static List<PublicProfile> ProfilesFromLinkedInPhotos(IEnumerable<PublicPhoto> photos, string prenom, string nom)
	{
		var found = new List<PublicProfile>();
		if (photos == null)
		{
			return found;
		}
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var photo in photos)
		{
			if (!TryLinkedInProfileSlug(photo.Link, out var slug) || !seen.Add(slug))
			{
				continue;
			}
			var title = StripLinkedInSuffix(photo.Title);
			var (name, headline, company) = ParseLinkedInTitle(title, "LinkedIn");
			if (string.IsNullOrWhiteSpace(name))
			{
				name = NameFromSlug(slug);
			}
			if (!IsExactPersonName(prenom, nom, name))
			{
				continue;
			}
			found.Add(new PublicProfile
			{
				Name = name,
				Headline = headline,
				About = "",
				Url = CleanLinkedInProfileUrl(photo.Link),
				Photo = Pick(photo.ThumbnailUrl, photo.ImageUrl),
				Location = "",
				Company = company,
				School = "",
			});
		}
		return found;
	}

	internal static bool MentionsPerson(string prenom, string nom, params string[] fields)
	{
		var target = NameTokens((prenom ?? "") + " " + (nom ?? ""));
		if (target.Count < 2)
		{
			return false;
		}
		var haystack = Fold(string.Join(" ", fields.Where(f => !string.IsNullOrWhiteSpace(f))));
		if (string.IsNullOrEmpty(haystack))
		{
			return false;
		}
		var words = new HashSet<string>(haystack.Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
		if (target.All(token => words.Contains(token)))
		{
			return true;
		}
		// URLs/handles like "anastasiaplasmans" without space separator
		var compact = haystack.Replace(" ", "");
		var compactTarget = string.Concat(target);
		return compactTarget.Length >= 6 && compact.Contains(compactTarget, StringComparison.Ordinal);
	}

	internal static bool TryLinkedInProfileSlug(string url, out string slug)
	{
		slug = null;
		if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
		{
			return false;
		}
		if (!uri.Host.Contains("linkedin.", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (segments.Length < 2 || !string.Equals(segments[0], "in", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		slug = Uri.UnescapeDataString(segments[1]).Trim().ToLowerInvariant();
		return slug.Length > 0;
	}

	private static bool IsLinkedInHost(string url)
	{
		return !string.IsNullOrWhiteSpace(url)
			&& Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
			&& uri.Host.Contains("linkedin.", StringComparison.OrdinalIgnoreCase);
	}

	private static string CleanLinkedInProfileUrl(string url)
	{
		if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri))
		{
			return url?.Trim() ?? "";
		}
		var path = uri.AbsolutePath.TrimEnd('/');
		return $"{uri.Scheme}://{uri.Host}{path}";
	}

	private static string StripLinkedInSuffix(string title)
	{
		var clean = (title ?? "").Trim();
		clean = Regex.Replace(clean, "\\s*[|\\-–—]\\s*LinkedIn\\s*$", "", RegexOptions.IgnoreCase);
		return clean.Trim();
	}

	private static (string Name, string Headline, string Company) ParseLinkedInTitle(string title, string websiteTitle)
	{
		var clean = (title ?? "").Trim();
		string name = "";
		string headline = "";
		if (!string.IsNullOrEmpty(clean))
		{
			var parts = Regex.Split(clean, "\\s+[-–—|]\\s+", RegexOptions.None);
			name = parts[0].Trim();
			if (parts.Length > 1)
			{
				headline = string.Join(" — ", parts.Skip(1).Select(p => p.Trim()).Where(p => p.Length > 0));
			}
		}
		if (string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(websiteTitle))
		{
			var dot = websiteTitle.Split('·');
			if (dot.Length >= 2)
			{
				name = dot[1].Trim();
			}
		}
		var company = ExtractCompany(headline);
		return (name, headline, company);
	}

	private static string ExtractCompany(string headline)
	{
		if (string.IsNullOrWhiteSpace(headline))
		{
			return "";
		}
		var match = Regex.Match(headline, "(?:\\bchez\\b|\\bat\\b|@)\\s+([^|·—–]+)$", RegexOptions.IgnoreCase);
		if (match.Success)
		{
			return match.Groups[1].Value.Trim();
		}
		return "";
	}

	private static string NameFromSlug(string slug)
	{
		var cleaned = Regex.Replace(slug ?? "", "-[0-9a-f]{5,}$", "", RegexOptions.IgnoreCase);
		return cleaned.Replace('-', ' ').Trim();
	}

	private static PublicProfile MergeLinkedInProfile(PublicProfile existing, PublicProfile incoming)
	{
		return new PublicProfile
		{
			Name = Pick(existing.Name, incoming.Name),
			Headline = Pick(existing.Headline, incoming.Headline),
			About = Pick(existing.About, incoming.About),
			Url = Pick(existing.Url, incoming.Url),
			Photo = Pick(existing.Photo, incoming.Photo),
			Location = Pick(existing.Location, incoming.Location),
			Company = Pick(existing.Company, incoming.Company),
			School = Pick(existing.School, incoming.School),
		};
	}

	private static PublicProfile Prefer(PublicProfile light, PublicProfile full)
	{
		return new PublicProfile
		{
			Name = Pick(full.Name, light.Name),
			Headline = Pick(full.Headline, light.Headline),
			About = Pick(full.About, light.About),
			Url = Pick(full.Url, light.Url),
			Photo = Pick(full.Photo, light.Photo),
			Location = Pick(full.Location, light.Location),
			Company = Pick(full.Company, light.Company),
			School = Pick(full.School, light.School),
		};
	}

	private static string Pick(string preferred, string fallback)
	{
		return string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
	}

	private static string StripMarkdown(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return "";
		}
		return Regex.Replace(value, "\\*\\*(.+?)\\*\\*|__(.+?)__", "$1$2");
	}

	private static bool IsFollowUpPrompt(string folded)
	{
		if (string.IsNullOrEmpty(folded))
		{
			return false;
		}
		foreach (var prefix in FollowUpPrefixes)
		{
			if (folded.StartsWith(prefix, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsTrailingLinksHeading(string folded)
	{
		foreach (var heading in TrailingLinksSectionHeadings)
		{
			if (folded == heading || folded.StartsWith(heading + " ", StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	internal static string CanonicalUrl(string url)
	{
		if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
		{
			return "";
		}
		var host = uri.Host.ToLowerInvariant();
		if (host.StartsWith("www.", StringComparison.Ordinal))
		{
			host = host.Substring(4);
		}
		var path = uri.AbsolutePath.TrimEnd('/').ToLowerInvariant();
		return host + path;
	}

	private static string CanonicalMediaKey(string imageUrl, string thumbUrl)
	{
		var raw = !string.IsNullOrWhiteSpace(imageUrl) ? imageUrl : thumbUrl;
		if (!Uri.TryCreate(raw.Trim(), UriKind.Absolute, out var uri))
		{
			return raw.Trim().ToLowerInvariant();
		}
		return (uri.Host + uri.AbsolutePath).ToLowerInvariant();
	}

	private static string HostOf(string url)
	{
		if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
		{
			return "";
		}
		return uri.Host.ToLowerInvariant();
	}

	private static string AiText(JsonElement page)
	{
		if (page.ValueKind != JsonValueKind.Object)
		{
			return null;
		}
		foreach (var name in new[] { "aiOverview", "aiOverviewText", "aiOverviewMarkdown", "aiMode", "aiModeAnswer" })
		{
			if (!page.TryGetProperty(name, out var value))
			{
				continue;
			}
			if (value.ValueKind == JsonValueKind.String)
			{
				return value.GetString();
			}
			if (value.ValueKind == JsonValueKind.Object)
			{
				foreach (var key in new[] { "text", "content", "markdown", "summary" })
				{
					if (value.TryGetProperty(key, out var inner) && inner.ValueKind == JsonValueKind.String)
					{
						var found = inner.GetString();
						if (!string.IsNullOrWhiteSpace(found))
						{
							return found;
						}
					}
				}
			}
		}
		return null;
	}

	private static string FirstText(JsonElement item, params string[] names)
	{
		foreach (var name in names)
		{
			var value = Text(item, name);
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value;
			}
		}
		return "";
	}

	private static string Text(JsonElement item, string name)
	{
		if (!item.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
		{
			return "";
		}
		return (value.GetString() ?? "").Trim();
	}

	private static int IntVal(JsonElement item, params string[] names)
	{
		foreach (var name in names)
		{
			if (item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var parsed))
			{
				return parsed;
			}
		}
		return 0;
	}

	private static string Cut(string value, int max)
	{
		if (string.IsNullOrEmpty(value) || value.Length <= max)
		{
			return value ?? "";
		}
		return value.Substring(0, max).TrimEnd();
	}

	private static bool IsAllDigits(string token)
	{
		foreach (var character in token)
		{
			if (character < '0' || character > '9')
			{
				return false;
			}
		}
		return token.Length > 0;
	}

	public static string Fold(string value)
	{
		var text = (value ?? "").Normalize(NormalizationForm.FormD).ToLowerInvariant();
		var builder = new StringBuilder(text.Length);
		var pendingSpace = false;
		foreach (var character in text)
		{
			var category = CharUnicodeInfo.GetUnicodeCategory(character);
			if (category == UnicodeCategory.NonSpacingMark)
			{
				continue;
			}
			if ((character >= 'a' && character <= 'z') || (character >= '0' && character <= '9'))
			{
				if (pendingSpace && builder.Length > 0)
				{
					builder.Append(' ');
				}
				pendingSpace = false;
				builder.Append(character);
			}
			else
			{
				pendingSpace = true;
			}
		}
		return builder.ToString().Trim();
	}
}
