using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using ApiPerleRare.Application.Contacts;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RegistreMandatsController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	private readonly IUserSessionService _session;

	public RegistreMandatsController(ApplicationDbContext context, IUserSessionService session)
	{
		_context = context;
		_session = session;
	}

	[HttpGet]
	public async Task<ActionResult<List<RegistreMandatRow>>> Get()
	{
		if (!_session.IsAdmin)
		{
			return Forbid();
		}
		List<RegistreMandatRow> rows = new List<RegistreMandatRow>();
		DbConnection conn = _context.Database.GetDbConnection();
		bool openedHere = conn.State != ConnectionState.Open;
		if (openedHere)
		{
			await conn.OpenAsync();
		}
		try
		{
			using DbCommand cmd = conn.CreateCommand();
			cmd.CommandText = RegistreMandatsQuery.Sql;
			using DbDataReader reader = await cmd.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				RegistreMandatRow row = RegistreMandatsQuery.FromValues(
					reader.GetValue(0),
					reader.GetValue(1),
					reader.GetValue(2),
					reader.GetValue(3),
					reader.GetValue(4),
					reader.GetValue(5),
					reader.GetValue(6),
					reader.GetValue(7),
					reader.GetValue(8),
					reader.GetValue(9));
				if (row.Ref > 0)
				{
					rows.Add(row);
				}
			}
		}
		finally
		{
			if (openedHere)
			{
				await conn.CloseAsync();
			}
		}
		return rows;
	}
}
