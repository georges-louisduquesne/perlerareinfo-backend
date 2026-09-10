using System;
using System.Linq;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using ApiPerleRare.Application.Abstractions;

namespace ApiPerleRare.Application.Tasks;

public sealed class ListProspectTachesUseCase : IListProspectTachesUseCase
{
	private readonly IApplicationDbContext _context;

	private readonly TachesExEnricher _enricher;

	public ListProspectTachesUseCase(IApplicationDbContext context, TachesExEnricher enricher)
	{
		_context = context;
		_enricher = enricher;
	}

	public async System.Threading.Tasks.Task<SelectResult<ProspectTaches>> Execute(EntityQuery query, string userLogin, bool applyQuiFilter)
	{
		query ??= new EntityQuery();
		IQueryable<Taches> taches = _context.Taches.AsNoTracking();
		taches = taches.Where((Taches t) => t.TEtat == "");
		DateTime tomorrow = DateTime.Today.AddDays(1.0);
		taches = taches.Where((Taches t) => t.TDateRealisation < tomorrow);
		taches = taches.Where((Taches t) => t.TRefContactNavigation.CStatut == "PROSPECT ACTIF" || t.TRefContactNavigation.CStatut == "PROSPECT MORT");
		if (applyQuiFilter && !string.IsNullOrEmpty(userLogin))
		{
			taches = taches.Where((Taches t) => t.TQui == userLogin);
		}
		IQueryable<ProspectTaches> tachesQuery = taches.Select((Taches t) => new ProspectTaches
		{
			TRef = t.TRef,
			TDateRealisation = t.TDateRealisation,
			CNomFamille = t.TRefContactNavigation.CNomFamille,
			TType = t.TType,
			TCom = t.TCom,
			TRefContact = t.TRefContact,
			TQui = t.TQui,
			CNegociateur = t.TRefContactNavigation.CNegociateur
		});
		return _enricher.CompleteTachesExes(await EFHelper<ProspectTaches>.Select(tachesQuery, query.Where, query.OrderBy, query.Take, query.Skip, query.Select));
	}
}
