using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;

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
}

/// <summary>
/// Lit les réponses Apify et décide si un libellé est exactement le prénom + nom.
/// Un résultat dont le nom contient des mots en plus (Jean-Baptiste Dupont pour Jean Dupont) ne compte pas.
/// </summary>
public static class PersonLookupReader
{
	private static readonly HashSet<string> SkipTokens = new HashSet<string>(StringComparer.Ordinal)
	{
		"de", "des", "du", "la", "le", "les", "d", "au", "aux",
		"mr", "mme", "mlle", "dr", "docteur", "pr", "professeur", "me", "maitre",
	};

	public static object Unavailable()
	{
		return new
		{
			available = false,
			aiSummary = (string)null,
			linkedin = Array.Empty<PublicProfile>(),
			web = Array.Empty<PublicWebHit>(),
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
				});
			}
		}
		return hits;
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
				return Cut(text.Trim(), 1200);
			}
		}
		return null;
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

	private static string AiText(JsonElement page)
	{
		if (page.ValueKind != JsonValueKind.Object)
		{
			return null;
		}
		foreach (var name in new[] { "aiOverview", "aiOverviewText", "aiOverviewMarkdown" })
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
