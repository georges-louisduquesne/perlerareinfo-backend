using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TagsAnnoncesController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public TagsAnnoncesController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<SelectResult<TagsAnnonce>>> GetTagsAnnonce([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		return await EFHelper<TagsAnnonce>.Select(_context.TagsAnnonce, where, orderby, take, skip, select);
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
