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

/// <summary>
/// Mirrors live <c>GetEncaissementsEnCoursEvenements</c>: TRANSACTION types (hors offres),
/// event already realized, contact still without invoice numbers.
/// </summary>
public sealed class ListEncaissementsEnCoursUseCase : IListEncaissementsEnCoursUseCase
{
	private readonly IApplicationDbContext _context;

	private readonly IMemoryCache _cache;

	public ListEncaissementsEnCoursUseCase(IApplicationDbContext context, IMemoryCache memoryCache)
	{
		_context = context;
		_cache = memoryCache;
	}

	public async Task<SelectResult<ClientEvenements>> Execute(EntityQuery query, string userLogin, bool applyNegociateurFilter)
	{
		query ??= new EntityQuery();
		IQueryable<Evenements> evenements = _context.Evenements.Where((Evenements e) => e.EStatut == 1);
		string[] types = await GetTransactionTypesExcludingOffres();
		DateTime today = DateTime.Today;
		evenements = evenements.Where((Evenements e) => e.EDate <= today);
		if (types.Length != 0)
		{
			evenements = evenements.Where((Evenements e) => types.Contains(e.ETypeEvenement));
		}
		evenements = evenements.Where((Evenements e) =>
			(e.ERefContactNavigation.CStatut == "CLIENT ACTIF" || e.ERefContactNavigation.CStatut == "CLIENT MORT")
			&& e.ERefContactNavigation.CFactureHon == null
			&& e.ERefContactNavigation.CFacturePs == null);
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
		IQueryable<ClientEvenements> projection = evenements.Select((Evenements e) => new ClientEvenements
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
		return await EFHelper<ClientEvenements>.Select(projection, query.Where, query.OrderBy, query.Take, query.Skip, query.Select);
	}

	private Task<string[]> GetTransactionTypesExcludingOffres()
	{
		return _cache.GetOrCreateAsync("EventType.Encaissements", delegate(ICacheEntry entry)
		{
			entry.SetAbsoluteExpiration(TimeSpan.FromHours(1.0));
			return _context.TypesEvenements
				.Where((TypesEvenements te) =>
					te.TeGenreEvenement == "TRANSACTION"
					&& te.TeCategorieEvenement != "OFFRE"
					&& te.TeCategorieEvenement != "REP. OFFRE")
				.Select((TypesEvenements te) => te.TeTypeEvenement)
				.ToArrayAsync();
		});
	}
}
