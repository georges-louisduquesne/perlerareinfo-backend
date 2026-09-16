namespace ApiPerleRare.Application.Events;

/// <summary>
/// Home « transactions réalisées en attente d'encaissement ».
/// Live CRM shows the latest past compromis/acte for an active client with honoraires,
/// whether or not a HON invoice number is already assigned.
/// </summary>
public static class EncaissementsEnCoursFilter
{
	public const string ActiveClientStatus = "CLIENT ACTIF";

	public static bool HasHonorairesToCollect(decimal? mttHono)
	{
		return mttHono.HasValue && mttHono.Value != 0m;
	}

	public static bool IsActiveClient(string statut)
	{
		return statut == ActiveClientStatus;
	}
}
