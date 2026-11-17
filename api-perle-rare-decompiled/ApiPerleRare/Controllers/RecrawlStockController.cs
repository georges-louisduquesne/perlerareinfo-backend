using ApiPerleRare.Application.Monitoring;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[EnableCors]
[ApiController]
[Authorize(Roles = "Admin")]
public class RecrawlStockController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public RecrawlStockController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public IActionResult Get()
	{
		MySqlConnection connection = (MySqlConnection)_context.Database.GetDbConnection();
		RecrawlStockQuery.Counts counts = RecrawlStockQuery.Read(connection);
		return Ok(new
		{
			over90 = counts.Over90,
			between30And90 = counts.Between30And90,
		});
	}
}
