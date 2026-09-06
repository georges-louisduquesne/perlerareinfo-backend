using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TypesMailsController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public TypesMailsController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<SelectResult<TypesMails>>> GetTypesMails([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		return await EFHelper<TypesMails>.Select(_context.TypesMails, where, orderby, take, skip, select);
	}
}
