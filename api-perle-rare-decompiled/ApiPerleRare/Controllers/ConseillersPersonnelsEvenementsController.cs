using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Application.Candidats;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Controllers;

/// <summary>
/// Advisor personal events (candidats home). GET list uses <see cref="QueryEntitiesController{TEntity}"/>.
/// </summary>
[EnableCors]
[Authorize(Roles = "Admin")]
public class ConseillersPersonnelsEvenementsController : QueryEntitiesController<ConseillersPersonnelsEvenements>
{
	private readonly ApplicationDbContext _context;

	public ConseillersPersonnelsEvenementsController(
		IQueryEntitiesUseCase<ConseillersPersonnelsEvenements> query,
		ApplicationDbContext context)
		: base(query)
	{
		_context = context;
	}

	/// <summary>RDV candidats actifs à partir d'aujourd'hui (catégorie RV).</summary>
	[HttpGet("Home")]
	public async Task<ActionResult<IEnumerable<ConseillersPersonnelsEvenements>>> GetHomeCandidatEvenements()
	{
		try
		{
			DateTime today = DateTime.Today;
			IQueryable<ConseillersPersonnelsEvenements> query = HomeCandidatAccueilQueries.UpcomingRdvForCandidats(
				_context.ConseillersPersonnelsEvenements.AsNoTracking(),
				_context.ConseillersPersonnels.AsNoTracking(),
				_context.ConseillersPersonnelsTypesEvenements.AsNoTracking(),
				today);
			List<ConseillersPersonnelsEvenements> res = await query.ToListAsync();
			foreach (ConseillersPersonnelsEvenements row in res)
			{
				EncodingHelper.FixEncodingInStringProperties(row);
			}
			return res;
		}
		catch (Exception ex)
		{
			return BadRequest(ex.ToString());
		}
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<ConseillersPersonnelsEvenements>> GetConseillersPersonnelsEvenements(int id)
	{
		ConseillersPersonnelsEvenements row = await _context.ConseillersPersonnelsEvenements.FindAsync(id);
		if (row == null)
		{
			return NotFound();
		}
		return row;
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> PutConseillersPersonnelsEvenements(int id, ConseillersPersonnelsEvenements body)
	{
		if (id != body.CpeRefEvenement)
		{
			return BadRequest();
		}
		_context.Entry(body).State = EntityState.Modified;
		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException)
		{
			if (!_context.ConseillersPersonnelsEvenements.Any((ConseillersPersonnelsEvenements e) => e.CpeRefEvenement == id))
			{
				return NotFound();
			}
			throw;
		}
		return NoContent();
	}

	[HttpPost]
	public async Task<ActionResult<ConseillersPersonnelsEvenements>> PostConseillersPersonnelsEvenements(ConseillersPersonnelsEvenements body)
	{
		_context.ConseillersPersonnelsEvenements.Add(body);
		await _context.SaveChangesAsync();
		return CreatedAtAction(nameof(GetConseillersPersonnelsEvenements), new { id = body.CpeRefEvenement }, body);
	}

	[HttpDelete("{id}")]
	public async Task<ActionResult<ConseillersPersonnelsEvenements>> DeleteConseillersPersonnelsEvenements(int id)
	{
		ConseillersPersonnelsEvenements row = await _context.ConseillersPersonnelsEvenements.FindAsync(id);
		if (row == null)
		{
			return NotFound();
		}
		_context.ConseillersPersonnelsEvenements.Remove(row);
		await _context.SaveChangesAsync();
		return row;
	}
}
