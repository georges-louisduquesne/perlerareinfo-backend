using System.Collections.Generic;
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
		List<ModeleCatalogueRow> rows = await _context.ModelesCommerciaux.AsNoTracking()
			.Where((ModelesCommerciaux m) => m.McCategorie != null)
			.OrderBy((ModelesCommerciaux m) => m.McCategorie)
			.ThenBy((ModelesCommerciaux m) => m.McTitre)
			.Select((ModelesCommerciaux m) => new ModeleCatalogueRow
			{
				Ref = m.McRef,
				Categorie = m.McCategorie ?? 0,
				CategorieNom = m.McCategorieNavigation.TmNom,
				Titre = m.McTitre,
				TitreAbr = m.McTitreAbr,
				HasDoc = m.McFichierDoc != null,
				HasPdf = m.McFichierPdf != null
			})
			.ToListAsync();
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
