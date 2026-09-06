using System.Threading.Tasks;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Mvc;

namespace ApiPerleRare.Controllers;

public class TagsAnnoncesController : QueryEntitiesController<TagsAnnonce>
{
	private readonly ApplicationDbContext _context;

	public TagsAnnoncesController(IQueryEntitiesUseCase<TagsAnnonce> query, ApplicationDbContext context)
		: base(query)
	{
		_context = context;
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<TagsAnnonce>> GetTagsAnnonce(uint id)
	{
		TagsAnnonce tagsAnnonce = await _context.TagsAnnonce.FindAsync(id);
		if (tagsAnnonce == null)
		{
			return NotFound();
		}
		return tagsAnnonce;
	}
}
