using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Application.Modeles;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ModelesCommerciauxesController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public ModelesCommerciauxesController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<SelectResult<ModelesCommerciaux>>> GetModelesCommerciaux([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		DbSet<ModelesCommerciaux> query = _context.ModelesCommerciaux;
		return await EFHelper<ModelesCommerciaux>.Select(query, where, orderby, take, skip, select);
	}

	/// <summary>Liste sans les fichiers : une ligne par modèle classé, comme la page PHP.</summary>
	[HttpGet("catalogue")]
	public async Task<ActionResult<List<ModeleCatalogueRow>>> Catalogue()
	{
		List<ModeleCatalogueRow> rows = new List<ModeleCatalogueRow>();
		DbConnection conn = _context.Database.GetDbConnection();
		bool openedHere = conn.State != ConnectionState.Open;
		if (openedHere)
		{
			await conn.OpenAsync();
		}
		try
		{
			using DbCommand cmd = conn.CreateCommand();
			cmd.CommandText = ModeleCatalogueQuery.Sql;
			using DbDataReader reader = await cmd.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				rows.Add(ModeleCatalogueQuery.FromValues(
					reader.GetValue(0),
					reader.GetValue(1),
					reader.GetValue(2),
					reader.GetValue(3),
					reader.GetValue(4),
					reader.GetValue(5),
					reader.GetValue(6)));
			}
		}
		finally
		{
			if (openedHere)
			{
				await conn.CloseAsync();
			}
		}
		return rows;
	}

	[HttpGet("{id:int}/fichier/{kind}")]
	public async Task<IActionResult> Fichier(int id, string kind)
	{
		if (!ModeleFichierNames.TryResolve(kind, out bool pdf))
		{
			return NotFound();
		}
		ModelesCommerciaux row = await _context.ModelesCommerciaux.AsNoTracking()
			.FirstOrDefaultAsync((ModelesCommerciaux m) => m.McRef == id);
		if (row == null)
		{
			return NotFound();
		}
		byte[] bytes = pdf ? row.McFichierPdf : row.McFichierDoc;
		if (bytes == null || bytes.Length == 0)
		{
			return NotFound();
		}
		return File(bytes, ModeleFichierNames.ContentType(pdf), ModeleFichierNames.DownloadName(row.McTitreAbr, pdf));
	}

	[HttpGet("{id:int}")]
	public async Task<ActionResult<ModelesCommerciaux>> GetModelesCommerciaux(int id)
	{
		ModelesCommerciaux modelesCommerciaux = await _context.ModelesCommerciaux.FindAsync(id);
		if (modelesCommerciaux == null)
		{
			return NotFound();
		}
		return modelesCommerciaux;
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> PutModelesCommerciaux(int id, ModelesCommerciaux modelesCommerciaux)
	{
		if (id != modelesCommerciaux.McRef)
		{
			return BadRequest();
		}
		_context.Entry(modelesCommerciaux).State = EntityState.Modified;
		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException)
		{
			if (!ModelesCommerciauxExists(id))
			{
				return NotFound();
			}
			throw;
		}
		return NoContent();
	}

	[HttpPost]
	public async Task<ActionResult<ModelesCommerciaux>> PostModelesCommerciaux(ModelesCommerciaux modelesCommerciaux)
	{
		_context.ModelesCommerciaux.Add(modelesCommerciaux);
		await _context.SaveChangesAsync();
		return CreatedAtAction("GetModelesCommerciaux", new
		{
			id = modelesCommerciaux.McRef
		}, modelesCommerciaux);
	}

	[HttpDelete("{id}")]
	public async Task<ActionResult<ModelesCommerciaux>> DeleteModelesCommerciaux(int id)
	{
		ModelesCommerciaux modelesCommerciaux = await _context.ModelesCommerciaux.FindAsync(id);
		if (modelesCommerciaux == null)
		{
			return NotFound();
		}
		_context.ModelesCommerciaux.Remove(modelesCommerciaux);
		await _context.SaveChangesAsync();
		return modelesCommerciaux;
	}

	private bool ModelesCommerciauxExists(int id)
	{
		return _context.ModelesCommerciaux.Any((ModelesCommerciaux e) => e.McRef == id);
	}
}
