using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ContactIntermediairesController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public ContactIntermediairesController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<SelectResult<ContactIntermediaireLight>>> GetContactIntermediaire([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		IQueryable<ContactIntermediaireLight> query = _context.ContactIntermediaire.Select((ContactIntermediaire c) => new ContactIntermediaireLight
		{
			CRefCtcInter = c.CRefCtcInter,
			CStatut = c.CStatut,
			CTel = c.CTel,
			CNom = c.CNom,
			CPrenom = c.CPrenom,
			CMel = c.CMel,
			CCom = c.CCom,
			CQualiteRelation = c.CQualiteRelation,
			CDirecteur = c.CDirecteur,
			CPhoto = c.CPhoto,
			IRefIntermediaire = c.CRefIntermediaireNavigation.IRefIntermediaire,
			INomIntermediaire = c.CRefIntermediaireNavigation.INomIntermediaire,
			IActif = c.CRefIntermediaireNavigation.IActif,
			ITelephone = c.CRefIntermediaireNavigation.ITelephone,
			ITelephone2 = c.CRefIntermediaireNavigation.ITelephone2
		});
		return await EFHelper<ContactIntermediaireLight>.Select(query, where, orderby, take, skip, select);
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<ContactIntermediaire>> GetContactIntermediaire(uint id)
	{
		ContactIntermediaire contactIntermediaire = await _context.ContactIntermediaire.FindAsync(id);
		if (contactIntermediaire == null)
		{
			return NotFound();
		}
		return contactIntermediaire;
	}

	/// <summary>Historique agent (actor_logs + actor_search) : ajout / modif / désactivation.</summary>
	[HttpGet("{id}/ActorHistory")]
	public ActionResult<ContactIntermediaireActorHistoryItem[]> GetActorHistory(uint id)
	{
		MySqlConnection c = (MySqlConnection)_context.Database.GetDbConnection();
		if (c.State != ConnectionState.Open)
		{
			c.Open();
		}
		var items = new List<ContactIntermediaireActorHistoryItem>();
		using (MySqlCommand cmd = c.CreateCommand())
		{
			cmd.CommandText = @"SELECT al.AL_Action AS Action, asr.AS_Date AS Date, asr.AS_Actor AS Actor
FROM actor_logs al
INNER JOIN actor_search asr ON asr.AS_Id = al.AL_RefAS
WHERE al.AL_RefCtcInter = @id
ORDER BY asr.AS_Date ASC, al.AL_Id ASC";
			cmd.Parameters.AddWithValue("@id", id);
			using MySqlDataReader reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				items.Add(new ContactIntermediaireActorHistoryItem
				{
					Action = reader.IsDBNull(0) ? "" : reader.GetString(0),
					Date = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1),
					Actor = reader.IsDBNull(2) ? "" : reader.GetString(2)
				});
			}
		}
		return items.ToArray();
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> PutContactIntermediaire(uint id, ContactIntermediaire contactIntermediaire)
	{
		if (id != contactIntermediaire.CRefCtcInter)
		{
			return BadRequest();
		}
		_context.Entry(contactIntermediaire).State = EntityState.Modified;
		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException)
		{
			if (!ContactIntermediaireExists(id))
			{
				return NotFound();
			}
			throw;
		}
		return NoContent();
	}

	[HttpPost]
	public async Task<ActionResult<ContactIntermediaire>> PostContactIntermediaire(ContactIntermediaire contactIntermediaire)
	{
		_context.ContactIntermediaire.Add(contactIntermediaire);
		await _context.SaveChangesAsync();
		return CreatedAtAction("GetContactIntermediaire", new
		{
			id = contactIntermediaire.CRefCtcInter
		}, contactIntermediaire);
	}

	[HttpDelete("{id}")]
	public async Task<ActionResult<ContactIntermediaire>> DeleteContactIntermediaire(uint id)
	{
		ContactIntermediaire contactIntermediaire = await _context.ContactIntermediaire.FindAsync(id);
		if (contactIntermediaire == null)
		{
			return NotFound();
		}
		_context.ContactIntermediaire.Remove(contactIntermediaire);
		await _context.SaveChangesAsync();
		return contactIntermediaire;
	}

	private bool ContactIntermediaireExists(uint id)
	{
		return _context.ContactIntermediaire.Any((ContactIntermediaire e) => e.CRefCtcInter == id);
	}
}
