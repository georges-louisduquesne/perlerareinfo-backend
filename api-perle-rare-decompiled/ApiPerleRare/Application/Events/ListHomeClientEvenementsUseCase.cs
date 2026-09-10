using System;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ApiPerleRare.Application.Abstractions;

namespace ApiPerleRare.Application.Events;

public sealed class ListHomeClientEvenementsUseCase : IListHomeClientEvenementsUseCase
{
	private readonly IApplicationDbContext _context;

	private readonly IMemoryCache _cache;

	public ListHomeClientEvenementsUseCase(IApplicationDbContext context, IMemoryCache memoryCache)
	{
		_context = context;
		_cache = memoryCache;
	}

	public async Task<SelectResult<ClientEvenements>> Execute(HomeEventKind kind, EntityQuery query, string userLogin, bool applyNegociateurFilter)
	{
		query ??= new EntityQuery();
		IQueryable<Evenements> evenements = _context.Evenements.AsNoTracking();
		evenements = evenements.Where((Evenements q) => q.EStatut == 1);
		string[] clientTypes = await GetEvenementTypes(kind);
		DateTime today = DateTime.Today;
		evenements = evenements.Where((Evenements e) => e.EDate >= today);
		if (clientTypes.Length != 0)
		{
			evenements = evenements.Where((Evenements e) => clientTypes.Contains(e.ETypeEvenement));
		}
		evenements = evenements.Where((Evenements e) => e.ERefContactNavigation.CStatut == "CLIENT ACTIF" || e.ERefContactNavigation.CStatut == "CLIENT MORT");
		if (applyNegociateurFilter && !string.IsNullOrEmpty(userLogin))
		{
			evenements = evenements.Where((Evenements e) =>
				e.ERefContactNavigation.CNegociateur == userLogin
				|| e.ERefContactNavigation.C2emeNegociateur == userLogin
				|| e.ERefContactNavigation.CNomFamilleConseiller == userLogin
				|| e.ERefContactNavigation.C2emeConseiller == userLogin
				|| e.ERefContactNavigation.CApporteur == userLogin
				|| e.ERefContactNavigation.C2emeApporteur == userLogin);
		}
		IQueryable<ClientEvenements> evQuery = evenements.Select((Evenements e) => new ClientEvenements
		{
			ERefEvenement = e.ERefEvenement,
			EDate = e.EDate,
			ETypeEvenement = e.ETypeEvenement,
			CNomFamille = e.ERefContactNavigation.CNomFamille,
			CNomFamilleConseiller = e.ERefContactNavigation.CNomFamilleConseiller,
			CMttHono = e.ERefContactNavigation.CMttHono,
			BRef = ((e.ERefBienNavigation != null) ? e.ERefBienNavigation.BRef : 0u),
			BCp = ((e.ERefBienNavigation != null) ? e.ERefBienNavigation.BCp : null),
			BAdresse = ((e.ERefBienNavigation != null) ? e.ERefBienNavigation.BAdresse : null),
			ETexte = e.ETexte,
			ERefContact = e.ERefContact,
			EConseiller_PS = ((e.ERefConseillerNavigation != null) ? e.ERefConseillerNavigation.CpPhotoSignature : null)
		});
		return await EFHelper<ClientEvenements>.Select(evQuery, query.Where, query.OrderBy, query.Take, query.Skip, query.Select);
	}

	private Task<string[]> GetEvenementTypes(HomeEventKind kind)
	{
		return EventTypeCache.GetTypes(_cache, _context, kind);
	}
}
