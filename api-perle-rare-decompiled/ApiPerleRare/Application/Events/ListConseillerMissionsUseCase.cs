using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Application.Abstractions;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Application.Events;

public sealed class ListConseillerMissionsUseCase : IListConseillerMissionsUseCase
{
	private readonly IApplicationDbContext _context;

	public ListConseillerMissionsUseCase(IApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<List<ConseillerMissionRow>> Execute(uint conseillerId, CancellationToken cancellationToken = default)
	{
		if (conseillerId == 0)
		{
			return new List<ConseillerMissionRow>();
		}
		string login = await _context.ConseillersPersonnels.AsNoTracking()
			.Where((ConseillersPersonnels c) => c.CpRefConseiller == conseillerId)
			.Select((ConseillersPersonnels c) => c.CpLogin)
			.FirstOrDefaultAsync(cancellationToken);
		if (string.IsNullOrWhiteSpace(login))
		{
			return new List<ConseillerMissionRow>();
		}
		login = login.Trim();
		string[] genres = ConseillerMissionParticipation.Genres;
		string[] types = await _context.TypesEvenements.AsNoTracking()
			.Where((TypesEvenements te) => genres.Contains(te.TeGenreEvenement))
			.Select((TypesEvenements te) => te.TeTypeEvenement)
			.ToArrayAsync(cancellationToken);
		if (types.Length == 0)
		{
			return new List<ConseillerMissionRow>();
		}
		var rows = await _context.Evenements.AsNoTracking()
			.Where((Evenements e) => e.EStatut == 1)
			.Where((Evenements e) => types.Contains(e.ETypeEvenement))
			.Where((Evenements e) =>
				e.ERefContactNavigation.CApporteur == login
				|| e.ERefContactNavigation.C2emeApporteur == login
				|| e.ERefContactNavigation.CNegociateur == login
				|| e.ERefContactNavigation.C2emeNegociateur == login
				|| e.ERefContactNavigation.CNomFamilleConseiller == login
				|| e.ERefContactNavigation.C2emeConseiller == login)
			.OrderByDescending((Evenements e) => e.EDate)
			.ThenByDescending((Evenements e) => e.ERefEvenement)
			.Select((Evenements e) => new
			{
				Id = e.ERefEvenement,
				Date = e.EDate,
				Type = e.ETypeEvenement,
				Texte = e.ETexte,
				ContactId = e.ERefContact,
				ContactNom = e.ERefContactNavigation.CNomFamille,
				ContactPrenom = e.ERefContactNavigation.CPrenom,
				NomContact = e.ENomContact,
				ContactStatut = e.ERefContactNavigation.CStatut,
				Cp = e.ERefBienNavigation != null ? e.ERefBienNavigation.BCp : null,
				Adresse = e.ERefBienNavigation != null ? e.ERefBienNavigation.BAdresse : null,
				Apporteur = e.ERefContactNavigation.CApporteur,
				Apporteur2 = e.ERefContactNavigation.C2emeApporteur,
				Negociateur = e.ERefContactNavigation.CNegociateur,
				Negociateur2 = e.ERefContactNavigation.C2emeNegociateur,
				Conseiller = e.ERefContactNavigation.CNomFamilleConseiller,
				Conseiller2 = e.ERefContactNavigation.C2emeConseiller,
			})
			.ToListAsync(cancellationToken);
		return rows.Select((row) => new ConseillerMissionRow
		{
			Id = row.Id,
			Date = row.Date,
			Type = row.Type ?? "",
			Texte = row.Texte ?? "",
			ContactId = row.ContactId,
			ContactNom = string.IsNullOrWhiteSpace(row.ContactNom) ? (row.NomContact ?? "") : row.ContactNom,
			ContactPrenom = row.ContactPrenom ?? "",
			ContactStatut = row.ContactStatut ?? "",
			Cp = row.Cp ?? "",
			Adresse = row.Adresse ?? "",
			Roles = ConseillerMissionParticipation.Roles(login, row.Apporteur, row.Apporteur2, row.Negociateur, row.Negociateur2, row.Conseiller, row.Conseiller2),
		}).ToList();
	}
}
