using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ApiPerleRare.Application.Abstractions;

namespace ApiPerleRare.Application.Events;

public sealed class CountEvenementsUseCase : ICountEvenementsUseCase
{
	private readonly IApplicationDbContext _context;

	private readonly IMemoryCache _cache;

	public CountEvenementsUseCase(IApplicationDbContext context, IMemoryCache memoryCache)
	{
		_context = context;
		_cache = memoryCache;
	}

	public async Task<int> Execute(string where, string option, string userLogin, bool applyNegociateurFilter)
	{
		IQueryable<Evenements> evenements = _context.Evenements.AsNoTracking();
		evenements = await ApplyDefaultFilter(evenements, option, userLogin, applyNegociateurFilter);
		evenements = EFHelper<Evenements>.Apply(evenements, where);
		return await evenements.CountAsync();
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
