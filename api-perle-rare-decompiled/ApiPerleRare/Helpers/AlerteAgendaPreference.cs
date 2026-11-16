namespace ApiPerleRare.Helpers;

/// <summary>
/// CP_AlerteAgenda : le bouton « Ajouter à l'agenda » des popins de création d'alerte.
/// À la création d'une fiche, la valeur stockée est 1. Seul le conseiller connecté peut la changer.
/// </summary>
internal static class AlerteAgendaPreference
{
	internal static bool ValueOnCreate() => true;

	internal static int ToStoredFlag(bool checkedByDefault) => checkedByDefault ? 1 : 0;

	/// <summary>
	/// Cible uniquement la ligne du JWT. Sans claim, ne pas confondre avec le compte historique 0.
	/// </summary>
	internal static uint? OwnRow(bool hasClaim, int userId)
	{
		if (!hasClaim || userId < 0)
		{
			return null;
		}
		return (uint)userId;
	}
}
