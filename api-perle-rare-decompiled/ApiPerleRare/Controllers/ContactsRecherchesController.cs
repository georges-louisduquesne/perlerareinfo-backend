using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApiPerleRare.Application.Contacts;
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
public class ContactsRecherchesController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	private readonly IUserSessionService _userSessionService;

	private readonly IListContactsRechercheExUseCase _listEx;

	private readonly IListContactsRechercheAccueilUseCase _listAccueil;

	private readonly ICountContactsRechercheUseCase _count;

	private readonly IGetContactsRechercheByIdUseCase _getById;

	private readonly ICreateContactsRechercheUseCase _create;

	private readonly IUpdateContactsRechercheUseCase _update;

	public ContactsRecherchesController(
		ApplicationDbContext context,
		IUserSessionService userSessionService,
		IListContactsRechercheExUseCase listEx,
		IListContactsRechercheAccueilUseCase listAccueil,
		ICountContactsRechercheUseCase count,
		IGetContactsRechercheByIdUseCase getById,
		ICreateContactsRechercheUseCase create,
		IUpdateContactsRechercheUseCase update)
	{
		_context = context;
		_userSessionService = userSessionService;
		_listEx = listEx;
		_listAccueil = listAccueil;
		_count = count;
		_getById = getById;
		_create = create;
		_update = update;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<ContactsRechercheEx>>> GetContactsRecherche([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		try
		{
			return await _listEx.Execute(select, where, orderby, skip, take, _userSessionService.Filter, this.GetUserLogin());
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[HttpGet]
	[Route("Accueil")]
	public async Task<ActionResult<IEnumerable<ContactsRechercheAccueil>>> GetContactsRechercheAccueil([FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0, [FromQuery] int withAnnonces = 0, [FromQuery] int withTaches = 0)
	{
		try
		{
			return await _listAccueil.Execute(where, orderby, skip, take, withAnnonces, withTaches, _userSessionService.Filter, this.GetUserLogin());
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[HttpGet]
	[Route("/api/ContactsRecherchesCount")]
	[Route("Count")]
	public async Task<ActionResult<int>> GetContactsRechercheCount([FromQuery] string where = null)
	{
		try
		{
			return await _count.Execute(where, _userSessionService.Filter, this.GetUserLogin());
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<ContactsRecherche>> GetContactsRecherche(uint id)
	{
		ContactsRecherche contactsRecherche = await _getById.Execute(id);
		if (contactsRecherche == null)
		{
			return NotFound();
		}
		return contactsRecherche;
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> PutContactsRecherche(uint id, ContactsRecherche contactsRecherche)
	{
		UpdateContactsRechercheStatus status = await _update.Execute(id, contactsRecherche);
		return status switch
		{
			UpdateContactsRechercheStatus.IdMismatch => BadRequest(),
			UpdateContactsRechercheStatus.NotFound => NotFound(),
			_ => NoContent()
		};
	}

	[HttpPost]
	public async Task<ActionResult<ContactsRecherche>> PostContactsRecherche(ContactsRecherche ncr)
	{
		ContactsRecherche created = await _create.Execute(ncr);
		if (created == null)
		{
			return BadRequest("Doublon");
		}
		return CreatedAtAction("GetContactsRecherche", new
		{
			id = created.CRefContact
		}, created);
	}

	[HttpDelete("{id}")]
	public async Task<ActionResult<ContactsRecherche>> DeleteContactsRecherche(uint id)
	{
		ContactsRecherche contactsRecherche = await _context.ContactsRecherche.FindAsync(id);
		if (contactsRecherche == null)
		{
			return NotFound();
		}
		_context.ContactsRecherche.Remove(contactsRecherche);
		await _context.SaveChangesAsync();
		return contactsRecherche;
	}
}
