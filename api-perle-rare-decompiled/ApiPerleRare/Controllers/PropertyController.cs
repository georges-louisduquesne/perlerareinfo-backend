using System.Threading.Tasks;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Mvc;

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
}
