using System.Collections.Generic;
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

public sealed class ListEvenementsExUseCase : IListEvenementsExUseCase
{
	private readonly IApplicationDbContext _context;

	private readonly IMemoryCache _cache;

	public ListEvenementsExUseCase(IApplicationDbContext context, IMemoryCache memoryCache)
	{
		_context = context;
		_cache = memoryCache;
	}

	public async Task<List<EvenementsEx>> Execute(EntityQuery query, string option, string userLogin, bool applyNegociateurFilter)
	{
		query ??= new EntityQuery();
		IQueryable<EvenementsEx> evenements = _context.Evenements.Select((Evenements e) => new EvenementsEx
		{
			ERefAnnAgc = e.ERefAnnAgc,
			ECr = e.ECr,
			EDate = e.EDate,
			EMail = e.EMail,
			ENomContact = e.ENomContact,
			EPropertyId = e.EPropertyId,
			ERefBien = e.ERefBien,
			ERefConseiller = e.ERefConseiller,
			ERefContact = e.ERefContact,
			ERefCtcInter = e.ERefCtcInter,
			ERefEvenement = e.ERefEvenement,
			ERefInterD = e.ERefInterD,
			ERefInterI = e.ERefInterI,
			EStatut = e.EStatut,
			ETexte = e.ETexte,
			ETypeEvenement = e.ETypeEvenement,
			EConseiller_Nom = ((e.ERefConseillerNavigation != null) ? e.ERefConseillerNavigation.CpNomFamille : null),
			EConseiller_PS = ((e.ERefConseillerNavigation != null) ? e.ERefConseillerNavigation.CpPhotoSignature : null)
		}).AsNoTracking();
		evenements = await ApplyDefaultFilter(evenements, option, userLogin, applyNegociateurFilter);
		evenements = EFHelper<EvenementsEx>.Apply(evenements, query.Where, query.OrderBy, query.Take, query.Skip, query.Select);
		return await evenements.ToListAsync();
	}

	private async Task<IQueryable<T>> ApplyDefaultFilter<T>(IQueryable<T> query, string option, string userLogin, bool applyNegociateurFilter) where T : Evenements
	{
		if (option == "prospects")
		{
			if (applyNegociateurFilter && !string.IsNullOrEmpty(userLogin))
			{
				query = query.Where((T e) =>
					e.ERefContactNavigation.CNegociateur == userLogin
					|| e.ERefContactNavigation.C2emeNegociateur == userLogin
					|| e.ERefContactNavigation.CNomFamilleConseiller == userLogin
					|| e.ERefContactNavigation.C2emeConseiller == userLogin
					|| e.ERefContactNavigation.CApporteur == userLogin
					|| e.ERefContactNavigation.C2emeApporteur == userLogin);
			}
			query = query.Where((T e) => e.ERefContactNavigation.CStatut == "PROSPECT ACTIF" || e.ERefContactNavigation.CStatut == "PROSPECT MORT");
			string[] prospectionTypes = await EventTypeCache.GetProspectionTypes(_cache, _context);
			if (prospectionTypes.Length != 0)
			{
				query = query.Where((T e) => prospectionTypes.Contains(e.ETypeEvenement));
			}
		}
		return query;
	}
}
