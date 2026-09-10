using System;
using System.Linq;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using ApiPerleRare.Application.Abstractions;

namespace ApiPerleRare.Application.Tasks;

public sealed class ListClientTachesUseCase : IListClientTachesUseCase
{
	private readonly IApplicationDbContext _context;

	private readonly TachesExEnricher _enricher;

	public ListClientTachesUseCase(IApplicationDbContext context, TachesExEnricher enricher)
	{
		_context = context;
		_enricher = enricher;
	}

	public async System.Threading.Tasks.Task<SelectResult<ProspectTaches>> Execute(EntityQuery query, string userLogin, bool applyQuiFilter)
	{
		query ??= new EntityQuery();
		IQueryable<Taches> taches = _context.Taches;
		taches = taches.Where((Taches t) => t.TEtat == "");
		DateTime today = DateTime.Today;
		taches = taches.Where((Taches t) => t.TDateRealisation <= today);
		taches = taches.Where((Taches t) => t.TRefContactNavigation.CStatut == "CLIENT ACTIF" || t.TRefContactNavigation.CStatut == "CLIENT MORT");
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
