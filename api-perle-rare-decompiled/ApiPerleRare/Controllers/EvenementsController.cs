using System;
using System.Collections.Generic;
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

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[EnableCors]
[ApiController]
[Authorize]
public class EvenementsController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	private readonly IUserSessionService _userSessionService;

	private readonly IListEncaissementsEnCoursUseCase _encaissementsEnCours;

	private readonly IListHomeClientEvenementsUseCase _homeClientEvenements;

	private readonly IListProspectEvenementsUseCase _prospectEvenements;

	private readonly IListClientLastEvenementsUseCase _clientLastEvenements;

	private readonly IListEvenementsExUseCase _listEvenementsEx;

	private readonly ICountEvenementsUseCase _countEvenements;

	private readonly IListContactEvenementsUseCase _contactEvenements;

	private readonly IListOffresEnCoursUseCase _offresEnCours;

	public EvenementsController(
		ApplicationDbContext context,
		IUserSessionService userSessionService,
		IListEncaissementsEnCoursUseCase encaissementsEnCours,
		IListHomeClientEvenementsUseCase homeClientEvenements,
		IListProspectEvenementsUseCase prospectEvenements,
		IListClientLastEvenementsUseCase clientLastEvenements,
		IListEvenementsExUseCase listEvenementsEx,
		ICountEvenementsUseCase countEvenements,
		IListContactEvenementsUseCase contactEvenements,
		IListOffresEnCoursUseCase offresEnCours)
	{
		_context = context;
		_userSessionService = userSessionService;
		_encaissementsEnCours = encaissementsEnCours;
		_homeClientEvenements = homeClientEvenements;
		_prospectEvenements = prospectEvenements;
		_clientLastEvenements = clientLastEvenements;
		_listEvenementsEx = listEvenementsEx;
		_countEvenements = countEvenements;
		_contactEvenements = contactEvenements;
		_offresEnCours = offresEnCours;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<EvenementsEx>>> GetEvenements([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0, [FromQuery] string option = null)
	{
		try
		{
			return await _listEvenementsEx.Execute(
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
	[Route("Contact")]
	public async Task<ActionResult<IEnumerable<ContactEvenements>>> GetContactEvenements([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0, [FromQuery] string option = null)
	{
		try
		{
			return await _contactEvenements.Execute(ToQuery(select, where, orderby, skip, take));
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
			return await _countEvenements.Execute(
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
	[Route("Clients")]
	public async Task<ActionResult<SelectResult<ClientEvenements>>> GetClientEvenements([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		return await GetHomeEvenements(HomeEventKind.Clients, select, where, orderby, skip, take);
	}

	[HttpGet]
	[Route("RdvClients")]
	public async Task<ActionResult<SelectResult<ClientEvenements>>> GetRdvClientsEvenements([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		return await GetHomeEvenements(HomeEventKind.RdvClient, select, where, orderby, skip, take);
	}

	[HttpGet]
	[Route("Transactions")]
	public async Task<ActionResult<SelectResult<ClientEvenements>>> GetTransactionsEvenements([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		return await GetHomeEvenements(HomeEventKind.Transactions, select, where, orderby, skip, take);
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
				ToQuery(select, where, orderby, skip, take),
				_userSessionService.Filter ? this.GetUserLogin() : null,
				_userSessionService.Filter);
		}
		catch (Exception ex)
		{
			return BadRequest(ex.ToString());
		}
	}

	private async Task<ActionResult<SelectResult<ClientEvenements>>> GetHomeEvenements(HomeEventKind kind, string select, string where, string orderby, int skip, int take)
	{
		try
		{
			return await _homeClientEvenements.Execute(
				kind,
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
	[Route("Prospects")]
	public async Task<ActionResult<SelectResult<ProspectEvenements>>> GetProspectEvenements([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		try
		{
			return await _prospectEvenements.Execute(
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
	[Route("ClientsLast")]
	public async Task<ActionResult<SelectResult<ClientLastEvenements>>> GetClientLastEvenements([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		try
		{
			return await _clientLastEvenements.Execute(
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

	[HttpGet("{id}")]
	public async Task<ActionResult<EvenementsEx>> GetEvenements(int id)
	{
		EvenementsEx evenements = (await _listEvenementsEx.Execute(
			ToQuery(null, $"ERefEvenement eq {id}", null, 0, 0),
			null,
			_userSessionService.Filter ? this.GetUserLogin() : null,
			_userSessionService.Filter)).Single();
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
		return await _offresEnCours.Execute(
			ToQuery(select, where, orderby, skip, take),
			eRefConseiller,
			_userSessionService.Filter ? this.GetUserLogin() : null,
			_userSessionService.Filter);
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
