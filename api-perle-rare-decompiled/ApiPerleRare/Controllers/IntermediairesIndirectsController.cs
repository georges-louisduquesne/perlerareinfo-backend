using System;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Application.Files;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class IntermediairesIndirectsController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	private readonly ImportNetworkLogoUseCase _importLogo;

	public IntermediairesIndirectsController(ApplicationDbContext context, ImportNetworkLogoUseCase importLogo)
	{
		_context = context;
		_importLogo = importLogo;
	}

	[HttpGet]
	public async Task<ActionResult<SelectResult<IntermediairesIndirects>>> GetIntermediairesIndirects([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		DbSet<IntermediairesIndirects> query = _context.IntermediairesIndirects;
		return await EFHelper<IntermediairesIndirects>.Select(query, where, orderby, take, skip, select);
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<IntermediairesIndirects>> GetIntermediairesIndirects(uint id)
	{
		IntermediairesIndirects intermediairesIndirects = await _context.IntermediairesIndirects.FindAsync(id);
		if (intermediairesIndirects == null)
		{
			return NotFound();
		}
		return intermediairesIndirects;
	}

	[HttpPost("{id}/logo")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> ImportLogo(uint id, AgencyLogoImportRequest request)
	{
		try
		{
			NetworkLogoImportResult result = await _importLogo.Execute(id, request?.Url, HttpContext.RequestAborted);
			return Ok(result);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { errors = ex.Message });
		}
	}

	[HttpDelete("{id}/logo")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> ClearLogo(uint id)
	{
		try
		{
			await _importLogo.ExecuteClear(id, HttpContext.RequestAborted);
			return NoContent();
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { errors = ex.Message });
		}
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> PutIntermediairesIndirects(uint id, IntermediairesIndirects intermediairesIndirects)
	{
		if (id != intermediairesIndirects.I2RefIntermIndirect)
		{
			return BadRequest();
		}
		_context.Entry(intermediairesIndirects).State = EntityState.Modified;
		// Le logo ne passe que par POST/DELETE …/logo. Sinon la fiche ouverte réécrit
		// l’ancien I2_Icon et un logo effacé ne peut plus être remplacé.
		_context.Entry(intermediairesIndirects).Property((IntermediairesIndirects row) => row.I2Icon).IsModified = false;
		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException)
		{
			if (!IntermediairesIndirectsExists(id))
			{
				return NotFound();
			}
			throw;
		}
		return NoContent();
	}

	[HttpPost]
	public async Task<ActionResult<IntermediairesIndirects>> PostIntermediairesIndirects(IntermediairesIndirects intermediairesIndirects)
	{
		_context.IntermediairesIndirects.Add(intermediairesIndirects);
		await _context.SaveChangesAsync();
		return CreatedAtAction("GetIntermediairesIndirects", new
		{
			id = intermediairesIndirects.I2RefIntermIndirect
		}, intermediairesIndirects);
	}

	[HttpDelete("{id}")]
	public async Task<ActionResult<IntermediairesIndirects>> DeleteIntermediairesIndirects(uint id)
	{
		IntermediairesIndirects intermediairesIndirects = await _context.IntermediairesIndirects.FindAsync(id);
		if (intermediairesIndirects == null)
		{
			return NotFound();
		}
		_context.IntermediairesIndirects.Remove(intermediairesIndirects);
		await _context.SaveChangesAsync();
		return intermediairesIndirects;
	}

	private bool IntermediairesIndirectsExists(uint id)
	{
		return _context.IntermediairesIndirects.Any((IntermediairesIndirects e) => e.I2RefIntermIndirect == id);
	}
}
