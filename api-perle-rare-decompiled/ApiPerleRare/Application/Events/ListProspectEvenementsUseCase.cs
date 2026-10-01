using System;
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
using ApiPerleRare;

namespace ApiPerleRare.Application.Events;

public sealed class ListProspectEvenementsUseCase : IListProspectEvenementsUseCase
{
	private readonly IApplicationDbContext _context;

	private readonly IMemoryCache _cache;

	private readonly IUserService _users;

	public ListProspectEvenementsUseCase(IApplicationDbContext context, IMemoryCache memoryCache, IUserService users)
	{
		_context = context;
		_cache = memoryCache;
		_users = users;
	}

	public async Task<SelectResult<ProspectEvenements>> Execute(EntityQuery query, string userLogin, bool applyNegociateurFilter)
	{
		query ??= new EntityQuery();
		IQueryable<Evenements> evenements = _context.Evenements.AsNoTracking();
		evenements = evenements.Where((Evenements q) => q.EStatut == 1);
		string[] prospectionTypes = await EventTypeCache.GetProspectionTypes(_cache, _context);
		if (prospectionTypes.Length != 0)
		{
			evenements = evenements.Where((Evenements e) => prospectionTypes.Contains(e.ETypeEvenement));
		}
		DateTime today = DateTime.Today;
		evenements = evenements.Where((Evenements e) => e.EDate >= today);
		evenements = evenements.Where((Evenements e) => e.ERefContactNavigation.CStatut == "PROSPECT ACTIF" || e.ERefContactNavigation.CStatut == "PROSPECT MORT");
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
		IQueryable<ProspectEvenements> evQuery = evenements.Select((Evenements e) => new ProspectEvenements
		{
			ERefEvenement = e.ERefEvenement,
			EDate = e.EDate,
			EDateCreation = e.EDateCreation,
			CStatut = e.ERefContactNavigation.CStatut,
			ETypeEvenement = e.ETypeEvenement,
			CNomFamille = e.ERefContactNavigation.CNomFamille,
			ETexte = e.ETexte,
			ENomContact = e.ENomContact,
			ERefContact = e.ERefContact,
			CNegociateur = e.ERefContactNavigation.CNegociateur,
			EConseiller_PS = ((e.ERefConseillerNavigation != null) ? e.ERefConseillerNavigation.CpPhotoSignature : null),
			EAppointmentId = e.EAppointmentId
		});
		SelectResult<ProspectEvenements> result = await EFHelper<ProspectEvenements>.Select(evQuery, query.Where, query.OrderBy, query.Take, query.Skip, query.Select);
		AttachNegociateurPhotos(result.Items);
		return result;
	}

	private void AttachNegociateurPhotos(ProspectEvenements[] rows)
	{
		if (rows == null || rows.Length == 0)
		{
			return;
		}
		HashSet<string> logins = new HashSet<string>(StringComparer.Ordinal);
		foreach (ProspectEvenements row in rows)
		{
			if (!string.IsNullOrEmpty(row.CNegociateur))
			{
				logins.Add(row.CNegociateur);
			}
		}
		_users.WarmConseillers(logins);
		foreach (ProspectEvenements row in rows)
		{
			if (string.IsNullOrEmpty(row.CNegociateur))
			{
				continue;
			}
			row.CNegociateur_PS = _users.GetConseiller(row.CNegociateur)?.CpPhotoSignature;
		}
	}
}
