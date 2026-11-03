using System;
using System.Linq;
using ApiPerleRare.Models;

namespace ApiPerleRare.Application.Candidats;

/// <summary>
/// Filtres SQL pour l'accueil admin et les listes candidats (évite de charger toutes les tâches / événements).
/// </summary>
public static class HomeCandidatAccueilQueries
{
	public const string CandidatStatut = "Candidat";

	public const string RdvCategory = "RV";

	/// <summary>Alertes ouvertes des candidats (y compris inactifs), toutes échéances.</summary>
	public static IQueryable<ConseillersPersonnelsTaches> OpenTachesForCandidats(
		IQueryable<ConseillersPersonnelsTaches> taches,
		IQueryable<ConseillersPersonnels> conseillers)
	{
		return from cpt in taches
			join cp in conseillers on cpt.CptRefConseiller equals cp.CpRefConseiller
			where cp.CpStatut == CandidatStatut && cpt.CptEtat != "fait"
			select cpt;
	}

	/// <summary>RDV actifs du jour et suivants, types catégorie RV, candidats uniquement.</summary>
	public static IQueryable<ConseillersPersonnelsEvenements> UpcomingRdvForCandidats(
		IQueryable<ConseillersPersonnelsEvenements> events,
		IQueryable<ConseillersPersonnels> conseillers,
		IQueryable<ConseillersPersonnelsTypesEvenements> types,
		DateTime today)
	{
		return from cpe in events
			join cp in conseillers on cpe.CpeRefConseiller equals cp.CpRefConseiller
			join te in types on cpe.CpeTypeEvenement equals te.CpteTypeEvenement
			where cp.CpStatut == CandidatStatut
				&& cpe.CpeStatut == 1
				&& te.CpteCategorieEvenement == RdvCategory
				&& cpe.CpeDateRealisation != null
				&& cpe.CpeDateRealisation >= today
			select cpe;
	}
}
