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
public class OriginesCandidatsController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public OriginesCandidatsController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	[Authorize]
	public async Task<ActionResult<IEnumerable<OriginesCandidats>>> GetOriginesCandidats()
	{
		string sql = "\r\nSELECT OC_Nom FROM `origines_candidats`\r\nORDER BY CASE OC_Nom\r\n  WHEN 'Hugo Bolzinger' THEN 1\r\n  WHEN 'LinkedIn' THEN 2\r\n  WHEN 'Contact personnel' THEN 3\r\n  WHEN 'Autre' THEN 4\r\n  ELSE 5 END, OC_Nom";
		DbConnection c = _context.Database.GetDbConnection();
		if (c.State != ConnectionState.Open)
		{
			await c.OpenAsync();
		}
		List<OriginesCandidats> list = new List<OriginesCandidats>();
		using (DbCommand cmd = c.CreateCommand())
		{
			cmd.CommandText = sql;
			using DbDataReader reader = await cmd.ExecuteReaderAsync();
			while (reader.Read())
			{
				list.Add(new OriginesCandidats
				{
					OcNom = reader.GetString(0)
				});
			}
		}
		return list;
	}
}
