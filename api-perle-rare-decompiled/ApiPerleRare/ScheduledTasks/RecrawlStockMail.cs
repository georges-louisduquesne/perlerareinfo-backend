using System;

namespace ApiPerleRare.ScheduledTasks;

/// <summary>
/// Rappel mensuel : volumes de biens à contrôler sur les portails, et les prompts à coller dans Cursor.
/// </summary>
internal static class RecrawlStockMail
{
	internal const string EnvVar = "PR_RECRAWL_STOCK_MAIL";

	internal const string FromAddress = "support@perle-rare.com";

	internal const string ToAddress = "gld@perle-rare.com";

	internal const string Subject = "Annonces à recrawler";

	internal const string CursorParam = "RecrawlStockMailMonth";

	internal const int SendHourParis = 8;

	internal static bool Enabled(string value)
	{
		return string.Equals(value?.Trim(), "on", StringComparison.OrdinalIgnoreCase);
	}

	internal static TimeZoneInfo ParisZone()
	{
		try
		{
			return TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
		}
		catch (TimeZoneNotFoundException)
		{
			return TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
		}
	}

	internal static int MonthKey(DateTime utc)
	{
		DateTime paris = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), ParisZone());
		return paris.Year * 100 + paris.Month;
	}

	/// <summary>Une fois par mois calendaire de Paris, à partir de 8 h. Un mois manqué part au prochain passage.</summary>
	internal static bool Due(int? lastMonth, DateTime utc)
	{
		DateTime paris = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), ParisZone());
		if (paris.Hour < SendHourParis)
		{
			return false;
		}
		return lastMonth != paris.Year * 100 + paris.Month;
	}

	internal static string Body(int over90, int between30And90)
	{
		return "Biens actifs (P_State = 1, sans date de fin) à contrôler sur les portails.\n"
			+ "Plus de 90 jours : " + over90 + "\n"
			+ "30 à 90 jours : " + between30And90 + "\n"
			+ "\n"
			+ "Prompt à coller dans Cursor — plus de 90 jours :\n"
			+ PromptOver90(over90) + "\n"
			+ "\n"
			+ "Prompt à coller dans Cursor — 30 à 90 jours :\n"
			+ PromptMid(between30And90);
	}

	internal static string PromptOver90(int count)
	{
		return "Lance le contrôle Apify des annonces portail pour les biens actifs dont P_DateLastUpdate a plus de 90 jours. "
			+ "Volume lu : " + count + " biens. "
			+ "Critère SQL : P_State = 1 AND (P_DateFin IS NULL OR P_DateFin < '1000-01-01') AND IFNULL(P_EstRattrapage, 0) <> 4 AND P_DateLastUpdate < DATE_SUB(NOW(), INTERVAL 90 DAY). "
			+ "Dans P_Ads, ignore A_VENDRE_A_LOUER, avendrealouer, MustBeIgnored et les URL déjà vues. "
			+ "Cheerio (lots de 250, mémoire 4096, timeout 900, proxy résidentiel FR) pour les autres URL http. "
			+ "Playwright DataDome (lots de 50, mémoire 8192, timeout 600) pour seloger.com, meilleursagents.com, bellesdemeures.com et lux-residence.com. "
			+ "Actor apify/website-content-crawler, deux runs en parallèle. "
			+ "Contrôle seulement : ne ferme aucun bien et ne pose pas P_EstRattrapage = 4 sans un ordre écrit séparé. "
			+ "Ne committe pas les fichiers temporaires. À la fin, donne le nombre d’URL, le coût usageTotalUsd, et les biens dont toutes les URL portail sont mortes.";
	}

	internal static string PromptMid(int count)
	{
		return "Lance le contrôle Apify Cheerio des annonces portail pour les biens actifs dont P_DateLastUpdate a entre 30 et 90 jours. "
			+ "Volume lu : " + count + " biens. "
			+ "Critère SQL : P_State = 1 AND (P_DateFin IS NULL OR P_DateFin < '1000-01-01') AND IFNULL(P_EstRattrapage, 0) <> 4 AND P_DateLastUpdate >= DATE_SUB(NOW(), INTERVAL 90 DAY) AND P_DateLastUpdate < DATE_SUB(NOW(), INTERVAL 30 DAY). "
			+ "Dans P_Ads, ignore A_VENDRE_A_LOUER, avendrealouer, MustBeIgnored et les URL déjà vues. "
			+ "Cheerio seulement (lots de 250, mémoire 4096, timeout 900, proxy résidentiel FR). Ne lance pas Playwright. "
			+ "Actor apify/website-content-crawler, deux runs en parallèle. "
			+ "Contrôle seulement : ne ferme aucun bien et ne pose pas P_EstRattrapage = 4 sans un ordre écrit séparé. "
			+ "Ne committe pas les fichiers temporaires. À la fin, donne le nombre d’URL, le coût usageTotalUsd, et les biens dont toutes les URL Cheerio sont mortes.";
	}

	internal static string ToHtml(string body)
	{
		string encoded = System.Net.WebUtility.HtmlEncode(body ?? "").Replace("\r\n", "\n").Replace("\n", "<br>");
		return encoded;
	}
}
