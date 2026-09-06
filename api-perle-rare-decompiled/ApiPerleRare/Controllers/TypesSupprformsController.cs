using System.Collections.Generic;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TypesSupprformsController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public TypesSupprformsController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<TypesSupprform>>> GetTypesSupprform()
	{
		return (await _context.TypesSupprform.ToListAsync());
	}
}
