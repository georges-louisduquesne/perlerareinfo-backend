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
public class HistoriqueAnnoncesController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public HistoriqueAnnoncesController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<SelectResult<HistoriqueAnnonces>>> GetHistoriqueAnnonces([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		DbSet<HistoriqueAnnonces> query = _context.HistoriqueAnnonces;
		return await EFHelper<HistoriqueAnnonces>.Select(query, where, orderby, take, skip, select);
	}
}
