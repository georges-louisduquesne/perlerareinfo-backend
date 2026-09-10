using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Application.Events;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[EnableCors]
[ApiController]
[Authorize]
public class EvenementsController : ControllerBase
{
	private enum EventType
	{
		Clients,
		RdvClient,
		Transactions,
		Agence
	}

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

	private readonly IMemoryCache _cache;

	private readonly ApplicationDbContext _context;

	private readonly IUserSessionService _userSessionService;

	private readonly IListEncaissementsEnCoursUseCase _encaissementsEnCours;

	private static string[] _prospectionTypes;

	private static DateTime _prospectionTypesDate;

	private static string[] _clientLastTypes;

	private static DateTime _clientLastTypesDate;

	public EvenementsController(
		ApplicationDbContext context,
		IUserSessionService userSessionService,
		IMemoryCache memoryCache,
		IListEncaissementsEnCoursUseCase encaissementsEnCours)
	{
		_cache = memoryCache;
		_context = context;
		_userSessionService = userSessionService;
		_encaissementsEnCours = encaissementsEnCours;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<EvenementsEx>>> GetEvenements([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0, [FromQuery] string option = null)
	{
		try
		{
			IQueryable<EvenementsEx> query = _context.Evenements.Select((Evenements e) => new EvenementsEx
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
			query = ApplyDefaultFilter(query, option);
			query = EFHelper<EvenementsEx>.Apply(query, where, orderby, take, skip, select);
			return (await query.ToListAsync());
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[HttpGet]
	[Route("Contact")]
	public async Task<ActionResult<IEnumerable<ContactEvenements>>> GetContactEvenements([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0, [FromQuery] string option = null)
	{
		try
		{
			IQueryable<ContactEvenements> query = _context.ContactEvenements.AsNoTracking();
			query = EFHelper<ContactEvenements>.Apply(query, where, orderby, take, skip, select);
			return (await query.ToListAsync());
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[HttpGet]
	[Route("Count")]
	public async Task<ActionResult<int>> GetEvenementsCount([FromQuery] string where = null, [FromQuery] string option = null)
	{
		try
		{
			IQueryable<Evenements> query = _context.Evenements;
			query = ApplyDefaultFilter(query, option);
			query = EFHelper<Evenements>.Apply(query, where);
			return await query.CountAsync();
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[HttpGet]
	[Route("Clients")]
	public async Task<ActionResult<SelectResult<ClientEvenements>>> GetClientEvenements([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		return await GetEvenements(EventType.Clients, select, where, orderby, skip, take);
	}

	[HttpGet]
	[Route("RdvClients")]
	public async Task<ActionResult<SelectResult<ClientEvenements>>> GetRdvClientsEvenements([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		return await GetEvenements(EventType.RdvClient, select, where, orderby, skip, take);
	}

	[HttpGet]
	[Route("Transactions")]
	public async Task<ActionResult<SelectResult<ClientEvenements>>> GetTransactionsEvenements([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		return await GetEvenements(EventType.Transactions, select, where, orderby, skip, take);
	}

	[HttpGet]
	[Route("EncaissementsEnCours")]
	public async Task<ActionResult<SelectResult<ClientEvenements>>> GetEncaissementsEnCoursEvenements(
		[FromQuery] string select = null,
		[FromQuery] string where = null,
		[FromQuery] string orderby = null,
		[FromQuery] int skip = 0,
		[FromQuery] int take = 0)
	{
		try
		{
			return await _encaissementsEnCours.Execute(
				new EntityQuery
				{
					Select = select,
					Where = where,
					OrderBy = orderby,
					Skip = skip,
					Take = take
				},
				_userSessionService.Filter ? this.GetUserLogin() : null,
				_userSessionService.Filter);
		}
		catch (Exception ex)
		{
			return BadRequest(ex.ToString());
		}
	}

	private async Task<ActionResult<SelectResult<ClientEvenements>>> GetEvenements(EventType type, [FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		try
		{
			IQueryable<Evenements> query = _context.Evenements;
			query = query.Where((Evenements q) => q.EStatut == 1);
			string[] clientTypes = await GetEvenementTypes(type);
			DateTime today = DateTime.Today;
			query = query.Where((Evenements e) => e.EDate >= today);
			if (clientTypes.Length != 0)
			{
				query = query.Where((Evenements e) => clientTypes.Contains(e.ETypeEvenement));
			}
			query = query.Where((Evenements e) => e.ERefContactNavigation.CStatut == "CLIENT ACTIF" || e.ERefContactNavigation.CStatut == "CLIENT MORT");
			if (_userSessionService.Filter)
			{
				string userLogin = this.GetUserLogin();
				query = query.Where((Evenements e) => e.ERefContactNavigation.CNegociateur == userLogin || e.ERefContactNavigation.C2emeNegociateur == userLogin || e.ERefContactNavigation.CNomFamilleConseiller == userLogin || e.ERefContactNavigation.C2emeConseiller == userLogin || e.ERefContactNavigation.CApporteur == userLogin || e.ERefContactNavigation.C2emeApporteur == userLogin);
			}
			IQueryable<ClientEvenements> evQuery = query.Select((Evenements e) => new ClientEvenements
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
			return await EFHelper<ClientEvenements>.Select(evQuery, where, orderby, take, skip, select);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	private async Task<string[]> GetEvenementTypes(EventType type)
	{
		return await _cache.GetOrCreateAsync(type.ToString(), delegate(ICacheEntry ce)
		{
			ce.SetAbsoluteExpiration(TimeSpan.FromHours(1.0));
			IQueryable<TypesEvenements> typesEvenements = _context.TypesEvenements;
			return (type switch
			{
				EventType.Clients => typesEvenements.Where((TypesEvenements te) => te.TeGenreEvenement == "MISSION" || te.TeGenreEvenement == "TRANSACTION"), 
				EventType.RdvClient => typesEvenements.Where((TypesEvenements te) => te.TeGenreEvenement == "MISSION"), 
				EventType.Transactions => typesEvenements.Where((TypesEvenements te) => te.TeGenreEvenement == "TRANSACTION" && te.TeCategorieEvenement != "OFFRE" && te.TeCategorieEvenement != "REP. OFFRE"), 
				EventType.Agence => typesEvenements.Where((TypesEvenements te) => te.TeCategorieEvenement == "AGENCE"), 
				_ => throw new NotImplementedException(), 
			}).Select((TypesEvenements te) => te.TeTypeEvenement).ToArrayAsync();
		});
	}

	[HttpGet]
	[Route("Prospects")]
	public async Task<ActionResult<SelectResult<ProspectEvenements>>> GetProspectEvenements([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		try
		{
			IQueryable<Evenements> query = _context.Evenements;
			query = query.Where((Evenements q) => q.EStatut == 1);
			if (_prospectionTypes == null || _prospectionTypesDate < DateTime.Now.AddHours(-1.0))
			{
				_prospectionTypes = (from te in _context.TypesEvenements
					where te.TeGenreEvenement == "PROSPECTION"
					select te.TeTypeEvenement).ToArray();
				_prospectionTypesDate = DateTime.Now;
			}
			if (_prospectionTypes.Length != 0)
			{
				query = query.Where((Evenements e) => _prospectionTypes.Contains(e.ETypeEvenement));
			}
			DateTime today = DateTime.Today;
			query = query.Where((Evenements e) => e.EDate >= today);
			query = query.Where((Evenements e) => e.ERefContactNavigation.CStatut == "PROSPECT ACTIF" || e.ERefContactNavigation.CStatut == "PROSPECT MORT");
			if (_userSessionService.Filter)
			{
				string userLogin = this.GetUserLogin();
				query = query.Where((Evenements e) => e.ERefContactNavigation.CNegociateur == userLogin || e.ERefContactNavigation.C2emeNegociateur == userLogin || e.ERefContactNavigation.CNomFamilleConseiller == userLogin || e.ERefContactNavigation.C2emeConseiller == userLogin || e.ERefContactNavigation.CApporteur == userLogin || e.ERefContactNavigation.C2emeApporteur == userLogin);
			}
			IQueryable<ProspectEvenements> evQuery = query.Select((Evenements e) => new ProspectEvenements
			{
				ERefEvenement = e.ERefEvenement,
				EDate = e.EDate,
				ETypeEvenement = e.ETypeEvenement,
				CNomFamille = e.ERefContactNavigation.CNomFamille,
				ETexte = e.ETexte,
				ENomContact = e.ENomContact,
				ERefContact = e.ERefContact,
				CNegociateur = e.ERefContactNavigation.CNegociateur,
				EConseiller_PS = ((e.ERefConseillerNavigation != null) ? e.ERefConseillerNavigation.CpPhotoSignature : null)
			});
			return await EFHelper<ProspectEvenements>.Select(evQuery, where, orderby, take, skip, select);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[HttpGet]
	[Route("ClientsLast")]
	public async Task<ActionResult<SelectResult<ClientLastEvenements>>> GetClientLastEvenements([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		try
		{
			IQueryable<Evenements> query = _context.Evenements;
			query = query.Where((Evenements q) => q.EStatut == 1);
			DateTime today = DateTime.Today;
			query = query.Where((Evenements e) => e.EDate < today);
			if (_clientLastTypes == null || _clientLastTypesDate < DateTime.Now.AddHours(-1.0))
			{
				_clientLastTypes = (from te in _context.TypesEvenements
					where te.TeRefTypeEvenement == 5 || te.TeRefTypeEvenement == 6
					select te.TeTypeEvenement).ToArray();
				_clientLastTypesDate = DateTime.Now;
			}
			if (_clientLastTypes.Length != 0)
			{
				query = query.Where((Evenements e) => _clientLastTypes.Contains(e.ETypeEvenement));
			}
			query = query.Where((Evenements e) => e.ERefContactNavigation.CStatut == "CLIENT ACTIF" || e.ERefContactNavigation.CStatut == "CLIENT MORT");
			if (_userSessionService.Filter)
			{
				string userLogin = this.GetUserLogin();
				query = query.Where((Evenements e) => e.ERefContactNavigation.CNegociateur == userLogin || e.ERefContactNavigation.C2emeNegociateur == userLogin || e.ERefContactNavigation.CNomFamilleConseiller == userLogin || e.ERefContactNavigation.C2emeConseiller == userLogin || e.ERefContactNavigation.CApporteur == userLogin || e.ERefContactNavigation.C2emeApporteur == userLogin);
			}
			IQueryable<ClientLastEvenements> evQuery = query.Select((Evenements e) => new ClientLastEvenements
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
			return await EFHelper<ClientLastEvenements>.Select(evQuery, where, orderby, take, skip, select);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	private IQueryable<T> ApplyDefaultFilter<T>(IQueryable<T> query, string option) where T : Evenements
	{
		if (option == "prospects")
		{
			if (_userSessionService.Filter)
			{
				string userLogin = this.GetUserLogin();
				query = query.Where((T e) => e.ERefContactNavigation.CNegociateur == userLogin || e.ERefContactNavigation.C2emeNegociateur == userLogin || e.ERefContactNavigation.CNomFamilleConseiller == userLogin || e.ERefContactNavigation.C2emeConseiller == userLogin || e.ERefContactNavigation.CApporteur == userLogin || e.ERefContactNavigation.C2emeApporteur == userLogin);
			}
			query = query.Where((T e) => e.ERefContactNavigation.CStatut == "PROSPECT ACTIF" || e.ERefContactNavigation.CStatut == "PROSPECT MORT");
			if (_prospectionTypes == null || _prospectionTypesDate < DateTime.Now.AddHours(-1.0))
			{
				_prospectionTypes = (from te in _context.TypesEvenements
					where te.TeGenreEvenement == "PROSPECTION"
					select te.TeTypeEvenement).ToArray();
				_prospectionTypesDate = DateTime.Now;
			}
			if (_prospectionTypes.Length != 0)
			{
				query = query.Where((T e) => _prospectionTypes.Contains(e.ETypeEvenement));
			}
		}
		return query;
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<EvenementsEx>> GetEvenements(int id)
	{
		EvenementsEx evenements = (await GetEvenements(null, $"ERefEvenement eq {id}")).Value.Single();
		if (evenements == null)
		{
			return NotFound();
		}
		return evenements;
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> PutEvenements(int id, Evenements evenements)
	{
		if (id != evenements.ERefEvenement)
		{
			return BadRequest();
		}
		_context.Entry(evenements).State = EntityState.Modified;
		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException)
		{
			if (!EvenementsExists(id))
			{
				return NotFound();
			}
			throw;
		}
		return NoContent();
	}

	[HttpPost]
	public async Task<ActionResult<Evenements>> PostEvenements(Evenements evenements)
	{
		try
		{
			_context.Evenements.Add(evenements);
			await _context.SaveChangesAsync();
			return CreatedAtAction("GetEvenements", new
			{
				id = evenements.ERefEvenement
			}, evenements);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[HttpDelete("{id}")]
	public async Task<ActionResult<Evenements>> DeleteEvenements(int id)
	{
		Evenements evenements = await _context.Evenements.FindAsync(id);
		if (evenements == null)
		{
			return NotFound();
		}
		_context.Evenements.Remove(evenements);
		await _context.SaveChangesAsync();
		return evenements;
	}

	private bool EvenementsExists(int id)
	{
		return _context.Evenements.Any((Evenements e) => e.ERefEvenement == id);
	}

	[HttpGet("OffresEnCours")]
	[Authorize]
	public async Task<ActionResult<SelectResult<ClientEvenements>>> GetOffresEnCours([FromQuery] uint eRefConseiller = 0u, [FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		List<string> types = (await GetEvenementTypes(EventType.Agence)).ToList();
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
		IQueryable<Evenements> query = _context.Evenements;
		query = query.Where((Evenements e) => evenementIds.Contains(e.ERefEvenement));
		if (_userSessionService.Filter)
		{
			string userLogin = this.GetUserLogin();
			query = query.Where((Evenements e) => e.ERefContactNavigation.CNegociateur == userLogin || e.ERefContactNavigation.C2emeNegociateur == userLogin || e.ERefContactNavigation.CNomFamilleConseiller == userLogin || e.ERefContactNavigation.C2emeConseiller == userLogin || e.ERefContactNavigation.CApporteur == userLogin || e.ERefContactNavigation.C2emeApporteur == userLogin);
		}
		IQueryable<ClientEvenements> evQuery = query.Select((Evenements e) => new ClientEvenements
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
		return await EFHelper<ClientEvenements>.Select(evQuery, where, orderby, take, skip, select);
	}
}
