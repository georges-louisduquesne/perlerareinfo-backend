using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OriginesController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public OriginesController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	[Authorize]
	public async Task<ActionResult<IEnumerable<Origines>>> GetOrigines()
	{
		string sql = "\r\nSELECT O_Nom FROM `origines` o \r\nLEFT OUTER JOIN (SELECT C_Origine, COUNT( C_Origine ) AS V_Occ FROM `contacts_recherche` GROUP BY C_Origine) p ON p.C_Origine = o.O_Nom \r\nORDER BY V_Occ DESC";
		DbConnection c = _context.Database.GetDbConnection();
		if (c.State != ConnectionState.Open)
		{
			await c.OpenAsync();
		}
		List<Origines> list = new List<Origines>();
		using (DbCommand cmd = c.CreateCommand())
		{
			cmd.CommandText = sql;
			using DbDataReader reader = await cmd.ExecuteReaderAsync();
			while (reader.Read())
			{
				list.Add(new Origines
				{
					ONom = reader.GetString(0)
				});
			}
		}
		return (ActionResult<IEnumerable<Origines>>)(IEnumerable<Origines>)list;
	}
}
