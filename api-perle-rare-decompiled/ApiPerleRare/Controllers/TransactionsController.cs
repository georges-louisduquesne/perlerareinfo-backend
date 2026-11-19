using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Application.Transactions;
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
[Authorize]
public class TransactionsController : ControllerBase
{
	private const int MaxResults = 500;
	private readonly ApplicationDbContext _context;

	public TransactionsController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet("by-address")]
	public async Task<IActionResult> GetByAddress(
		[FromQuery] string cp = null,
		[FromQuery] string street = null,
		[FromQuery] string housenumber = null,
		[FromQuery] string parcelle = null,
		[FromQuery] bool includeParcelle = false)
	{
		string cleanCp = new string((cp ?? string.Empty).Where(char.IsDigit).ToArray());
		string streetNorm = TransactionValuationLogic.NormalizeStreet(street);
		IReadOnlyList<string> numVariants = TransactionValuationLogic.HouseNumberVariants(housenumber);
		string cleanParcelle = (parcelle ?? string.Empty).Trim().ToUpperInvariant();

		bool hasAddress = cleanCp.Length == 5 && streetNorm.Length > 0 && numVariants.Count > 0;
		bool hasParcelle = includeParcelle && cleanParcelle.Length == 14;
		if (!hasAddress && !hasParcelle)
		{
			return Ok(new
			{
				items = Array.Empty<TransactionListingItemDto>(),
				total = 0,
				tableReady = true,
				referenceQuarter = (string)null,
			});
		}

		List<TransactionImmo> rows;
		try
		{
			IQueryable<TransactionImmo> query = _context.Transactions.AsNoTracking();
			if (hasAddress && hasParcelle)
			{
				query = query.Where((t) =>
					(t.TCp == cleanCp && t.TAdresseRueNorm == streetNorm && numVariants.Contains(t.TAdresseNum))
					|| t.TIdParcelle == cleanParcelle);
			}
			else if (hasAddress)
			{
				query = query.Where((t) =>
					t.TCp == cleanCp && t.TAdresseRueNorm == streetNorm && numVariants.Contains(t.TAdresseNum));
			}
			else
			{
				query = query.Where((t) => t.TIdParcelle == cleanParcelle);
			}

			rows = await query
				.OrderByDescending((t) => t.TDateTransaction)
				.ThenByDescending((t) => t.TRef)
				.Take(MaxResults)
				.ToListAsync();
		}
		catch (MySqlException ex) when (ex.Number == 1146)
		{
			return Ok(new
			{
				items = Array.Empty<TransactionListingItemDto>(),
				total = 0,
				tableReady = false,
				referenceQuarter = (string)null,
			});
		}

		var cpSet = new HashSet<uint>();
		if (uint.TryParse(cleanCp, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint requestedCp))
		{
			cpSet.Add(requestedCp);
		}
		foreach (TransactionImmo row in rows)
		{
			if (uint.TryParse(row.TCp, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint rowCp))
			{
				cpSet.Add(rowCp);
			}
		}

		List<PrixMoyenPoint> history = cpSet.Count == 0
			? new List<PrixMoyenPoint>()
			: await _context.PrixMoyens.AsNoTracking()
				.Where((pm) => cpSet.Contains(pm.PmCp))
				.Select((pm) => new PrixMoyenPoint((int)pm.PmCp, pm.PmAnnee, pm.PmTrim, (int)pm.PmPrixMoyen))
				.ToListAsync();

		List<TransactionListingItemDto> items = rows
			.Select((tx) => TransactionValuationLogic.MapToDto(tx, history))
			.ToList();

		string referenceQuarter = items.Select((i) => i.ReferenceQuarter).FirstOrDefault((q) => !string.IsNullOrEmpty(q));
		if (referenceQuarter == null && history.Count > 0)
		{
			PrixMoyenPoint latest = history
				.Where((h) => TransactionValuationLogic.ParseQuarterNumber(h.Trim) > 0)
				.OrderByDescending((h) => TransactionValuationLogic.QuarterOrdinal(h.Annee, TransactionValuationLogic.ParseQuarterNumber(h.Trim)))
				.FirstOrDefault();
			if (latest != null)
			{
				referenceQuarter = $"{latest.Trim.ToUpperInvariant()} {latest.Annee}";
			}
		}

		return Ok(new
		{
			items,
			total = items.Count,
			tableReady = true,
			referenceQuarter,
		});
	}
}
