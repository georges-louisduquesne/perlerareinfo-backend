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

	/// <summary>
	/// Onglet Événements de Mes candidats : actif, du jour ou à venir, tout type
	/// (MAIL IN compris). Un passage de statut reste listé même si la personne
	/// n’est plus au statut Candidat.
	/// </summary>
	public static bool ListedOnCandidatEventsTab(
		string conseillerStatut,
		string eventType,
		sbyte eventStatut,
		DateTime? realisation,
		DateTime today)
	{
		if (eventStatut != 1 || !realisation.HasValue || !IsOnOrAfterDay(realisation.Value, today))
		{
			return false;
		}
		if (string.Equals((conseillerStatut ?? "").Trim(), CandidatStatut, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return IsCandidatLifeEventType(eventType);
	}

	/// <summary>Type « CANDIDAT ACTIF &gt; … » ou « … &gt; CANDIDAT ACTIF ».</summary>
	public static bool IsCandidatLifeEventType(string eventType)
	{
		if (string.IsNullOrWhiteSpace(eventType))
		{
			return false;
		}
		string value = eventType.Trim().ToUpperInvariant();
		return value.StartsWith("CANDIDAT ", StringComparison.Ordinal)
			|| value.Contains("> CANDIDAT", StringComparison.Ordinal);
	}

	private static bool IsOnOrAfterDay(DateTime value, DateTime today)
	{
		DateTime day = DateTime.SpecifyKind(value, DateTimeKind.Unspecified).Date;
		DateTime start = DateTime.SpecifyKind(today, DateTimeKind.Unspecified).Date;
		return day >= start;
	}
}
