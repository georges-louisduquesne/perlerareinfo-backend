using System.Linq;
using ApiPerleRare.Controllers;
using ApiPerleRare.Models;

namespace ApiPerleRare.Application.Tasks;

internal static class TachesQuery
{
	public static IQueryable<Taches> ApplyProspectOption(IQueryable<Taches> query, string option, string userLogin, bool applyQuiFilter)
	{
		if (option == "prospects")
		{
			if (applyQuiFilter && !string.IsNullOrEmpty(userLogin))
			{
				query = query.Where((Taches t) => t.TQui == userLogin);
			}
			query = query.Where((Taches t) => t.TRefContactNavigation.CStatut == "PROSPECT ACTIF" || t.TRefContactNavigation.CStatut == "PROSPECT MORT");
		}
		return query;
	}

	/// <summary>
	/// Same scalar + contact navigation fields as the legacy list (JSON contract).
	/// Callers filter on <see cref="Taches"/> first so prospect options stay sargable.
	/// </summary>
	public static IQueryable<TachesEx> ProjectEx(IQueryable<Taches> query)
	{
		return query.Select((Taches t) => new TachesEx
		{
			TRefAnnonce = t.TRefAnnonce,
			TCom = t.TCom,
			TDateCreation = t.TDateCreation,
			TDateRealisation = t.TDateRealisation,
			TEtat = t.TEtat,
			TLien = t.TLien,
			TPropertyId = t.TPropertyId,
			TQui = t.TQui,
			TRef = t.TRef,
			TRefContact = t.TRefContact,
			TRefContactNavigation = t.TRefContactNavigation,
			TType = t.TType,
			CNegociateur = t.TRefContactNavigation.CNegociateur
		});
	}
}
