using System;
using System.Globalization;

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

	internal static bool ShowsAppointmentDate(string eventType)
	{
		return eventType == "RV COMPROMIS" || eventType == "RV ACTE AUTHENT.";
	}

	internal static string FormatAppointment(DateTime when)
	{
		return "le " + when.ToString("dd/MM/yy", CultureInfo.InvariantCulture) + " à " + when.ToString("HH:mm", CultureInfo.InvariantCulture);
	}

	internal static string PersonName(string firstName, string lastName)
	{
		string first = (firstName ?? "").Trim();
		string last = (lastName ?? "").Trim();
		if (last.Length > 0)
		{
			last = last.ToUpperInvariant();
		}
		if (first.Length == 0)
		{
			return last;
		}
		if (last.Length == 0)
		{
			return first;
		}
		return first + " " + last;
	}

	internal static string Subject(string eventType, int missionId, string clientName, DateTime? appointment = null)
	{
		string label = SubjectLabel(eventType, missionId);
		string name = (clientName ?? "").Trim();
		string head = name.Length == 0 ? label : label + " " + name;
		if (!appointment.HasValue || !ShowsAppointmentDate(eventType))
		{
			return head;
		}
		return head + " " + FormatAppointment(appointment.Value);
	}

	internal static string HtmlBody(string comment, string negotiatorName, string advisorName)
	{
		string text = System.Net.WebUtility.HtmlEncode(comment ?? "").Replace("\r\n", "\n").Replace("\n", "<br>");
		string negotiator = "Négociateur : " + System.Net.WebUtility.HtmlEncode((negotiatorName ?? "").Trim());
		string advisor = "Conseiller : " + System.Net.WebUtility.HtmlEncode((advisorName ?? "").Trim());
		if (text.Length == 0)
		{
			return negotiator + "<br>" + advisor;
		}
		return text + "<br>" + negotiator + "<br>" + advisor;
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
