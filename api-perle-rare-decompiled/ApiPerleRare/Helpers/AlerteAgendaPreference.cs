namespace ApiPerleRare.Helpers;

/// <summary>
/// CP_AlerteAgenda : le bouton « Ajouter à l'agenda » des popins de création d'alerte.
/// À la création d'une fiche, la valeur stockée est 1.
/// Le conseiller connecté change la sienne. Un admin change celle de la fiche ouverte.
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

	/// <summary>
	/// Fiche conseiller : la ligne demandée, si l'appelant est admin ou s'il s'agit de la sienne.
	/// Sans claim, ne pas viser le compte historique 0.
	/// </summary>
	internal static uint? EditableRow(bool hasClaim, int callerId, bool isAdmin, int requestedId)
	{
		if (!hasClaim || callerId < 0 || requestedId < 0)
		{
			return null;
		}
		if (!isAdmin && requestedId != callerId)
		{
			return null;
		}
		return (uint)requestedId;
	}
}
