using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare;
using ApiPerleRare.Application.Monitoring;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[EnableCors]
[ApiController]
[Authorize]
public class HomePageLoadController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public HomePageLoadController(ApplicationDbContext context)
	{
		_context = context;
	}

	public class ReportBody
	{
		public int DurationMs { get; set; }
	}

	[HttpPost]
	public async Task<IActionResult> Report([FromBody] ReportBody body)
	{
		if (body == null)
		{
			return BadRequest();
		}
		int userId = this.GetUserId();
		if (userId <= 0)
		{
			return Unauthorized();
		}
		int durationMs = HomePageLoadMetrics.ClampDurationMs(body.DurationMs);
		DateTime measuredAt = DateTime.UtcNow;
		_context.HomePageLoads.Add(new HomePageLoads
		{
			HplRefConseiller = (uint)userId,
			HplDurationMs = durationMs,
			HplMeasuredAt = measuredAt,
		});
		await _context.SaveChangesAsync();
		await PruneOldRowsAsync();
		return Ok(new { durationMs, measuredAt });
	}

	[HttpGet("cockpit")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> Cockpit()
	{
		double averageMs = await _context.HomePageLoads.AsNoTracking()
			.AverageAsync((HomePageLoads row) => (double?)row.HplDurationMs) ?? 0;
		int sampleCount = await _context.HomePageLoads.AsNoTracking().CountAsync();
		var recent = await (from row in _context.HomePageLoads.AsNoTracking()
			join cp in _context.ConseillersPersonnels.AsNoTracking()
				on row.HplRefConseiller equals cp.CpRefConseiller into people
			from cp in people.DefaultIfEmpty()
			orderby row.HplRef descending
			select new
			{
				row.HplDurationMs,
				row.HplMeasuredAt,
				cp.CpPrenom,
				cp.CpNomFamille,
			}).Take(HomePageLoadMetrics.CockpitRecentCount).ToListAsync();
		return Ok(new
		{
			averageMs = Math.Round(averageMs, 1),
			sampleCount,
			recent = recent.Select((row) => new
			{
				durationMs = row.HplDurationMs,
				measuredAt = row.HplMeasuredAt,
				userLabel = HomePageLoadMetrics.FormatPrenomNom(row.CpPrenom, row.CpNomFamille),
			}),
		});
	}

	private async Task PruneOldRowsAsync()
	{
		List<HomePageLoads> stale = await _context.HomePageLoads
			.OrderByDescending((HomePageLoads row) => row.HplRef)
			.Skip(HomePageLoadMetrics.RetentionCount)
			.ToListAsync();
		if (stale.Count == 0)
		{
			return;
		}
		_context.HomePageLoads.RemoveRange(stale);
		await _context.SaveChangesAsync();
	}
}
