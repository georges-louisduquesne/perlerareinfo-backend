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
[EnableCors]
[ApiController]
[Authorize(Roles = "Admin")]
public class ConseillersPersonnelsTachesController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public ConseillersPersonnelsTachesController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<ConseillersPersonnelsTaches>>> GetConseillersPersonnelsTaches([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		try
		{
			IQueryable<ConseillersPersonnelsTaches> query = _context.ConseillersPersonnelsTaches.AsNoTracking();
			query = EFHelper<ConseillersPersonnelsTaches>.Apply(query, where, orderby, take, skip, select);
			List<ConseillersPersonnelsTaches> res = await query.AsNoTracking().ToListAsync();
			foreach (ConseillersPersonnelsTaches cr in res)
			{
				EncodingHelper.FixEncodingInStringProperties(cr);
			}
			return res;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[HttpGet("/api/AgentCommerciauxTaches")]
	public async Task<ActionResult<IEnumerable<AgentCommerciauxTache>>> GetAgentsCommerciauxTaches([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		try
		{
			IQueryable<AgentCommerciauxTache> query = from cpt in _context.ConseillersPersonnelsTaches
				join cp in _context.ConseillersPersonnels on cpt.CptRefConseiller equals cp.CpRefConseiller
				where (int?)cp.CpActif == (int?)1 && cpt.CptEtat != "fait" && cp.CpStatut == "AGENT COMMERCIAL"
				select new AgentCommerciauxTache
				{
					CptCom = cpt.CptCom,
					CptDateCreation = cpt.CptDateCreation,
					CptDateRealisation = cpt.CptDateRealisation,
					CptEtat = cpt.CptEtat,
					CptLien = cpt.CptLien,
					CptQui = cpt.CptQui,
					CptRef = cpt.CptRef,
					CptRefConseiller = cpt.CptRefConseiller,
					CptRefConseillerNavigation = cpt.CptRefConseillerNavigation,
					CptType = cpt.CptType,
					NomComplet = string.Concat(cp.CpNomFamille + " ", cp.CpPrenom)
				};
			query = EFHelper<AgentCommerciauxTache>.Apply(query, where, orderby, take, skip, select);
			return (await query.ToListAsync());
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[HttpGet("/api/AgentCommerciauxTaches/Count")]
	public async Task<ActionResult<int>> GetAgentsCommerciauxTachesCount([FromQuery] string where = null)
	{
		try
		{
			IQueryable<AgentCommerciauxTache> query = from cpt in _context.ConseillersPersonnelsTaches
				join cp in _context.ConseillersPersonnels on cpt.CptRefConseiller equals cp.CpRefConseiller
				where (int?)cp.CpActif == (int?)1 && cpt.CptEtat != "fait" && cp.CpStatut == "AGENT COMMERCIAL"
				select new AgentCommerciauxTache
				{
					CptCom = cpt.CptCom,
					CptDateCreation = cpt.CptDateCreation,
					CptDateRealisation = cpt.CptDateRealisation,
					CptEtat = cpt.CptEtat,
					CptLien = cpt.CptLien,
					CptQui = cpt.CptQui,
					CptRef = cpt.CptRef,
					CptRefConseiller = cpt.CptRefConseiller,
					CptRefConseillerNavigation = cpt.CptRefConseillerNavigation,
					CptType = cpt.CptType,
					NomComplet = string.Concat(cp.CpNomFamille + " ", cp.CpPrenom)
				};
			query = EFHelper<AgentCommerciauxTache>.Apply(query, where);
			return await query.CountAsync();
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<ConseillersPersonnelsTaches>> GetConseillersPersonnelsTaches(uint id)
	{
		ConseillersPersonnelsTaches conseillersPersonnelsTaches = await _context.ConseillersPersonnelsTaches.FindAsync(id);
		if (conseillersPersonnelsTaches == null)
		{
			return NotFound();
		}
		return conseillersPersonnelsTaches;
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> PutConseillersPersonnelsTaches(uint id, ConseillersPersonnelsTaches conseillersPersonnelsTaches)
	{
		if (id != conseillersPersonnelsTaches.CptRef)
		{
			return BadRequest();
		}
		_context.Entry(conseillersPersonnelsTaches).State = EntityState.Modified;
		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException)
		{
			if (!ConseillersPersonnelsTachesExists(id))
			{
				return NotFound();
			}
			throw;
		}
		return NoContent();
	}

	[HttpPost]
	public async Task<ActionResult<ConseillersPersonnelsTaches>> PostConseillersPersonnelsTaches(ConseillersPersonnelsTaches conseillersPersonnelsTaches)
	{
		_context.ConseillersPersonnelsTaches.Add(conseillersPersonnelsTaches);
		await _context.SaveChangesAsync();
		return CreatedAtAction("GetConseillersPersonnelsTaches", new
		{
			id = conseillersPersonnelsTaches.CptRef
		}, conseillersPersonnelsTaches);
	}

	[HttpDelete("{id}")]
	public async Task<ActionResult<ConseillersPersonnelsTaches>> DeleteConseillersPersonnelsTaches(uint id)
	{
		ConseillersPersonnelsTaches conseillersPersonnelsTaches = await _context.ConseillersPersonnelsTaches.FindAsync(id);
		if (conseillersPersonnelsTaches == null)
		{
			return NotFound();
		}
		_context.ConseillersPersonnelsTaches.Remove(conseillersPersonnelsTaches);
		await _context.SaveChangesAsync();
		return conseillersPersonnelsTaches;
	}

	private bool ConseillersPersonnelsTachesExists(uint id)
	{
		return _context.ConseillersPersonnelsTaches.Any((ConseillersPersonnelsTaches e) => e.CptRef == id);
	}
}
