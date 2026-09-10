using System;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ApiPerleRare.Application.Abstractions;

namespace ApiPerleRare.Application.Events;

internal static class EventTypeCache
{
	public static Task<string[]> GetTypes(IMemoryCache cache, IApplicationDbContext context, HomeEventKind kind)
	{
		return cache.GetOrCreateAsync(kind.ToString(), delegate(ICacheEntry ce)
		{
			ce.SetAbsoluteExpiration(TimeSpan.FromHours(1.0));
			IQueryable<TypesEvenements> typesEvenements = context.TypesEvenements;
			return (kind switch
			{
				HomeEventKind.Clients => typesEvenements.Where((TypesEvenements te) => te.TeGenreEvenement == "MISSION" || te.TeGenreEvenement == "TRANSACTION"),
				HomeEventKind.RdvClient => typesEvenements.Where((TypesEvenements te) => te.TeGenreEvenement == "MISSION"),
				HomeEventKind.Transactions => typesEvenements.Where((TypesEvenements te) => te.TeGenreEvenement == "TRANSACTION" && te.TeCategorieEvenement != "OFFRE" && te.TeCategorieEvenement != "REP. OFFRE"),
				HomeEventKind.Agence => typesEvenements.Where((TypesEvenements te) => te.TeCategorieEvenement == "AGENCE"),
				_ => throw new NotImplementedException(),
			}).Select((TypesEvenements te) => te.TeTypeEvenement).ToArrayAsync();
		});
	}

	public static Task<string[]> GetProspectionTypes(IMemoryCache cache, IApplicationDbContext context)
	{
		return cache.GetOrCreateAsync("EventType.Prospection", delegate(ICacheEntry entry)
		{
			entry.SetAbsoluteExpiration(TimeSpan.FromHours(1.0));
			return context.TypesEvenements
				.Where((TypesEvenements te) => te.TeGenreEvenement == "PROSPECTION")
				.Select((TypesEvenements te) => te.TeTypeEvenement)
				.ToArrayAsync();
		});
	}

	public static Task<string[]> GetClientLastTypes(IMemoryCache cache, IApplicationDbContext context)
	{
		return cache.GetOrCreateAsync("EventType.ClientLast", delegate(ICacheEntry entry)
		{
			entry.SetAbsoluteExpiration(TimeSpan.FromHours(1.0));
			return context.TypesEvenements
				.Where((TypesEvenements te) => te.TeRefTypeEvenement == 5 || te.TeRefTypeEvenement == 6)
				.Select((TypesEvenements te) => te.TeTypeEvenement)
				.ToArrayAsync();
		});
	}
}
