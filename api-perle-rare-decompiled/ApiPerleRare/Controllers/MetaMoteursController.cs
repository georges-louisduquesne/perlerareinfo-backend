using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MetaMoteursController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public MetaMoteursController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<SelectResult<MetaMoteurs>>> GetMetaMoteurs([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		DbSet<MetaMoteurs> query = _context.MetaMoteurs;
		return await EFHelper<MetaMoteurs>.Select(query, where, orderby, take, skip, select);
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<MetaMoteurs>> GetMetaMoteurs(int id)
	{
		MetaMoteurs metaMoteurs = await _context.MetaMoteurs.FindAsync(id);
		if (metaMoteurs == null)
		{
			return NotFound();
		}
		return metaMoteurs;
	}
}
