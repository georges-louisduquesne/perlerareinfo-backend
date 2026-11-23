using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ApiPerleRare.Application.Doublons;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using ApiPerleRare.ScheduledTasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace ApiPerleRare.Controllers;

/// <summary>
/// Outil Admin (Lot 3) de revue côte à côte des paires de biens étonnamment dédoublonnées :
/// - GET /api/DoublonReviews/next : retourne la prochaine paire suspecte non encore revue + compteurs.
/// - POST /api/DoublonReviews/decide : enregistre "confirmed" (doublon) ou "rejected" (pas doublon,
///   délie P_DoublonPropertyId et bloque tout re-lien ultérieur par YanportDailyMaintenance).
/// </summary>
[Route("api/[controller]")]
[EnableCors]
[ApiController]
[Authorize(Roles = "Admin")]
public class DoublonReviewsController : ControllerBase
{
	public sealed class DecisionRequest
	{
		public string PropertyIdA { get; set; }

		public string PropertyIdB { get; set; }

		public string Decision { get; set; }

		public string Reason { get; set; }
	}

	private readonly ApplicationDbContext _context;

	public DoublonReviewsController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet("next")]
	public async Task<IActionResult> GetNext([FromQuery] int skip = 0)
	{
		object payload = await BuildNextPayloadAsync(Math.Max(0, skip));
		return Ok(payload);
	}

	[HttpPost("decide")]
	public async Task<IActionResult> Decide([FromBody] DecisionRequest request)
	{
		if (request == null
			|| !DoublonReviewLogic.TryCanonicalizePair(request.PropertyIdA, request.PropertyIdB, out string idA, out string idB)
			|| !DoublonReviewLogic.TryNormalizeDecision(request.Decision, out string decision))
		{
			return BadRequest(new { error = "Paramètres invalides (propertyIdA, propertyIdB, decision='confirmed'|'rejected')." });
		}

		MySqlConnection conn = (MySqlConnection)_context.Database.GetDbConnection();
		if (conn.State != ConnectionState.Open)
		{
			await conn.OpenAsync();
		}

		string login = this.GetUserLogin() ?? "admin";
		DateTime parisNow = YanportDailyMaintenance.ToParisTime(DateTime.UtcNow);
		string reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
		if (reason != null && reason.Length > 120)
		{
			reason = reason.Substring(0, 120);
		}

		try
		{
			using MySqlTransaction tx = await conn.BeginTransactionAsync();
			try
			{
				using (MySqlCommand cmd = conn.CreateCommand())
				{
					cmd.Transaction = tx;
					cmd.CommandText = DoublonReviewLogic.UpsertReviewSql;
					cmd.Parameters.AddWithValue("@idA", idA);
					cmd.Parameters.AddWithValue("@idB", idB);
					cmd.Parameters.AddWithValue("@decision", decision);
					cmd.Parameters.AddWithValue("@reason", (object)reason ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@reviewedBy", login);
					cmd.Parameters.AddWithValue("@reviewedAt", parisNow);
					await cmd.ExecuteNonQueryAsync();
				}

				if (decision == DoublonReviewLogic.DecisionRejected)
				{
					using MySqlCommand unlinkCmd = conn.CreateCommand();
					unlinkCmd.Transaction = tx;
					unlinkCmd.CommandText = DoublonReviewLogic.UnlinkRejectedPairSql;
					unlinkCmd.Parameters.AddWithValue("@idA", idA);
					unlinkCmd.Parameters.AddWithValue("@idB", idB);
					await unlinkCmd.ExecuteNonQueryAsync();
				}

				await tx.CommitAsync();
			}
			catch
			{
				await tx.RollbackAsync();
				throw;
			}
		}
		catch (MySqlException ex) when (ex.Number == 1146)
		{
			return StatusCode(503, new
			{
				error = "Table property_doublon_review absente : exécuter infra/ovh/sql/20261123-property-doublon-review.sql sur MariaDB."
			});
		}

		object next = await BuildNextPayloadAsync(0);
		return Ok(next);
	}

	private async Task<object> BuildNextPayloadAsync(int skip)
	{
		MySqlConnection conn = (MySqlConnection)_context.Database.GetDbConnection();
		if (conn.State != ConnectionState.Open)
		{
			await conn.OpenAsync();
		}

		HashSet<string> reviewedKeys = new HashSet<string>(StringComparer.Ordinal);
		int confirmedCount = 0;
		int rejectedCount = 0;
		bool tableReady = true;

		try
		{
			using MySqlCommand revCmd = conn.CreateCommand();
			revCmd.CommandText = DoublonReviewLogic.SelectAllReviewsSql;
			using MySqlDataReader r = await revCmd.ExecuteReaderAsync();
			while (await r.ReadAsync())
			{
				string a = r.IsDBNull(0) ? null : r.GetString(0)?.Trim();
				string b = r.IsDBNull(1) ? null : r.GetString(1)?.Trim();
				string dec = r.IsDBNull(2) ? null : r.GetString(2)?.Trim();
				string key = DoublonReviewLogic.CanonicalPairKey(a, b);
				if (!string.IsNullOrEmpty(key))
				{
					reviewedKeys.Add(key);
				}
				if (string.Equals(dec, DoublonReviewLogic.DecisionConfirmed, StringComparison.OrdinalIgnoreCase))
				{
					confirmedCount++;
				}
				else if (string.Equals(dec, DoublonReviewLogic.DecisionRejected, StringComparison.OrdinalIgnoreCase))
				{
					rejectedCount++;
				}
			}
		}
		catch (MySqlException ex) when (ex.Number == 1146)
		{
			tableReady = false;
		}

		List<DoublonReviewLogic.SuspiciousCandidate> candidates = new List<DoublonReviewLogic.SuspiciousCandidate>();
		using (MySqlCommand candCmd = conn.CreateCommand())
		{
			candCmd.CommandText = DoublonReviewLogic.SelectSuspiciousPairsBaseSql;
			candCmd.CommandTimeout = 60;
			using MySqlDataReader r = await candCmd.ExecuteReaderAsync();
			while (await r.ReadAsync())
			{
				candidates.Add(new DoublonReviewLogic.SuspiciousCandidate(
					DupId: r.IsDBNull(0) ? null : r.GetString(0),
					MasterId: r.IsDBNull(1) ? null : r.GetString(1),
					DupSurface: r.IsDBNull(2) ? (double?)null : Convert.ToDouble(r.GetValue(2)),
					MasterSurface: r.IsDBNull(3) ? (double?)null : Convert.ToDouble(r.GetValue(3)),
					DupPrix: r.IsDBNull(4) ? (double?)null : Convert.ToDouble(r.GetValue(4)),
					MasterPrix: r.IsDBNull(5) ? (double?)null : Convert.ToDouble(r.GetValue(5)),
					DupPieces: r.IsDBNull(6) ? (int?)null : Convert.ToInt32(r.GetValue(6)),
					MasterPieces: r.IsDBNull(7) ? (int?)null : Convert.ToInt32(r.GetValue(7)),
					DupCp: r.IsDBNull(8) ? null : r.GetString(8),
					MasterCp: r.IsDBNull(9) ? null : r.GetString(9),
					DupType: r.IsDBNull(10) ? null : r.GetString(10),
					MasterType: r.IsDBNull(11) ? null : r.GetString(11)));
			}
		}

		List<DoublonReviewLogic.ScoredPair> queue = DoublonReviewLogic.BuildReviewQueue(candidates, reviewedKeys);
		int remaining = queue.Count;
		if (remaining == 0)
		{
			return new
			{
				remainingCount = 0,
				confirmedCount,
				rejectedCount,
				tableReady,
				pair = (object)null,
			};
		}

		int idx = skip % remaining;
		DoublonReviewLogic.ScoredPair selected = queue[idx];

		Property left = await _context.Property.AsNoTracking().FirstOrDefaultAsync((Property p) => p.PPropertyId == selected.MasterId);
		Property right = await _context.Property.AsNoTracking().FirstOrDefaultAsync((Property p) => p.PPropertyId == selected.DupId);
		if (left != null)
		{
			EncodingHelper.FixEncodingInStringProperties(left);
		}
		if (right != null)
		{
			EncodingHelper.FixEncodingInStringProperties(right);
		}

		return new
		{
			remainingCount = remaining,
			confirmedCount,
			rejectedCount,
			tableReady,
			currentIndex = idx,
			pair = new
			{
				propertyIdA = selected.CanonicalIdA,
				propertyIdB = selected.CanonicalIdB,
				score = selected.Score,
				reasons = selected.Reasons,
				left,
				right,
			},
		};
	}
}
