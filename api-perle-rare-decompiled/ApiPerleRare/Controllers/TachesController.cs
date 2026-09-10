using System;
using System.Collections.Generic;
using System.Linq;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Application.Tasks;
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

	private readonly IListTachesExUseCase _listTachesEx;

	private readonly ICountTachesUseCase _countTaches;

	private readonly IListProspectTachesUseCase _prospectTaches;

	private readonly IListClientTachesUseCase _clientTaches;

	private readonly ICountActiveTachesUseCase _countActiveTaches;

	public TachesController(
		ApplicationDbContext context,
		IUserSessionService userSessionService,
		IListTachesExUseCase listTachesEx,
		ICountTachesUseCase countTaches,
		IListProspectTachesUseCase prospectTaches,
		IListClientTachesUseCase clientTaches,
		ICountActiveTachesUseCase countActiveTaches)
	{
		_context = context;
		_userSessionService = userSessionService;
		_listTachesEx = listTachesEx;
		_countTaches = countTaches;
		_prospectTaches = prospectTaches;
		_clientTaches = clientTaches;
		_countActiveTaches = countActiveTaches;
	}

	[HttpGet]
	public async System.Threading.Tasks.Task<ActionResult<IEnumerable<TachesEx>>> GetTaches([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0, [FromQuery] string option = null)
	{
		try
		{
			return await _listTachesEx.Execute(
				ToQuery(select, where, orderby, skip, take),
				option,
				_userSessionService.Filter ? this.GetUserLogin() : null,
				_userSessionService.Filter);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[HttpGet]
	[Route("Count")]
	public async System.Threading.Tasks.Task<ActionResult<int>> GetTachesCount([FromQuery] string where = null, [FromQuery] string option = null)
	{
		try
		{
			return await _countTaches.Execute(
				where,
				option,
				_userSessionService.Filter ? this.GetUserLogin() : null,
				_userSessionService.Filter);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[HttpGet]
	[Route("Prospects")]
	public async System.Threading.Tasks.Task<ActionResult<SelectResult<ProspectTaches>>> GetProspectTaches([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		try
		{
			return await _prospectTaches.Execute(
				ToQuery(select, where, orderby, skip, take),
				_userSessionService.Filter ? this.GetUserLogin() : null,
				_userSessionService.Filter);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[HttpGet]
	[Route("Clients")]
	public async System.Threading.Tasks.Task<ActionResult<SelectResult<ProspectTaches>>> GetClientsTaches([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		try
		{
			return await _clientTaches.Execute(
				ToQuery(select, where, orderby, skip, take),
				_userSessionService.Filter ? this.GetUserLogin() : null,
				_userSessionService.Filter);
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
	public async System.Threading.Tasks.Task<ActionResult<int>> GetActiveTachesCount()
	{
		return await _countActiveTaches.Execute(this.GetUserLogin());
	}

	[HttpGet("{id}")]
	public async System.Threading.Tasks.Task<ActionResult<Taches>> GetTaches(uint id)
	{
		Taches taches = await _context.Taches.FindAsync(id);
		if (taches == null)
		{
			return NotFound();
		}
		return taches;
	}

	[HttpPut("{id}")]
	public async System.Threading.Tasks.Task<IActionResult> PutTaches(uint id, Taches taches)
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
	public async System.Threading.Tasks.Task<ActionResult<Taches>> PostTaches(Taches taches)
	{
		_context.Taches.Add(taches);
		await _context.SaveChangesAsync();
		return CreatedAtAction("GetTaches", new
		{
			id = taches.TRef
		}, taches);
	}

	[HttpDelete("{id}")]
	public async System.Threading.Tasks.Task<ActionResult<Taches>> DeleteTaches(uint id)
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

	private static EntityQuery ToQuery(string select, string where, string orderby, int skip, int take)
	{
		return new EntityQuery
		{
			Select = select,
			Where = where,
			OrderBy = orderby,
			Skip = skip,
			Take = take
		};
	}
}
