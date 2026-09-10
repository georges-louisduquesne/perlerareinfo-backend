using System.Linq;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using ApiPerleRare.Application.Abstractions;

namespace ApiPerleRare.Application.Tasks;

public sealed class CountTachesUseCase : ICountTachesUseCase
{
	private readonly IApplicationDbContext _context;

	public CountTachesUseCase(IApplicationDbContext context)
	{
		_context = context;
	}

	public async System.Threading.Tasks.Task<int> Execute(string where, string option, string userLogin, bool applyQuiFilter)
	{
		IQueryable<TachesEx> taches = GetTachesExQuery();
		taches = ApplyDefaultFilter(taches, option, userLogin, applyQuiFilter);
		taches = EFHelper<TachesEx>.Apply(taches, where);
		return await taches.CountAsync();
	}

	private IQueryable<TachesEx> GetTachesExQuery()
	{
		return _context.Taches.Select((Taches t) => new TachesEx
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

	private static IQueryable<TachesEx> ApplyDefaultFilter(IQueryable<TachesEx> query, string option, string userLogin, bool applyQuiFilter)
	{
		if (option == "prospects")
		{
			if (applyQuiFilter && !string.IsNullOrEmpty(userLogin))
			{
				query = query.Where((TachesEx t) => t.TQui == userLogin);
			}
			query = query.Where((TachesEx t) => t.TRefContactNavigation.CStatut == "PROSPECT ACTIF" || t.TRefContactNavigation.CStatut == "PROSPECT MORT");
		}
		return query;
	}
}
