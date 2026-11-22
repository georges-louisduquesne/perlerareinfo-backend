using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using ApiPerleRare.ScheduledTasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace ApiPerleRare.Services;

/// <summary>
/// Maintenance quotidienne unifiée et économique Yanport (Lot 1 fantômes + Lot 2 doublons) en une seule passe API.
/// Activée uniquement par <c>PR_YANPORT_DAILY_MAINTENANCE=on</c>, indépendante de <c>PR_SCHEDULED_TASKS</c>.
/// La démo reste en <c>PR_LOCAL_SAFE</c> et ne l'enregistre pas.
/// </summary>
public class YanportDailyMaintenanceHostedService : IHostedService, IDisposable
{
	private readonly ILogger<YanportDailyMaintenanceHostedService> _logger;

	private readonly IServiceScopeFactory _serviceScopeFactory;

	private readonly IYanportService _yanportService;

	private Timer _timer;

	private int _running;

	public YanportDailyMaintenanceHostedService(
		ILogger<YanportDailyMaintenanceHostedService> logger,
		IServiceScopeFactory serviceScopeFactory,
		IYanportService yanportService)
	{
		_logger = logger;
		_serviceScopeFactory = serviceScopeFactory;
		_yanportService = yanportService;
	}

	public Task StartAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation(
			"YanportDailyMaintenance actif (mode éco unifié, contrôle toutes les 30 min, exécution quotidienne après {Hour} h heure de Paris, plafond {Cap} biens/jour).",
			YanportDailyMaintenance.RunHourParis,
			YanportDailyMaintenance.MaxTargetsPerRun);
		_timer = new Timer(Tick, null, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(30));
		return Task.CompletedTask;
	}

	private void Tick(object state)
	{
		if (Startup.IsLocalSafe)
		{
			return;
		}
		if (Interlocked.Exchange(ref _running, 1) == 1)
		{
			return;
		}
		try
		{
			RunIfDue();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "YanportDailyMaintenance");
		}
		finally
		{
			Interlocked.Exchange(ref _running, 0);
		}
	}

	private void RunIfDue()
	{
		if (_yanportService == null || !_yanportService.IsConfigure)
		{
			_logger.LogWarning("YanportDailyMaintenance : token Yanport absent, exécution ignorée.");
			return;
		}

		DateTime utcStart = DateTime.UtcNow;
		DateTime parisStart = YanportDailyMaintenance.ToParisTime(utcStart);
		List<YanportDailyMaintenance.ActiveRow> actives;

		using (IServiceScope readScope = _serviceScopeFactory.CreateScope())
		using (ApplicationDbContext db = readScope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
		{
			int? lastDay = ReadCursor(db);
			if (!YanportDailyMaintenance.Due(lastDay, utcStart))
			{
				return;
			}
			actives = LoadActiveRows(db);
		}

		Stopwatch sw = Stopwatch.StartNew();
		YanportDailyMaintenance.TargetSelection selection = YanportDailyMaintenance.SelectTargetsForDay(actives, parisStart);

		List<YanportGhostCleanup.CloseDecision> toCloseNotFound = new List<YanportGhostCleanup.CloseDecision>();
		List<YanportGhostCleanup.CloseDecision> toCloseEnded = new List<YanportGhostCleanup.CloseDecision>();
		HashSet<string> queriedIds = new HashSet<string>(StringComparer.Ordinal);
		Dictionary<string, IReadOnlyList<string>> relatedById = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

		int totalBatches = 0;
		int failedBatches = 0;
		int checkedCount = 0;
		int stillActive = 0;
		int skippedRecent404 = 0;

		for (int offset = 0; offset < selection.Targets.Count; offset += YanportDailyMaintenance.BatchSize)
		{
			int count = Math.Min(YanportDailyMaintenance.BatchSize, selection.Targets.Count - offset);
			List<YanportDailyMaintenance.ActiveRow> batch = selection.Targets.GetRange(offset, count);
			totalBatches++;

			if (!TryFetchBatchWithRetry(
				batch,
				out Dictionary<string, YanportGhostCleanup.YanportStatus> foundStatus,
				out Dictionary<string, IReadOnlyList<string>> foundRelated))
			{
				failedBatches++;
				continue;
			}

			checkedCount += batch.Count;
			List<YanportGhostCleanup.Candidate> candidates = new List<YanportGhostCleanup.Candidate>(batch.Count);
			foreach (YanportDailyMaintenance.ActiveRow row in batch)
			{
				queriedIds.Add(row.PropertyId);
				candidates.Add(new YanportGhostCleanup.Candidate(row.PropertyId, row.DateLastUpdate));
			}

			YanportGhostCleanup.BatchClassification classified = YanportGhostCleanup.ClassifyBatch(candidates, foundStatus, parisStart);
			stillActive += classified.StillActive;
			skippedRecent404 += classified.SkippedRecent404;
			toCloseNotFound.AddRange(classified.ToCloseNotFound);
			toCloseEnded.AddRange(classified.ToCloseEnded);

			foreach (KeyValuePair<string, IReadOnlyList<string>> kv in foundRelated)
			{
				relatedById[kv.Key] = kv.Value;
			}
		}

		int totalNotFoundIncludingRecent = toCloseNotFound.Count + skippedRecent404;
		bool ghostAnomaly = YanportGhostCleanup.ShouldAbortForAnomaly(checkedCount, totalNotFoundIncludingRecent);
		bool batchRateOk = YanportGhostCleanup.BatchSuccessRateAcceptable(totalBatches, failedBatches);

		HashSet<string> closedIds = new HashSet<string>(StringComparer.Ordinal);
		int closedDbCount = 0;
		List<YanportGhostCleanup.CloseDecision> allClosures = new List<YanportGhostCleanup.CloseDecision>(toCloseNotFound.Count + toCloseEnded.Count);
		if (!ghostAnomaly && batchRateOk)
		{
			allClosures.AddRange(toCloseNotFound);
			allClosures.AddRange(toCloseEnded);
			foreach (YanportGhostCleanup.CloseDecision d in allClosures)
			{
				closedIds.Add(d.PropertyId);
			}
		}

		YanportDoublonSync.SyncPlan doublonPlan = batchRateOk
			? YanportDailyMaintenance.ComputeIncrementalDoublonPlan(actives, closedIds, queriedIds, relatedById)
			: new YanportDoublonSync.SyncPlan();

		int plannedDoublonUpdates = doublonPlan.ToLink.Count + doublonPlan.ToClear.Count;
		bool doublonAnomaly = YanportDailyMaintenance.ShouldAbortDoublonUpdates(plannedDoublonUpdates);
		int appliedDoublonUpdates = 0;

		using (IServiceScope writeScope = _serviceScopeFactory.CreateScope())
		using (ApplicationDbContext db = writeScope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
		{
			if (!ghostAnomaly && batchRateOk && allClosures.Count > 0)
			{
				closedDbCount = ApplyClosures(db, allClosures);
			}
			if (batchRateOk && !doublonAnomaly && plannedDoublonUpdates > 0)
			{
				List<YanportDoublonSync.DoublonUpdate> allUpdates = new List<YanportDoublonSync.DoublonUpdate>(plannedDoublonUpdates);
				allUpdates.AddRange(doublonPlan.ToLink);
				allUpdates.AddRange(doublonPlan.ToClear);
				appliedDoublonUpdates = ApplyDoublonUpdates(db, allUpdates);
			}

			if (batchRateOk && !ghostAnomaly && !doublonAnomaly)
			{
				WriteCursor(db, YanportDailyMaintenance.DayKey(utcStart));
			}

			string summary = string.Format(
				"actifs={0}, cibles={1} (nouveaux={2}, tranche14j={3}/{4}, slot={5}, plafondEcarte={6}), lots={7} (echecs={8}), "
				+ "fantomes: encoreActifs={9}, 404RecentsIgnores={10}, fermes={11} (404={12}, closYanport={13}, anomalie={14}); "
				+ "doublons: groupes={15}, totalDoublons={16}, lies={17}, delies={18}, appliques={19}, anomalie={20}, duree={21:F1}s",
				actives.Count,
				selection.Targets.Count,
				selection.NewCount,
				selection.StaleSliceCount,
				selection.StaleEligibleCount,
				selection.DaySlot,
				selection.CappedCount,
				totalBatches,
				failedBatches,
				stillActive,
				skippedRecent404,
				closedDbCount,
				toCloseNotFound.Count,
				toCloseEnded.Count,
				ghostAnomaly,
				doublonPlan.ClustersCount,
				doublonPlan.TotalDuplicatesInClusters,
				doublonPlan.ToLink.Count,
				doublonPlan.ToClear.Count,
				appliedDoublonUpdates,
				doublonAnomaly,
				sw.Elapsed.TotalSeconds);

			WriteTaskLog(db, parisStart, summary);
			_logger.LogInformation("YanportDailyMaintenance terminé : {Summary}", summary);
		}
	}

	private bool TryFetchBatchWithRetry(
		List<YanportDailyMaintenance.ActiveRow> batch,
		out Dictionary<string, YanportGhostCleanup.YanportStatus> foundStatus,
		out Dictionary<string, IReadOnlyList<string>> foundRelated)
	{
		foundStatus = null;
		foundRelated = null;
		List<string> ids = batch.Select((YanportDailyMaintenance.ActiveRow r) => r.PropertyId).ToList();
		string url = YanportGhostCleanup.BuildBatchUrl(ids);

		for (int attempt = 0; attempt < 3; attempt++)
		{
			try
			{
				string raw = _yanportService.Get(0, url);
				if (YanportGhostCleanup.TryParseBatchResponse(raw, out foundStatus)
					&& YanportDoublonSync.TryParseBatchRelatedIds(raw, out foundRelated))
				{
					return true;
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "YanportDailyMaintenance : erreur lot (tentative {Attempt}/3)", attempt + 1);
			}
			if (attempt < 2)
			{
				Thread.Sleep(500 * (attempt + 1));
			}
		}
		return false;
	}

	private static List<YanportDailyMaintenance.ActiveRow> LoadActiveRows(ApplicationDbContext db)
	{
		List<YanportDailyMaintenance.ActiveRow> result = new List<YanportDailyMaintenance.ActiveRow>();
		var conn = db.Database.GetDbConnection();
		if (conn.State != ConnectionState.Open)
		{
			conn.Open();
		}
		using var cmd = conn.CreateCommand();
		cmd.CommandText = YanportDailyMaintenance.SelectActiveSql;
		cmd.CommandTimeout = 120;
		using var reader = cmd.ExecuteReader();
		while (reader.Read())
		{
			string id = reader.IsDBNull(0) ? null : reader.GetString(0)?.Trim();
			if (string.IsNullOrEmpty(id))
			{
				continue;
			}
			string tx = reader.IsDBNull(1) ? null : reader.GetString(1)?.Trim();
			DateTime? debut = reader.IsDBNull(2) ? null : reader.GetDateTime(2);
			DateTime? crea = reader.IsDBNull(3) ? null : reader.GetDateTime(3);
			DateTime? lastUpd = reader.IsDBNull(4) ? null : reader.GetDateTime(4);
			string doublon = reader.IsDBNull(5) ? null : reader.GetString(5)?.Trim();
			result.Add(new YanportDailyMaintenance.ActiveRow(id, tx, debut, crea, lastUpd, doublon));
		}
		return result;
	}

	private static int ApplyClosures(ApplicationDbContext db, IReadOnlyList<YanportGhostCleanup.CloseDecision> decisions)
	{
		if (decisions.Count == 0)
		{
			return 0;
		}
		int updated = 0;
		var conn = db.Database.GetDbConnection();
		if (conn.State != ConnectionState.Open)
		{
			conn.Open();
		}
		using var tx = conn.BeginTransaction();
		try
		{
			using var cmd = conn.CreateCommand();
			cmd.Transaction = tx;
			cmd.CommandText = YanportGhostCleanup.CloseOneSql;
			var pId = new MySqlParameter("@id", MySqlDbType.VarChar, 40);
			var pDate = new MySqlParameter("@dateFin", MySqlDbType.DateTime);
			cmd.Parameters.Add(pId);
			cmd.Parameters.Add(pDate);

			foreach (YanportGhostCleanup.CloseDecision d in decisions)
			{
				pId.Value = d.PropertyId;
				pDate.Value = d.DateFinParis;
				updated += cmd.ExecuteNonQuery();
			}
			tx.Commit();
		}
		catch
		{
			tx.Rollback();
			throw;
		}
		return updated;
	}

	private static int ApplyDoublonUpdates(ApplicationDbContext db, IReadOnlyList<YanportDoublonSync.DoublonUpdate> updates)
	{
		if (updates.Count == 0)
		{
			return 0;
		}
		int affected = 0;
		var conn = db.Database.GetDbConnection();
		if (conn.State != ConnectionState.Open)
		{
			conn.Open();
		}
		using var tx = conn.BeginTransaction();
		try
		{
			using var cmd = conn.CreateCommand();
			cmd.Transaction = tx;
			cmd.CommandText = YanportDoublonSync.UpdateDoublonSql;
			var pId = new MySqlParameter("@id", MySqlDbType.VarChar, 40);
			var pDoublon = new MySqlParameter("@doublonId", MySqlDbType.VarChar, 40);
			cmd.Parameters.Add(pId);
			cmd.Parameters.Add(pDoublon);

			foreach (YanportDoublonSync.DoublonUpdate u in updates)
			{
				pId.Value = u.PropertyId;
				pDoublon.Value = string.IsNullOrEmpty(u.TargetDoublonPropertyId) ? DBNull.Value : u.TargetDoublonPropertyId;
				affected += cmd.ExecuteNonQuery();
			}
			tx.Commit();
		}
		catch
		{
			tx.Rollback();
			throw;
		}
		return affected;
	}

	private static int? ReadCursor(ApplicationDbContext db)
	{
		AdminVersions row = db.AdminVersions.Find(YanportDailyMaintenance.CursorParam);
		return row?.AvVersion;
	}

	private static void WriteCursor(ApplicationDbContext db, int dayKey)
	{
		AdminVersions row = db.AdminVersions.Find(YanportDailyMaintenance.CursorParam);
		if (row == null)
		{
			db.AdminVersions.Add(new AdminVersions
			{
				AvParamName = YanportDailyMaintenance.CursorParam,
				AvVersion = dayKey
			});
		}
		else
		{
			row.AvVersion = dayKey;
		}
		db.SaveChanges();
	}

	private void WriteTaskLog(ApplicationDbContext db, DateTime parisStart, string message)
	{
		try
		{
			db.ScheduledTaskLogs.Add(new ScheduledTaskLogs
			{
				StlNom = YanportDailyMaintenance.TaskLogName,
				StlDateDebut = parisStart,
				StlDateFin = YanportDailyMaintenance.ToParisTime(DateTime.UtcNow),
				StlResults = message != null && message.Length > 1000 ? message.Substring(0, 1000) : message
			});
			db.SaveChanges();
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "YanportDailyMaintenance : impossible d'écrire dans scheduled_task_logs");
		}
	}

	public Task StopAsync(CancellationToken stoppingToken)
	{
		_timer?.Change(Timeout.Infinite, 0);
		return Task.CompletedTask;
	}

	public void Dispose()
	{
		_timer?.Dispose();
	}
}
