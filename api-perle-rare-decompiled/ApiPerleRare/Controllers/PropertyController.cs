using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Controllers;

/// <summary>
/// Yanport listings used by the CRM Métamoteur Recherche tab. Route frozen as <c>/api/Property</c>.
/// </summary>
public class PropertyController : QueryEntitiesController<Property>
{
	private readonly ApplicationDbContext _context;

	public PropertyController(IQueryEntitiesUseCase<Property> query, ApplicationDbContext context)
		: base(query)
	{
		_context = context;
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<Property>> GetProperty(string id)
	{
		Property row = await _context.Property.FindAsync(id);
		if (row == null)
		{
			return NotFound();
		}
		EncodingHelper.FixEncodingInStringProperties(row);
		return row;
	}

	/// <summary>
	/// Autres biens actifs appartenant au même groupe multi-agences (<c>P_DoublonPropertyId</c>)
	/// que le bien demandé (excluant le bien lui-même).
	/// </summary>
	[HttpGet("{id}/doublons")]
	public async Task<ActionResult<List<Property>>> GetPropertyDoublons(string id)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			return new List<Property>();
		}
		string trimmedId = id.Trim();
		Property row = await _context.Property.AsNoTracking().FirstOrDefaultAsync((Property p) => p.PPropertyId == trimmedId);
		if (row == null)
		{
			return NotFound();
		}
		string groupKey = string.IsNullOrWhiteSpace(row.PDoublonPropertyId)
			? row.PPropertyId
			: row.PDoublonPropertyId.Trim();

		List<Property> siblings = await _context.Property
			.AsNoTracking()
			.Where((Property p) =>
				p.PPropertyId != trimmedId
				&& p.PState == 1
				&& p.PDateFin == null
				&& (p.PPropertyId == groupKey || p.PDoublonPropertyId == groupKey))
			.OrderBy((Property p) => p.PPrix)
			.ThenBy((Property p) => p.PDateDebut)
			.ToListAsync();

		foreach (Property s in siblings)
		{
			EncodingHelper.FixEncodingInStringProperties(s);
		}
		return siblings;
	}
}
