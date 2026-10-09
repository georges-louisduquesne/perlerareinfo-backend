using System;

namespace ApiPerleRare.Application.Tasks;

/// <summary>
/// Alertes affichées sur l'accueil : passées et toute la journée en cours.
/// Un seuil à minuit excluait une alerte créée pour plus tard le même jour.
/// </summary>
public static class AccueilTacheWindow
{
	public static DateTime DueBefore(DateTime now) => now.Date.AddDays(1);
}
