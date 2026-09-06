using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
[EnableCors]
public class TachesController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	private readonly IUserSessionService _userSessionService;

	private readonly IUserService _userService;

	public TachesController(ApplicationDbContext context, IUserSessionService userSessionService, IUserService userService)
	{
		_context = context;
		_userSessionService = userSessionService;
		_userService = userService;
	}

	private IQueryable<TachesEx> GetTachesExQuery()
	{
		return _context.Taches.Select((Taches t) => new TachesEx
		{
			TRefAnnonce = t.TRefAnnonce,
			TCom = t.TCom,
			TDateCreation = t.TDateCreation,
			TDateRealisation = t.TDateRealisation,
			TEtat = t.TEtat,
			TLien = t.TLien,
			TPropertyId = t.TPropertyId,
			TQui = t.TQui,
			TRef = t.TRef,
			TRefContact = t.TRefContact,
			TRefContactNavigation = t.TRefContactNavigation,
			TType = t.TType,
			CNegociateur = t.TRefContactNavigation.CNegociateur
		});
	}

	private T CompleteTachesExes<T>(T taches) where T : IEnumerable<ITachesEx>
	{
		foreach (ITachesEx t in taches)
		{
			if (!string.IsNullOrEmpty(t.TQui))
			{
				t.TQui_PS = _userService.GetConseiller(t.TQui)?.CpPhotoSignature;
			}
			if (!string.IsNullOrEmpty(t.CNegociateur))
			{
				t.CNegociateur_PS = _userService.GetConseiller(t.CNegociateur)?.CpPhotoSignature;
			}
		}
		return taches;
	}

	private SelectResult<T> CompleteTachesExes<T>(SelectResult<T> taches) where T : ITachesEx
	{
		CompleteTachesExes(((IEnumerable<T>)taches.Items).Select((Func<T, ITachesEx>)((T v) => v)));
		return taches;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<TachesEx>>> GetTaches([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0, [FromQuery] string option = null)
	{
		try
		{
			IQueryable<TachesEx> query = GetTachesExQuery();
			query = ApplyDefaultFilter(query, option);
			query = EFHelper<TachesEx>.Apply(query, where, orderby, take, skip, select);
			List<TachesEx> list = await query.ToListAsync();
			list.FixEncoding();
			return CompleteTachesExes(list);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[HttpGet]
	[Route("Count")]
	public async Task<ActionResult<int>> GetTachesCount([FromQuery] string where = null, [FromQuery] string option = null)
	{
		try
		{
			IQueryable<TachesEx> query = GetTachesExQuery();
			query = ApplyDefaultFilter(query, option);
			query = EFHelper<TachesEx>.Apply(query, where);
			return await query.CountAsync();
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	private IQueryable<TachesEx> ApplyDefaultFilter(IQueryable<TachesEx> query, string option)
	{
		if (option == "prospects")
		{
			if (_userSessionService.Filter)
			{
				string user = this.GetUserLogin();
				query = query.Where((TachesEx t) => t.TQui == user);
			}
			query = query.Where((TachesEx t) => t.TRefContactNavigation.CStatut == "PROSPECT ACTIF" || t.TRefContactNavigation.CStatut == "PROSPECT MORT");
		}
		return query;
	}

	[HttpGet]
	[Route("Prospects")]
	public async Task<ActionResult<SelectResult<ProspectTaches>>> GetProspectTaches([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		try
		{
			IQueryable<Taches> query = _context.Taches;
			query = query.Where((Taches t) => t.TEtat == "");
			DateTime tomorrow = DateTime.Today.AddDays(1.0);
			query = query.Where((Taches t) => t.TDateRealisation < tomorrow);
			query = query.Where((Taches t) => t.TRefContactNavigation.CStatut == "PROSPECT ACTIF" || t.TRefContactNavigation.CStatut == "PROSPECT MORT");
			if (_userSessionService.Filter)
			{
				string userLogin = this.GetUserLogin();
				query = query.Where((Taches t) => t.TQui == userLogin);
			}
			IQueryable<ProspectTaches> tachesQuery = query.Select((Taches t) => new ProspectTaches
			{
				TRef = t.TRef,
				TDateRealisation = t.TDateRealisation,
				CNomFamille = t.TRefContactNavigation.CNomFamille,
				TType = t.TType,
				TCom = t.TCom,
				TRefContact = t.TRefContact,
				TQui = t.TQui,
				CNegociateur = t.TRefContactNavigation.CNegociateur
			});
			return CompleteTachesExes(await EFHelper<ProspectTaches>.Select(tachesQuery, where, orderby, take, skip, select));
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[HttpGet]
	[Route("Clients")]
	public async Task<ActionResult<SelectResult<ProspectTaches>>> GetClientsTaches([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		try
		{
			IQueryable<Taches> query = _context.Taches;
			query = query.Where((Taches t) => t.TEtat == "");
			DateTime today = DateTime.Today;
			query = query.Where((Taches t) => t.TDateRealisation <= today);
			query = query.Where((Taches t) => t.TRefContactNavigation.CStatut == "CLIENT ACTIF" || t.TRefContactNavigation.CStatut == "CLIENT MORT");
			if (_userSessionService.Filter)
			{
				string userLogin = this.GetUserLogin();
				query = query.Where((Taches t) => t.TQui == userLogin);
			}
			IQueryable<ProspectTaches> tachesQuery = query.Select((Taches t) => new ProspectTaches
			{
				TRef = t.TRef,
				TDateRealisation = t.TDateRealisation,
				CNomFamille = t.TRefContactNavigation.CNomFamille,
				TType = t.TType,
				TCom = t.TCom,
				TRefContact = t.TRefContact,
				TQui = t.TQui,
				CNegociateur = t.TRefContactNavigation.CNegociateur
			});
			return CompleteTachesExes(await EFHelper<ProspectTaches>.Select(tachesQuery, where, orderby, take, skip, select));
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[Authorize]
	[HttpGet]
	[Route("ActiveCount")]
	public async Task<ActionResult<int>> GetActiveTachesCount()
	{
		string login = this.GetUserLogin();
		return await _context.Taches.CountAsync((Taches f) => f.TQui == login && f.TEtat != "fait" && f.TDateRealisation <= DateTime.Now);
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<Taches>> GetTaches(uint id)
	{
		Taches taches = await _context.Taches.FindAsync(id);
		if (taches == null)
		{
			return NotFound();
		}
		return taches;
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> PutTaches(uint id, Taches taches)
	{
		if (id != taches.TRef)
		{
			return BadRequest();
		}
		_context.Entry(taches).State = EntityState.Modified;
		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException)
		{
			if (!TachesExists(id))
			{
				return NotFound();
			}
			throw;
		}
		return NoContent();
	}

	[HttpPost]
	public async Task<ActionResult<Taches>> PostTaches(Taches taches)
	{
		_context.Taches.Add(taches);
		await _context.SaveChangesAsync();
		return CreatedAtAction("GetTaches", new
		{
			id = taches.TRef
		}, taches);
	}

	[HttpDelete("{id}")]
	public async Task<ActionResult<Taches>> DeleteTaches(uint id)
	{
		Taches taches = await _context.Taches.FindAsync(id);
		if (taches == null)
		{
			return NotFound();
		}
		_context.Taches.Remove(taches);
		await _context.SaveChangesAsync();
		return taches;
	}

	private bool TachesExists(uint id)
	{
		return _context.Taches.Any((Taches e) => e.TRef == id);
	}
}
