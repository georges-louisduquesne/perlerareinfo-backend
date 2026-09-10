using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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

public sealed class ListOffresEnCoursUseCase : IListOffresEnCoursUseCase
{
	private class OffreInfo
	{
		public string ETypeEvenement { get; set; }

		public uint ERefBien { get; set; }

		public uint ERefContact { get; set; }

		public uint ERefConseiller { get; set; }

		public int MaxId { get; set; }

		public DateTime LastDate { get; set; }
	}

	private class OffreRes
	{
		public readonly uint E_RefBien;

		public readonly uint E_RefContact;

		public readonly uint E_RefConseiller;

		private readonly OffreInfo _offre;

		private readonly OffreInfo _acceptee;

		private readonly OffreInfo _refus;

		private readonly OffreInfo _good;

		private readonly bool _hasAgence;

		public int EvenementId => _good?.MaxId ?? 0;

		public bool IsGood => _good != null;

		public OffreRes(uint e_RefBien, uint e_RefContact, uint e_RefConseiller, OffreInfo[] offreInfos)
		{
			E_RefBien = e_RefBien;
			E_RefContact = e_RefContact;
			E_RefConseiller = e_RefConseiller;
			_offre = offreInfos.SingleOrDefault((OffreInfo o) => o.ETypeEvenement == "OFFRE");
			_acceptee = offreInfos.SingleOrDefault((OffreInfo o) => o.ETypeEvenement == "OFFRE ACCEPTEE");
			_refus = offreInfos.SingleOrDefault((OffreInfo o) => o.ETypeEvenement == "OFFRE REFUSEE");
			_hasAgence = offreInfos.Any((OffreInfo o) => o != _offre && o != _acceptee && o != _refus);
			_good = GetGoodOffre();
		}

		private OffreInfo GetGoodOffre()
		{
			if (_offre != null && (_refus == null || _refus.LastDate < _offre.LastDate) && (_acceptee == null || _acceptee.LastDate < _offre.LastDate))
			{
				return _offre;
			}
			if (_acceptee != null && (_refus == null || _refus.LastDate < _acceptee.LastDate) && !_hasAgence)
			{
				return _acceptee;
			}
			if (_refus != null && _refus.LastDate == DateTime.Today)
			{
				return _refus;
			}
			return null;
		}
	}

	private readonly IApplicationDbContext _context;

	private readonly IMemoryCache _cache;

	public ListOffresEnCoursUseCase(IApplicationDbContext context, IMemoryCache memoryCache)
	{
		_context = context;
		_cache = memoryCache;
	}

	public async Task<SelectResult<ClientEvenements>> Execute(EntityQuery query, uint eRefConseiller, string userLogin, bool applyNegociateurFilter)
	{
		query ??= new EntityQuery();
		List<string> types = (await EventTypeCache.GetTypes(_cache, _context, HomeEventKind.Agence)).ToList();
		types.AddRange(new string[3] { "OFFRE", "OFFRE REFUSEE", "OFFRE ACCEPTEE" });
		string conseillerWhere = ((eRefConseiller != 0) ? $"AND E_RefConseiller={eRefConseiller}" : "");
		string clientActif = "AND c.C_Statut='CLIENT ACTIF'";
		string sql = $"\r\nSELECT E_TypeEvenement, E_RefBien, E_RefContact, MAX(E_Date) AS Lastdate, MAX(E_RefEvenement) AS MaxId, E_RefConseiller\r\nFROM evenements e INNER JOIN contacts_recherche c ON e.E_RefContact=c.C_RefContact\r\nWHERE E_TypeEvenement IN ({string.Join(", ", types.Select((string t) => "'" + t + "'"))}) AND E_Statut=1 AND \r\n    E_RefBien IS NOT null {clientActif}  {conseillerWhere} \r\nGROUP BY E_TypeEvenement, E_RefBien, E_RefContact, E_RefConseiller";
		DbConnection c = _context.Database.GetDbConnection();
		if (c.State != ConnectionState.Open)
		{
			await c.OpenAsync();
		}
		OffreInfo[] infos;
		using (DbCommand cmd = c.CreateCommand())
		{
			cmd.CommandText = sql;
			using DbDataReader reader = cmd.ExecuteReader();
			infos = reader.ToModels<OffreInfo>();
		}
		int[] evenementIds = (from i in infos
			group i by new { i.ERefBien, i.ERefContact, i.ERefConseiller } into g
			select new OffreRes(g.Key.ERefBien, g.Key.ERefContact, g.Key.ERefConseiller, g.ToArray()) into r
			where r.IsGood
			select r.EvenementId).ToArray();
		if (evenementIds.Length == 0)
		{
			SelectResult<ClientEvenements> selectResult = new SelectResult<ClientEvenements>();
			selectResult.Total = 0;
			selectResult.Items = new ClientEvenements[0];
			return selectResult;
		}
		IQueryable<Evenements> evenements = _context.Evenements;
		evenements = evenements.Where((Evenements e) => evenementIds.Contains(e.ERefEvenement));
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
			BRef = e.ERefBienNavigation.BRef,
			BCp = e.ERefBienNavigation.BCp,
			BAdresse = e.ERefBienNavigation.BAdresse,
			ETexte = e.ETexte,
			ERefContact = e.ERefContact
		});
		return await EFHelper<ClientEvenements>.Select(evQuery, query.Where, query.OrderBy, query.Take, query.Skip, query.Select);
	}
}
