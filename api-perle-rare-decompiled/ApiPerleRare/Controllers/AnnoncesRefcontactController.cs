using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AnnoncesRefcontactController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public AnnoncesRefcontactController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<SelectResult<AnnoncesRefcontact>>> GetAnnoncesRefcontact([FromQuery] long contactRef, [FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		string tableName = $"annonces_refcontact_{contactRef}";
		MySqlConnection connection = (MySqlConnection)_context.Database.GetDbConnection();
		if (connection.State != ConnectionState.Open)
		{
			await connection.OpenAsync();
		}
		return DataReaderHelper<AnnoncesRefcontact>.Select(connection, tableName, GetSqlName, where, orderby, take, skip, select);
	}

	[HttpGet]
	[Route("/api/contact/{contactRef}/annonces")]
	public async Task<ActionResult<SelectResult<AnnoncesRefcontact>>> GetAnnoncesRefcontact2(long contactRef, [FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		string tableName = $"annonces_refcontact_{contactRef}";
		MySqlConnection connection = (MySqlConnection)_context.Database.GetDbConnection();
		if (connection.State != ConnectionState.Open)
		{
			await connection.OpenAsync();
		}
		return DataReaderHelper<AnnoncesRefcontact>.Select(connection, tableName, GetSqlName, where, orderby, take, skip, select);
	}

	private string GetSqlName(string[] propNames)
	{
		string propName = propNames.Single();
		if (propName.StartsWith("ADate", StringComparison.InvariantCultureIgnoreCase))
		{
			return "A_Date_" + propName.Substring(5);
		}
		return "A_" + propName.Substring(1);
	}
}
