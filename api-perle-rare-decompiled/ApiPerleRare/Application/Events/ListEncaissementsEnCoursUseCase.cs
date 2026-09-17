using System;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using ApiPerleRare.Application.Abstractions;

namespace ApiPerleRare.Application.Events;

/// <summary>
/// Home tab « transactions réalisées en attente d'encaissement ».
/// Latest past <c>RV ACTE AUTHENT.</c> for an active client with honoraires
/// (see <see cref="EncaissementsEnCoursFilter"/>). Compromis stay on tab 7.
/// </summary>
public sealed class ListEncaissementsEnCoursUseCase : IListEncaissementsEnCoursUseCase
{
	private readonly IApplicationDbContext _context;

	public ListEncaissementsEnCoursUseCase(IApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<SelectResult<ClientEvenements>> Execute(EntityQuery query, string userLogin, bool applyNegociateurFilter)
	{
		query ??= new EntityQuery();
		IQueryable<Evenements> evenements = _context.Evenements.AsNoTracking().Where((Evenements e) => e.EStatut == 1);
		DateTime today = DateTime.Today;
		evenements = evenements.Where((Evenements e) =>
			e.EDate <= today && e.ETypeEvenement == EncaissementsEnCoursFilter.RealizedEventType);
		// PHP Accueil: contacts_recherche.C_Statut like 'CLIENT ACTIF' (no wildcards).
		evenements = evenements.Where((Evenements e) =>
			EF.Functions.Like(e.ERefContactNavigation.CStatut, EncaissementsEnCoursFilter.ActiveClientStatus)
			&& e.ERefContactNavigation.CMttHono != null
			&& e.ERefContactNavigation.CMttHono != 0m);
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
		int[] latestIds = (await evenements
			.Select(e => new { e.ERefContact, e.EDate, e.ERefEvenement })
			.ToListAsync())
			.GroupBy(e => e.ERefContact)
			.Select(g => g.OrderByDescending(e => e.EDate).ThenByDescending(e => e.ERefEvenement).First().ERefEvenement)
			.ToArray();
		if (latestIds.Length == 0)
		{
			return new SelectResult<ClientEvenements>
			{
				Total = 0,
				Items = Array.Empty<ClientEvenements>()
			};
		}
		evenements = evenements.Where((Evenements e) => latestIds.Contains(e.ERefEvenement));
		IQueryable<ClientEvenements> projection = evenements.Select((Evenements e) => new ClientEvenements
		{
			ERefEvenement = e.ERefEvenement,
			EDate = e.EDate,
			EDateCreation = e.EDateCreation,
			CStatut = e.ERefContactNavigation.CStatut,
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
}
