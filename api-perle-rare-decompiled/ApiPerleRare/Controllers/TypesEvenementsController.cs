using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableCors]
[Authorize]
public class TypesEvenementsController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public TypesEvenementsController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<SelectResult<TypesEvenements>>> GetTypesEvenements([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		return await EFHelper<TypesEvenements>.Select(_context.TypesEvenements, where, orderby, take, skip, select);
	}
}
