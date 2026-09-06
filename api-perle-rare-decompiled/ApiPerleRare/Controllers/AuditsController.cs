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
[Authorize]
public class AuditsController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public AuditsController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<IEnumerable<Audit>>> GetAudit([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		try
		{
			IQueryable<Audit> query = _context.Audit.AsNoTracking();
			query = ApplyDefaultFilter(query);
			query = EFHelper<Audit>.Apply(query, where, orderby, take, skip, select);
			return (ActionResult<IEnumerable<Audit>>)(IEnumerable<Audit>)(await query.ToListAsync());
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	private IQueryable<Audit> ApplyDefaultFilter(IQueryable<Audit> query)
	{
		return query;
	}

	[HttpGet("{id}")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<Audit>> GetAudit(int id)
	{
		Audit audit = await _context.Audit.FindAsync(id);
		if (audit == null)
		{
			return NotFound();
		}
		return audit;
	}

	[HttpPut("{id}")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> PutAudit(int id, Audit audit)
	{
		if (id != audit.AId)
		{
			return BadRequest();
		}
		_context.Entry(audit).State = EntityState.Modified;
		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException)
		{
			if (!AuditExists(id))
			{
				return NotFound();
			}
			throw;
		}
		return NoContent();
	}

	[HttpPost]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<Audit>> PostAudit(Audit audit)
	{
		_context.Audit.Add(audit);
		await _context.SaveChangesAsync();
		return CreatedAtAction("GetAudit", new
		{
			id = audit.AId
		}, audit);
	}

	[HttpDelete("{id}")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<Audit>> DeleteAudit(int id)
	{
		Audit audit = await _context.Audit.FindAsync(id);
		if (audit == null)
		{
			return NotFound();
		}
		_context.Audit.Remove(audit);
		await _context.SaveChangesAsync();
		return audit;
	}

	private bool AuditExists(int id)
	{
		return _context.Audit.Any((Audit e) => e.AId == id);
	}
}
