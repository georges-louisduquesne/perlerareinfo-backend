using System;
using System.Linq;
using ApiPerleRare.Models;

namespace ApiPerleRare.Application.Tasks;

/// <summary>
/// Listes alertes accueil (onglets 2 prospect / 3 client) : jointure explicite contacts + fenêtre échéance.
/// </summary>
public static class HomeAccueilTachesQueries
{
	public const string ProspectActif = "PROSPECT ACTIF";

	public const string ProspectMort = "PROSPECT MORT";

	public const string ClientActif = "CLIENT ACTIF";

	public const string ClientMort = "CLIENT MORT";

	public static IQueryable<Taches> OpenDueBefore(
		IQueryable<Taches> taches,
		DateTime dueBefore,
		string userLogin,
		bool applyQuiFilter)
	{
		IQueryable<Taches> query = taches.Where((Taches t) => t.TEtat == "" && t.TDateRealisation < dueBefore);
		if (applyQuiFilter && !string.IsNullOrEmpty(userLogin))
		{
			query = query.Where((Taches t) => t.TQui == userLogin);
		}

		return query;
	}

	public static IQueryable<Taches> ProspectAlertesAccueil(
		IQueryable<Taches> taches,
		IQueryable<ContactsRecherche> contacts,
		DateTime dueBefore,
		string userLogin,
		bool applyQuiFilter)
	{
		IQueryable<Taches> open = OpenDueBefore(taches, dueBefore, userLogin, applyQuiFilter);
		return from t in open
			join c in contacts on t.TRefContact equals c.CRefContact
			where c.CStatut == ProspectActif || c.CStatut == ProspectMort
			select t;
	}

	public static IQueryable<Taches> ClientAlertesAccueil(
		IQueryable<Taches> taches,
		IQueryable<ContactsRecherche> contacts,
		DateTime dueBefore,
		string userLogin,
		bool applyQuiFilter)
	{
		IQueryable<Taches> open = OpenDueBefore(taches, dueBefore, userLogin, applyQuiFilter);
		return from t in open
			join c in contacts on t.TRefContact equals c.CRefContact
			where c.CStatut == ClientActif || c.CStatut == ClientMort
			select t;
	}
}
