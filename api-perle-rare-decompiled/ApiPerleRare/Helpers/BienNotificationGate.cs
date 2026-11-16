using System;

namespace ApiPerleRare.Helpers;

/// <summary>
/// E-mails « nouveau bien » et « modif prix » d’un contact.
/// L’interrupteur <c>C_AlerteMail</c> est le choix du conseiller.
/// Un RV COMPROMIS ou RV ACTE AUTHENT. actif coupe l’envoi, même si l’interrupteur est resté à 1.
/// </summary>
internal static class BienNotificationGate
{
	internal const string TypeCompromis = "RV COMPROMIS";

	internal const string TypeActe = "RV ACTE AUTHENT.";

	internal const string TypeActeBare = "RV ACTE AUTHENT";

	/// <summary>File d’envoi : plus à envoyer, sans compter comme une erreur.</summary>
	internal const sbyte SkippedState = 3;

	internal static bool IsBlockingType(string type)
	{
		if (string.IsNullOrWhiteSpace(type))
		{
			return false;
		}
		string key = type.Trim();
		return key.Equals(TypeCompromis, StringComparison.OrdinalIgnoreCase)
			|| key.Equals(TypeActe, StringComparison.OrdinalIgnoreCase)
			|| key.Equals(TypeActeBare, StringComparison.OrdinalIgnoreCase);
	}

	internal static bool Allows(sbyte alerteMail, bool hasActiveBlockingEvent)
	{
		return alerteMail == 1 && !hasActiveBlockingEvent;
	}
}
