using System;

namespace ApiPerleRare.Helpers;

/// <summary>
/// Notifications mobiles « nouveau bien » et « modif prix » d’un contact.
/// L’interrupteur <c>C_AlerteMail</c> est le choix du conseiller.
/// Un RV COMPROMIS ou RV ACTE AUTHENT. actif coupe l’envoi, même si l’interrupteur est resté à 1.
/// L’API qui tourne réellement met la file mobile à <c>N_State = -3</c> et la marque <c>3</c> une fois envoyée.
/// </summary>
internal static class BienNotificationGate
{
	internal const string TypeCompromis = "RV COMPROMIS";

	internal const string TypeActe = "RV ACTE AUTHENT.";

	internal const string TypeActeBare = "RV ACTE AUTHENT";

	/// <summary>File mobile en attente d’envoi (lue par l’API qui pousse les notifications).</summary>
	internal const sbyte MobilePendingState = -3;

	/// <summary>Notification mobile envoyée.</summary>
	internal const sbyte MobileSentState = 3;

	/// <summary>Pas envoyée : interrupteur coupé ou RV COMPROMIS / RV ACTE AUTHENT. actif. Distinct de l’état « envoyée ».</summary>
	internal const sbyte SkippedState = 4;

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
