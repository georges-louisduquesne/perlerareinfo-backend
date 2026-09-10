using System.Collections.Generic;
using System.Linq;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using ApiPerleRare.Application.Abstractions;

namespace ApiPerleRare.Application.Tasks;

public sealed class ListTachesExUseCase : IListTachesExUseCase
{
	private readonly IApplicationDbContext _context;

	private readonly TachesExEnricher _enricher;

	public ListTachesExUseCase(IApplicationDbContext context, TachesExEnricher enricher)
	{
		_context = context;
		_enricher = enricher;
	}

	public async System.Threading.Tasks.Task<List<TachesEx>> Execute(EntityQuery query, string option, string userLogin, bool applyQuiFilter)
	{
		query ??= new EntityQuery();
		IQueryable<TachesEx> taches = GetTachesExQuery();
		taches = ApplyDefaultFilter(taches, option, userLogin, applyQuiFilter);
		taches = EFHelper<TachesEx>.Apply(taches, query.Where, query.OrderBy, query.Take, query.Skip, query.Select);
		List<TachesEx> list = await taches.ToListAsync();
		list.FixEncoding();
		return _enricher.CompleteTachesExes(list);
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
