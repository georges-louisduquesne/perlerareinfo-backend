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
		DateTime dueBefore = AccueilTacheWindow.DueBefore(DateTime.Now);
		IQueryable<Taches> taches = HomeAccueilTachesQueries.ClientAlertesAccueil(
			_context.Taches.AsNoTracking(),
			_context.ContactsRecherche.AsNoTracking(),
			dueBefore,
			userLogin,
			applyQuiFilter);
		IQueryable<ProspectTaches> tachesQuery = taches.Select((Taches t) => new ProspectTaches
		{
			TRef = t.TRef,
			TDateCreation = t.TDateCreation,
			TDateRealisation = t.TDateRealisation,
			CNomFamille = t.TRefContactNavigation.CNomFamille,
			TType = t.TType,
			TCom = t.TCom,
			TAppointmentId = t.TAppointmentId,
			TPropertyId = t.TPropertyId,
			TPcId = t.TPcId,
			TRefContact = t.TRefContact,
			TQui = t.TQui,
			CNegociateur = t.TRefContactNavigation.CNegociateur
		});
		return _enricher.CompleteTachesExes(await EFHelper<ProspectTaches>.Select(tachesQuery, query.Where, query.OrderBy, query.Take, query.Skip, query.Select));
	}
}
