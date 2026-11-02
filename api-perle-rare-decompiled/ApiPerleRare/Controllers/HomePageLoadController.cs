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

	private sealed class LoadRow
	{
		public long Ref { get; init; }

		public int DurationMs { get; init; }

		public DateTime MeasuredAt { get; init; }

		public string Prenom { get; init; }

		public string Nom { get; init; }

		public bool IsAdmin { get; init; }
	}

	[HttpPost]
	public async Task<IActionResult> Report([FromBody] ReportBody body)
	{
		if (body == null)
		{
			return BadRequest();
		}
		if (!User.TryGetId(out int userId) || !HomePageLoadMetrics.CanRecordLoad(true, userId))
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
		List<LoadRow> rows = await LoadRowsQuery().ToListAsync();
		return Ok(new
		{
			all = BuildSegment(rows, null),
			nonAdmin = BuildSegment(rows, false),
			admin = BuildSegment(rows, true),
		});
	}

	private IQueryable<LoadRow> LoadRowsQuery()
	{
		return from row in _context.HomePageLoads.AsNoTracking()
			join cp in _context.ConseillersPersonnels.AsNoTracking()
				on row.HplRefConseiller equals cp.CpRefConseiller into people
			from cp in people.DefaultIfEmpty()
			select new LoadRow
			{
				Ref = row.HplRef,
				DurationMs = row.HplDurationMs,
				MeasuredAt = row.HplMeasuredAt,
				Prenom = cp.CpPrenom,
				Nom = cp.CpNomFamille,
				IsAdmin = cp != null && cp.CpAdmin,
			};
	}

	private static object BuildSegment(IReadOnlyList<LoadRow> rows, bool? adminFilter)
	{
		IEnumerable<LoadRow> filtered = rows;
		if (adminFilter == true)
		{
			filtered = filtered.Where((row) => row.IsAdmin);
		}
		else if (adminFilter == false)
		{
			filtered = filtered.Where((row) => !row.IsAdmin);
		}
		List<LoadRow> list = filtered.ToList();
		(double averageMs, int sampleCount) = HomePageLoadMetrics.SegmentAggregate(
			list.Select((row) => (row.DurationMs, row.IsAdmin)),
			adminFilter);
		var recent = list
			.OrderByDescending((row) => row.Ref)
			.Take(HomePageLoadMetrics.CockpitRecentCount)
			.Select(MapRecent);
		return new
		{
			averageMs = Math.Round(averageMs, 1),
			sampleCount,
			recent,
		};
	}

	private static object MapRecent(LoadRow row)
	{
		return new
		{
			durationMs = row.DurationMs,
			measuredAt = row.MeasuredAt,
			userLabel = HomePageLoadMetrics.FormatPrenomNom(row.Prenom, row.Nom),
		};
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
