#define TRACE
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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
public class BiensController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	private readonly IUserSessionService _userSessionService;

	public BiensController(ApplicationDbContext context, IUserSessionService userSessionService)
	{
		_context = context;
		_userSessionService = userSessionService;
	}

	[HttpGet]
	[Authorize]
	public async Task<ActionResult<IEnumerable<Biens>>> GetBiens([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		try
		{
			IQueryable<Biens> query = _context.Biens.AsNoTracking();
			query = ApplyDefaultFilter(query);
			query = EFHelper<Biens>.Apply(query, where, orderby, take, skip, select);
			return (await query.ToListAsync());
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	private IQueryable<Biens> ApplyDefaultFilter(IQueryable<Biens> query)
	{
		return query;
	}

	[HttpGet]
	[Route("Count")]
	[Authorize]
	public async Task<ActionResult<int>> GetBiensCount([FromQuery] string where = null)
	{
		try
		{
			IQueryable<Biens> query = _context.Biens;
			query = ApplyDefaultFilter(query);
			query = EFHelper<Biens>.Apply(query, where);
			return await query.CountAsync();
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[HttpGet]
	[Route("NonVus")]
	[Authorize]
	public ActionResult<long> GetBiensNonVus()
	{
		try
		{
			string login = this.GetUserLogin();
			bool mustFilter = _userSessionService.Filter;
			string select = "SELECT COUNT(*) FROM property_contact pc\r\nINNER JOIN property p ON pc.PC_PropertyId=p.P_PropertyId\r\nINNER JOIN contacts_recherche cr ON pc.PC_RefContact=cr.C_RefContact\r\nWHERE pc.PC_Actif=1 AND pc.PC_Vu<>1 AND (C_NomFamilleConseiller=@conseillerid OR C_2emeConseiller=@conseillerid)";
			MySqlConnection c = (MySqlConnection)_context.Database.GetDbConnection();
			if (c.State != ConnectionState.Open)
			{
				c.Open();
			}
			using MySqlCommand cmd = c.CreateCommand();
			cmd.CommandText = select;
			cmd.Parameters.AddWithValue("@conseillerid", login);
			object res = cmd.ExecuteScalar();
			if (res is long v)
			{
				return v;
			}
			if (res is int i)
			{
				return i;
			}
			if (res == null || res is DBNull)
			{
				Trace.WriteLine("Null récupéré pour Non vu");
				return -1L;
			}
			throw new Exception("Type '" + res.GetType().FullName + "' non géré");
		}
		catch (Exception ex)
		{
			return BadRequest(ex.ToString());
		}
	}

	[HttpGet("{id}")]
	[Authorize]
	public async Task<ActionResult<Biens>> GetBiens(uint id)
	{
		Biens biens = await _context.Biens.FindAsync(id);
		if (biens == null)
		{
			return NotFound();
		}
		return biens;
	}

	[HttpPut("{id}")]
	[Authorize]
	public async Task<IActionResult> PutBiens(uint id, Biens biens)
	{
		if (id != biens.BRef)
		{
			return BadRequest();
		}
		_context.Entry(biens).State = EntityState.Modified;
		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException)
		{
			if (!BiensExists(id))
			{
				return NotFound();
			}
			throw;
		}
		return NoContent();
	}

	[HttpPost]
	[Authorize]
	public async Task<ActionResult<Biens>> PostBiens(Biens biens)
	{
		_context.Biens.Add(biens);
		await _context.SaveChangesAsync();
		return CreatedAtAction("GetBiens", new
		{
			id = biens.BRef
		}, biens);
	}

	[HttpDelete("{id}")]
	[Authorize]
	public async Task<ActionResult<Biens>> DeleteBiens(uint id)
	{
		Biens biens = await _context.Biens.FindAsync(id);
		if (biens == null)
		{
			return NotFound();
		}
		_context.Biens.Remove(biens);
		await _context.SaveChangesAsync();
		return biens;
	}

	private bool BiensExists(uint id)
	{
		return _context.Biens.Any((Biens e) => e.BRef == id);
	}

	[HttpGet]
	[Authorize]
	[Route("/api/ContactBiens")]
	public ActionResult<ContactBien[]> GetContactBiens([FromQuery] int refCtc, [FromQuery] string typeEvenement)
	{
		try
		{
			string where = $"E_RefContact = '{refCtc}' AND E_Statut = '1'";
			switch (typeEvenement)
			{
			case "VIS":
			case "OFFRE":
				where += " AND E_TypeEvenement NOT LIKE 'OFFRE%'";
				break;
			case "REP. OFFRE":
				where += " AND E_TypeEvenement = 'OFFRE'";
				break;
			case "AGENCE":
				where += " AND E_TypeEvenement = 'OFFRE ACCEPTEE'";
				break;
			}
			string query = "SELECT B_Ref, B_Adresse, B_CP, \r\n\t\t\t\tIFNULL(E_RefInterI, 'NC') AS E_RefInterI, I2_NomInterm_Indirect, \r\n\t\t\t\tIFNULL(E_RefInterD, 'NC') AS E_RefInterD, I_NomIntermediaire, \r\n\t\t\t\tIFNULL(E_RefCtcInter, 'NC') AS E_RefCtcInter, C_Nom, C_Prenom, \r\n\t\t\t\tIFNULL(E_RefAnnAGC, 'NC') AS E_RefAnnAGC, \r\n\t\t\t\tIF(E_CR IS NULL OR E_CR = '', 'NC', E_CR) AS E_CR \r\n\t\t\tFROM biens \r\n\t\t\t\tLEFT JOIN evenements ON B_Ref = E_RefBien \r\n\t\t\t\tLEFT JOIN contact_intermediaire ON E_RefCtcInter = C_RefCtcInter \r\n\t\t\t\tLEFT JOIN intermediaires_indirects ON E_RefInterI = I2_RefInterm_indirect \r\n\t\t\t\tLEFT JOIN intermediaires_directs ON E_RefInterD = I_RefIntermediaire \r\n\t\t\tWHERE " + where + "\r\n\t\t\tGROUP BY B_Ref, E_RefInterI, E_RefInterD, E_RefCtcInter \r\n\t\t\tORDER BY E_Date DESC, E_RefEvenement DESC";
			MySqlConnection c = (MySqlConnection)_context.Database.GetDbConnection();
			if (c.State != ConnectionState.Open)
			{
				c.Open();
			}
			using MySqlCommand cmd = c.CreateCommand();
			cmd.CommandText = query;
			using MySqlDataReader reader = cmd.ExecuteReader();
			return reader.ToModels<ContactBien>();
		}
		catch (Exception ex)
		{
			return BadRequest(ex.ToString());
		}
	}
}
