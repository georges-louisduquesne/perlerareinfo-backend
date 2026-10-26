using System;

namespace ApiPerleRare.ScheduledTasks;

/// <summary>
/// Mail interne à la création d'un évènement de transaction.
/// Mission 2 = L (location), 4 = B (location bailleur) : libellés dossier / bail.
/// </summary>
internal static class TransactionEventMail
{
	internal const string FromAddress = "support@perle-rare.com";

	internal const string CursorParam = "TransactionEventMailLastId";

	internal static readonly string[] Recipients = new string[3]
	{
		"ct@perle-rare.com",
		"gld@perle-rare.com",
		"mn@perle-rare.com"
	};

	internal static readonly string[] WatchedTypes = new string[5]
	{
		"OFFRE",
		"OFFRE ACCEPTEE",
		"OFFRE REFUSEE",
		"RV COMPROMIS",
		"RV ACTE AUTHENT."
	};

	internal static bool Enabled(string value)
	{
		return string.Equals(value?.Trim(), "on", StringComparison.OrdinalIgnoreCase);
	}

	internal static bool Watches(string eventType)
	{
		if (string.IsNullOrEmpty(eventType))
		{
			return false;
		}
		for (int i = 0; i < WatchedTypes.Length; i++)
		{
			if (string.Equals(WatchedTypes[i], eventType, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	internal static bool UsesRentalLabels(int missionId)
	{
		return missionId == 2 || missionId == 4;
	}

	internal static string SubjectLabel(string eventType, int missionId)
	{
		if (!UsesRentalLabels(missionId))
		{
			return eventType ?? "";
		}
		switch (eventType)
		{
		case "OFFRE":
			return "DEPOT DOSSIER";
		case "OFFRE REFUSEE":
			return "DOSSIER REFUSE";
		case "OFFRE ACCEPTEE":
			return "DOSSIER ACCEPTE";
		case "RV ACTE AUTHENT.":
			return "RV SIGNATURE BAIL";
		default:
			return eventType ?? "";
		}
	}

	internal static string ClientName(string eventName, string familyName)
	{
		string fromEvent = (eventName ?? "").Trim();
		if (fromEvent.Length > 0)
		{
			return fromEvent;
		}
		return (familyName ?? "").Trim();
	}

	internal static string Subject(string eventType, int missionId, string clientName)
	{
		string label = SubjectLabel(eventType, missionId);
		string name = (clientName ?? "").Trim();
		if (name.Length == 0)
		{
			return label;
		}
		return label + " " + name;
	}

	internal static string HtmlBody(string comment)
	{
		string text = comment ?? "";
		return System.Net.WebUtility.HtmlEncode(text).Replace("\r\n", "\n").Replace("\n", "<br>");
	}

	internal static int CursorAfterSend(int cursor, int eventId, bool sent)
	{
		if (sent && eventId > cursor)
		{
			return eventId;
		}
		return cursor;
	}
}
