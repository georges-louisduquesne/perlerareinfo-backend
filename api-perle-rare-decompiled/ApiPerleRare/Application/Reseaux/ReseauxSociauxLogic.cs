using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace ApiPerleRare.Application.Reseaux;

internal enum ReseauxResetDecision
{
	None,
	RecordOnly,
	Reset,
}

internal sealed class ReseauPost
{
	public string Id { get; init; }

	public string Reseau { get; init; }

	public string Url { get; init; }

	public DateTime PublishedUtc { get; init; }

	public string Vignette { get; init; }

	public string Texte { get; init; }
}

internal sealed class ReseauSollicitationPick
{
	public uint Id { get; init; }

	public DateTime DateEnvoi { get; init; }
}

internal sealed class ReseauxConseillerPick
{
	public uint Ref { get; init; }

	public string Prenom { get; init; }

	public string Nom { get; init; }

	public int Solde { get; init; }
}

/// <summary>
/// Règles de la page Réseaux sociaux : modèles, éligibilité, quota du lundi, posts.
/// </summary>
internal static class ReseauxSociauxLogic
{
	internal const string Like = "like";

	internal const string Commentaire = "commentaire";

	internal const string LinkedIn = "linkedin";

	internal const string Instagram = "instagram";

	internal const string WeekParam = "RESEAUX_QUOTA_WEEK";

	internal const string SyncParam = "RESEAUX_POSTS_SYNC";

	internal const int PostsPerNetwork = 6;

	internal const int PostsShown = 6;

	internal const int SollicitationsShown = 50;

	internal const int SyncStaleSeconds = 2 * 60 * 60;

	internal const int QuotaMax = 100;

	internal static string DefaultLikeContenu()
	{
		return "Bonjour {prenom},\n\nPourrais-tu aimer cette publication Perle Rare ?\n{url}\n\nMerci !";
	}

	internal static string DefaultCommentaireContenu()
	{
		return "Bonjour {prenom},\n\nPourrais-tu copier-coller un message en commentaire de cette publication ?\n{url}\n\nMerci !";
	}

	internal static bool IsType(string type)
	{
		return type == Like || type == Commentaire;
	}

	internal static bool IsReseau(string reseau)
	{
		return reseau == LinkedIn || reseau == Instagram;
	}

	internal static string ApplyTemplate(string contenu, string prenom, string nom, string url)
	{
		return (contenu ?? string.Empty)
			.Replace("{prenom}", prenom ?? string.Empty, StringComparison.Ordinal)
			.Replace("{nom}", nom ?? string.Empty, StringComparison.Ordinal)
			.Replace("{url}", url ?? string.Empty, StringComparison.Ordinal);
	}

	internal static int ClampQuota(int value)
	{
		if (value < 0)
		{
			return 0;
		}
		return value > QuotaMax ? QuotaMax : value;
	}

	/// <summary>
	/// Première saisie : un quota posé alors que le solde est encore à 0 remplit le solde pour la semaine en cours.
	/// Ensuite le solde ne dépasse pas le quota.
	/// </summary>
	internal static (int Quota, int Solde) ApplyQuotaEdit(int previousQuota, int previousSolde, int requestedQuota, int requestedSolde)
	{
		int quota = ClampQuota(requestedQuota);
		int solde = ClampQuota(requestedSolde);
		if (previousQuota == 0 && previousSolde == 0 && quota > 0 && solde == 0)
		{
			solde = quota;
		}
		if (solde > quota)
		{
			solde = quota;
		}
		return (quota, solde);
	}

	internal static bool CanSolicit(int solde, bool alreadySolicited)
	{
		return solde > 0 && !alreadySolicited;
	}

	internal static IReadOnlyList<ReseauxConseillerPick> Eligible(
		IEnumerable<ReseauxConseillerPick> conseillers,
		string postId,
		string reseau,
		IEnumerable<(string PostId, string Reseau, uint Ref)> pairs)
	{
		var taken = new HashSet<uint>();
		if (pairs != null)
		{
			foreach ((string pairPost, string pairReseau, uint pairRef) in pairs)
			{
				if (pairPost == postId && pairReseau == reseau)
				{
					taken.Add(pairRef);
				}
			}
		}
		return (conseillers ?? Array.Empty<ReseauxConseillerPick>())
			.Where((ReseauxConseillerPick c) => c != null && CanSolicit(c.Solde, taken.Contains(c.Ref)))
			.OrderByDescending((ReseauxConseillerPick c) => c.Solde)
			.ThenBy((ReseauxConseillerPick c) => c.Prenom ?? string.Empty, StringComparer.Create(new CultureInfo("fr-FR"), ignoreCase: true))
			.ThenBy((ReseauxConseillerPick c) => c.Nom ?? string.Empty, StringComparer.Create(new CultureInfo("fr-FR"), ignoreCase: true))
			.ToList();
	}

	internal static ReseauxResetDecision DecideReset(int storedWeekKey, int currentWeekKey)
	{
		if (currentWeekKey <= 0)
		{
			return ReseauxResetDecision.None;
		}
		if (storedWeekKey <= 0)
		{
			return ReseauxResetDecision.RecordOnly;
		}
		return storedWeekKey == currentWeekKey ? ReseauxResetDecision.None : ReseauxResetDecision.Reset;
	}

	/// <summary>Clé année ISO × 100 + numéro de semaine. Le lundi 00:00 heure de Paris ouvre une nouvelle clé.</summary>
	internal static int IsoWeekKey(DateTime parisLocal)
	{
		DateTime date = parisLocal.Date;
		return ISOWeek.GetYear(date) * 100 + ISOWeek.GetWeekOfYear(date);
	}

	internal static TimeZoneInfo ParisTimeZone()
	{
		if (TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Paris", out TimeZoneInfo paris))
		{
			return paris;
		}
		return TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
	}

	internal static DateTime ToParisUnspecified(DateTime instant, TimeZoneInfo paris)
	{
		DateTime utc = instant.Kind == DateTimeKind.Utc
			? instant
			: DateTime.SpecifyKind(instant, DateTimeKind.Utc);
		DateTime local = TimeZoneInfo.ConvertTimeFromUtc(utc, paris);
		return DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
	}

	internal static string FormatParis(DateTime parisLocal)
	{
		return parisLocal.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
	}

	internal static bool SyncIsStale(int storedUnix, int nowUnix)
	{
		if (storedUnix <= 0 || nowUnix <= 0)
		{
			return true;
		}
		return nowUnix - storedUnix >= SyncStaleSeconds;
	}

	/// <summary>Les dernières sollicitations, du plus récent envoi au plus ancien.</summary>
	internal static IReadOnlyList<ReseauSollicitationPick> LatestSollicitations(IEnumerable<ReseauSollicitationPick> rows, int limit)
	{
		int take = limit <= 0 ? SollicitationsShown : limit;
		return (rows ?? Array.Empty<ReseauSollicitationPick>())
			.Where((ReseauSollicitationPick row) => row != null)
			.OrderByDescending((ReseauSollicitationPick row) => row.DateEnvoi)
			.ThenByDescending((ReseauSollicitationPick row) => row.Id)
			.Take(take)
			.ToList();
	}

	internal static IReadOnlyList<ReseauPost> Latest(IEnumerable<ReseauPost> posts, int limit)
	{
		int take = limit <= 0 ? PostsShown : limit;
		return (posts ?? Array.Empty<ReseauPost>())
			.Where((ReseauPost p) => p != null && !string.IsNullOrWhiteSpace(p.Id) && !string.IsNullOrWhiteSpace(p.Url))
			.OrderByDescending((ReseauPost p) => p.PublishedUtc)
			.ThenBy((ReseauPost p) => p.Reseau, StringComparer.Ordinal)
			.ThenBy((ReseauPost p) => p.Id, StringComparer.Ordinal)
			.Take(take)
			.ToList();
	}

	internal static IReadOnlyList<ReseauPost> ParseLinkedIn(string json)
	{
		return ParseArray(json, LinkedIn, ReadLinkedIn);
	}

	internal static IReadOnlyList<ReseauPost> ParseInstagram(string json)
	{
		return ParseArray(json, Instagram, ReadInstagram);
	}

	private static IReadOnlyList<ReseauPost> ParseArray(string json, string reseau, Func<JsonElement, string, ReseauPost> read)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return Array.Empty<ReseauPost>();
		}
		try
		{
			using JsonDocument doc = JsonDocument.Parse(json);
			if (doc.RootElement.ValueKind != JsonValueKind.Array)
			{
				return Array.Empty<ReseauPost>();
			}
			var list = new List<ReseauPost>();
			foreach (JsonElement item in doc.RootElement.EnumerateArray())
			{
				ReseauPost post = read(item, reseau);
				if (post != null)
				{
					list.Add(post);
				}
			}
			return list;
		}
		catch (JsonException)
		{
			return Array.Empty<ReseauPost>();
		}
	}

	private static ReseauPost ReadLinkedIn(JsonElement item, string reseau)
	{
		string id = ReadId(item, "id");
		string url = ReadString(item, "linkedinUrl");
		if (!TryReadDate(item, "postedAt", "date", out DateTime published) && !TryReadTimestamp(item, out published))
		{
			return null;
		}
		if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(url))
		{
			return null;
		}
		return new ReseauPost
		{
			Id = id.Trim(),
			Reseau = reseau,
			Url = url.Trim(),
			PublishedUtc = DateTime.SpecifyKind(published, DateTimeKind.Utc),
			Vignette = ReadLinkedInImage(item),
			Texte = TrimTexte(ReadString(item, "content")),
		};
	}

	private static ReseauPost ReadInstagram(JsonElement item, string reseau)
	{
		string id = ReadId(item, "id");
		string url = ReadString(item, "url");
		if (string.IsNullOrWhiteSpace(url))
		{
			string code = ReadString(item, "shortCode");
			if (!string.IsNullOrWhiteSpace(code))
			{
				url = "https://www.instagram.com/p/" + code.Trim() + "/";
			}
		}
		if (!TryReadIso(ReadString(item, "timestamp"), out DateTime published))
		{
			return null;
		}
		if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(url))
		{
			return null;
		}
		return new ReseauPost
		{
			Id = id.Trim(),
			Reseau = reseau,
			Url = url.Trim(),
			PublishedUtc = DateTime.SpecifyKind(published, DateTimeKind.Utc),
			Vignette = ReadString(item, "displayUrl"),
			Texte = TrimTexte(ReadString(item, "caption")),
		};
	}

	private static string ReadLinkedInImage(JsonElement item)
	{
		if (item.TryGetProperty("postImages", out JsonElement images) && images.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement image in images.EnumerateArray())
			{
				string url = ReadString(image, "url");
				if (!string.IsNullOrWhiteSpace(url))
				{
					return url;
				}
			}
		}
		if (item.TryGetProperty("postVideo", out JsonElement video) && video.ValueKind == JsonValueKind.Object)
		{
			return ReadString(video, "thumbnailUrl");
		}
		return string.Empty;
	}

	private static bool TryReadTimestamp(JsonElement item, out DateTime published)
	{
		published = default;
		if (!item.TryGetProperty("postedAt", out JsonElement posted) || posted.ValueKind != JsonValueKind.Object)
		{
			return false;
		}
		if (!posted.TryGetProperty("timestamp", out JsonElement stamp))
		{
			return false;
		}
		long ms = stamp.ValueKind == JsonValueKind.Number && stamp.TryGetInt64(out long value) ? value : 0;
		if (ms <= 0)
		{
			return false;
		}
		try
		{
			published = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
			return true;
		}
		catch (ArgumentOutOfRangeException)
		{
			return false;
		}
	}

	private static bool TryReadDate(JsonElement item, string parent, string child, out DateTime published)
	{
		published = default;
		if (!item.TryGetProperty(parent, out JsonElement posted) || posted.ValueKind != JsonValueKind.Object)
		{
			return false;
		}
		return TryReadIso(ReadString(posted, child), out published);
	}

	private static bool TryReadIso(string raw, out DateTime published)
	{
		published = default;
		if (string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}
		if (!DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset parsed))
		{
			return false;
		}
		published = parsed.UtcDateTime;
		return true;
	}

	private static string ReadId(JsonElement item, string name)
	{
		if (!item.TryGetProperty(name, out JsonElement value))
		{
			return string.Empty;
		}
		if (value.ValueKind == JsonValueKind.String)
		{
			return value.GetString() ?? string.Empty;
		}
		if (value.ValueKind == JsonValueKind.Number)
		{
			return value.GetRawText();
		}
		return string.Empty;
	}

	private static string ReadString(JsonElement item, string name)
	{
		if (!item.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.String)
		{
			return string.Empty;
		}
		return value.GetString() ?? string.Empty;
	}

	private static string TrimTexte(string value)
	{
		string text = (value ?? string.Empty).Trim();
		if (text.Length <= 500)
		{
			return text;
		}
		return text.Substring(0, 500);
	}
}
