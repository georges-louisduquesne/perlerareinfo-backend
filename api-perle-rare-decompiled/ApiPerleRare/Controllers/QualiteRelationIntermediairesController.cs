using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class QualiteRelationIntermediairesController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public QualiteRelationIntermediairesController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<SelectResult<QualiteRelationIntermediaire>>> GetQualiteRelationIntermediaire([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		return await EFHelper<QualiteRelationIntermediaire>.Select(_context.QualiteRelationIntermediaire, where, orderby, take, skip, select);
	}
}
