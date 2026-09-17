using System;

namespace ApiPerleRare.Application.Events;

/// <summary>
/// Home « transactions réalisées en attente d'encaissement ».
/// Live CRM shows the latest past acte authentique for an active client with honoraires.
/// Compromis-only files stay on the « transactions en cours » tab.
/// </summary>
public static class EncaissementsEnCoursFilter
{
	public const string ActiveClientStatus = "CLIENT ACTIF";

	/// <summary>Exact PHP Accueil clause (no wildcards).</summary>
	public const string ActiveClientSql = "contacts_recherche.C_Statut like 'CLIENT ACTIF'";

	public const string RealizedEventType = "RV ACTE AUTHENT.";

	public static bool HasHonorairesToCollect(decimal? mttHono)
	{
		return mttHono.HasValue && mttHono.Value != 0m;
	}

	public static bool IsActiveClient(string statut)
	{
		return string.Equals((statut ?? string.Empty).Trim(), ActiveClientStatus, StringComparison.OrdinalIgnoreCase);
	}

	public static bool IsRealizedTransaction(string typeEvenement)
	{
		return typeEvenement == RealizedEventType;
	}
}
