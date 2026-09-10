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

public sealed class ListClientLastEvenementsUseCase : IListClientLastEvenementsUseCase
{
	private readonly IApplicationDbContext _context;

	private readonly IMemoryCache _cache;

	public ListClientLastEvenementsUseCase(IApplicationDbContext context, IMemoryCache memoryCache)
	{
		_context = context;
		_cache = memoryCache;
	}

	public async Task<SelectResult<ClientLastEvenements>> Execute(EntityQuery query, string userLogin, bool applyNegociateurFilter)
	{
		query ??= new EntityQuery();
		IQueryable<Evenements> evenements = _context.Evenements.AsNoTracking();
		evenements = evenements.Where((Evenements q) => q.EStatut == 1);
		DateTime today = DateTime.Today;
		evenements = evenements.Where((Evenements e) => e.EDate < today);
		string[] clientLastTypes = await EventTypeCache.GetClientLastTypes(_cache, _context);
		if (clientLastTypes.Length != 0)
		{
			evenements = evenements.Where((Evenements e) => clientLastTypes.Contains(e.ETypeEvenement));
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
		IQueryable<ClientLastEvenements> evQuery = evenements.Select((Evenements e) => new ClientLastEvenements
		{
			ERefEvenement = e.ERefEvenement,
			EDate = e.EDate,
			ETypeEvenement = e.ETypeEvenement,
			CNomFamille = e.ERefContactNavigation.CNomFamille,
			CNomFamilleConseiller = e.ERefContactNavigation.CNomFamilleConseiller,
			BCp = e.ERefBienNavigation.BCp,
			BAdresse = e.ERefBienNavigation.BAdresse,
			ETexte = e.ETexte,
			ERefContact = e.ERefContact,
			CIRef = e.ERefCtcInter,
			CIPrenom = e.ERefCtcInterNavigation.CPrenom,
			CINom = e.ERefCtcInterNavigation.CNom,
			INomIntermediaire = e.ERefInterDNavigation.INomIntermediaire,
			EConseiller_PS = ((e.ERefConseillerNavigation != null) ? e.ERefConseillerNavigation.CpPhotoSignature : null)
		});
		return await EFHelper<ClientLastEvenements>.Select(evQuery, query.Where, query.OrderBy, query.Take, query.Skip, query.Select);
	}
}
