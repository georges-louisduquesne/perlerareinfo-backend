using System.Collections.Generic;
using System.Linq;
using ApiPerleRare.Application.Abstractions;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;

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
		IQueryable<Taches> root = TachesQuery.ApplyProspectOption(_context.Taches.AsNoTracking(), option, userLogin, applyQuiFilter);
		IQueryable<TachesEx> taches = TachesQuery.ProjectEx(root);
		taches = EFHelper<TachesEx>.Apply(taches, query.Where, query.OrderBy, query.Take, query.Skip, query.Select);
		List<TachesEx> list = await taches.ToListAsync();
		list.FixEncoding();
		return _enricher.CompleteTachesExes(list);
	}
}
