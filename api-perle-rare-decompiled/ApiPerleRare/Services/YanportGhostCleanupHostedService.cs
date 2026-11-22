using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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
/// Nettoyage quotidien des biens Yanport fantômes (HTTP 404 > 48 h) ou déjà fermés côté Yanport.
/// Activé uniquement par <c>PR_YANPORT_GHOST_CLEANUP=on</c>, indépendant de <c>PR_SCHEDULED_TASKS</c>.
/// La démo reste en <c>PR_LOCAL_SAFE</c> et ne l'enregistre pas.
/// </summary>
public class YanportGhostCleanupHostedService : IHostedService, IDisposable
{
	private readonly ILogger<YanportGhostCleanupHostedService> _logger;

	private readonly IServiceScopeFactory _serviceScopeFactory;

	private readonly IYanportService _yanportService;

	private Timer _timer;

	private int _running;

	public YanportGhostCleanupHostedService(
		ILogger<YanportGhostCleanupHostedService> logger,
		IServiceScopeFactory serviceScopeFactory,
		IYanportService yanportService)
	{
		_logger = logger;
		_serviceScopeFactory = serviceScopeFactory;
		_yanportService = yanportService;
	}

	public Task StartAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("YanportGhostCleanup actif (contrôle toutes les 30 min, exécution quotidienne après {Hour} h heure de Paris).", YanportGhostCleanup.RunHourParis);
		_timer = new Timer(Tick, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(30));
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
			_logger.LogError(ex, "YanportGhostCleanup");
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
			_logger.LogWarning("YanportGhostCleanup : token Yanport absent, exécution ignorée.");
			return;
		}

		DateTime utcStart = DateTime.UtcNow;
		DateTime parisStart = YanportGhostCleanup.ToParisTime(utcStart);
		List<YanportGhostCleanup.Candidate> candidates;

		using (IServiceScope readScope = _serviceScopeFactory.CreateScope())
		using (ApplicationDbContext db = readScope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
		{
			int? lastDay = ReadCursor(db);
			if (!YanportGhostCleanup.Due(lastDay, utcStart))
			{
				return;
			}
			candidates = LoadActiveCandidates(db);
		}

		Stopwatch sw = Stopwatch.StartNew();
		List<YanportGhostCleanup.CloseDecision> toCloseNotFound = new List<YanportGhostCleanup.CloseDecision>();
		List<YanportGhostCleanup.CloseDecision> toCloseEnded = new List<YanportGhostCleanup.CloseDecision>();
		int totalBatches = 0;
		int failedBatches = 0;
		int checkedCount = 0;
		int stillActive = 0;
		int skippedRecent404 = 0;

		for (int offset = 0; offset < candidates.Count; offset += YanportGhostCleanup.BatchSize)
		{
			int count = Math.Min(YanportGhostCleanup.BatchSize, candidates.Count - offset);
			List<YanportGhostCleanup.Candidate> batch = candidates.GetRange(offset, count);
			totalBatches++;

			if (!TryFetchBatchWithRetry(batch, out Dictionary<string, YanportGhostCleanup.YanportStatus> found))
			{
				failedBatches++;
				continue;
			}

			checkedCount += batch.Count;
			YanportGhostCleanup.BatchClassification classified = YanportGhostCleanup.ClassifyBatch(batch, found, parisStart);
			stillActive += classified.StillActive;
			skippedRecent404 += classified.SkippedRecent404;
			toCloseNotFound.AddRange(classified.ToCloseNotFound);
			toCloseEnded.AddRange(classified.ToCloseEnded);
			Thread.Sleep(150);
		}

		int totalNotFoundIncludingRecent = toCloseNotFound.Count + skippedRecent404;
		if (YanportGhostCleanup.ShouldAbortForAnomaly(checkedCount, totalNotFoundIncludingRecent))
		{
			string abortMsg = $"ABORT anomalie Yanport : {totalNotFoundIncludingRecent}/{checkedCount} absents (> {YanportGhostCleanup.MaxNotFoundRatioAbort:P0}). Aucun bien fermé.";
			_logger.LogError("YanportGhostCleanup : {Message}", abortMsg);
			RecordLog(parisStart, abortMsg);
			return;
		}

		List<YanportGhostCleanup.CloseDecision> allToClose = new List<YanportGhostCleanup.CloseDecision>(toCloseNotFound.Count + toCloseEnded.Count);
		allToClose.AddRange(toCloseNotFound);
		allToClose.AddRange(toCloseEnded);

		int updatedRows = 0;
		bool batchRateOk = YanportGhostCleanup.BatchSuccessRateAcceptable(totalBatches, failedBatches);

		using (IServiceScope writeScope = _serviceScopeFactory.CreateScope())
		using (ApplicationDbContext db = writeScope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
		{
			if (allToClose.Count > 0)
			{
				updatedRows = ApplyClosures(db, allToClose);
			}
			if (batchRateOk)
			{
				WriteCursor(db, YanportGhostCleanup.DayKey(utcStart));
			}
			sw.Stop();
			string summary =
				$"checked={checkedCount}/{candidates.Count}, active={stillActive}, closed404={toCloseNotFound.Count}, "
				+ $"closedEnded={toCloseEnded.Count}, updatedRows={updatedRows}, skippedRecent404={skippedRecent404}, "
				+ $"failedBatches={failedBatches}/{totalBatches}, elapsed={sw.Elapsed.TotalSeconds:N1}s";
			AddTaskLog(db, parisStart, summary);
			db.SaveChanges();
			_logger.LogInformation("YanportGhostCleanup terminé : {Summary}", summary);
		}
	}

	private bool TryFetchBatchWithRetry(
		List<YanportGhostCleanup.Candidate> batch,
		out Dictionary<string, YanportGhostCleanup.YanportStatus> found)
	{
		found = null;
		string[] ids = new string[batch.Count];
		for (int i = 0; i < batch.Count; i++)
		{
			ids[i] = batch[i].PropertyId;
		}
		string url = YanportGhostCleanup.BuildBatchUrl(ids);
		for (int attempt = 0; attempt < 3; attempt++)
		{
			try
			{
				string json = _yanportService.Get(0, url);
				if (YanportGhostCleanup.TryParseBatchResponse(json, out found))
				{
					return true;
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "YanportGhostCleanup : tentative {Attempt}/3 échouée sur un lot de {Count} biens.", attempt + 1, batch.Count);
			}
			if (attempt < 2)
			{
				Thread.Sleep(1500 * (attempt + 1));
			}
		}
		return false;
	}

	private static List<YanportGhostCleanup.Candidate> LoadActiveCandidates(ApplicationDbContext db)
	{
		MySqlConnection conn = (MySqlConnection)db.Database.GetDbConnection();
		if (conn.State != ConnectionState.Open)
		{
			conn.Open();
		}
		List<YanportGhostCleanup.Candidate> list = new List<YanportGhostCleanup.Candidate>();
		using MySqlCommand cmd = conn.CreateCommand();
		cmd.CommandTimeout = 60;
		cmd.CommandText = YanportGhostCleanup.SelectActiveSql;
		using MySqlDataReader reader = cmd.ExecuteReader();
		while (reader.Read())
		{
			string id = reader.GetString(0);
			DateTime? lastUpdate = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1);
			list.Add(new YanportGhostCleanup.Candidate(id, lastUpdate));
		}
		return list;
	}

	private static int ApplyClosures(ApplicationDbContext db, List<YanportGhostCleanup.CloseDecision> decisions)
	{
		MySqlConnection conn = (MySqlConnection)db.Database.GetDbConnection();
		if (conn.State != ConnectionState.Open)
		{
			conn.Open();
		}
		int updated = 0;
		using MySqlTransaction tx = conn.BeginTransaction();
		try
		{
			using MySqlCommand cmd = conn.CreateCommand();
			cmd.Transaction = tx;
			cmd.CommandText = YanportGhostCleanup.CloseOneSql;
			MySqlParameter pDateFin = cmd.Parameters.Add("@dateFin", MySqlDbType.DateTime);
			MySqlParameter pId = cmd.Parameters.Add("@id", MySqlDbType.VarChar, 40);
			foreach (YanportGhostCleanup.CloseDecision d in decisions)
			{
				pDateFin.Value = d.DateFinParis;
				pId.Value = d.PropertyId;
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

	private void RecordLog(DateTime parisStart, string summary)
	{
		using IServiceScope scope = _serviceScopeFactory.CreateScope();
		using ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		AddTaskLog(db, parisStart, summary);
		db.SaveChanges();
	}

	private static void AddTaskLog(ApplicationDbContext db, DateTime parisStart, string summary)
	{
		db.ScheduledTaskLogs.Add(new ScheduledTaskLogs
		{
			StlNom = YanportGhostCleanup.TaskLogName,
			StlDateDebut = parisStart,
			StlDateFin = YanportGhostCleanup.ToParisTime(DateTime.UtcNow),
			StlResults = summary.Length > 1000 ? summary.Substring(0, 1000) : summary,
		});
	}

	private static int? ReadCursor(ApplicationDbContext db)
	{
		AdminVersions row = db.AdminVersions.Find(YanportGhostCleanup.CursorParam);
		return row?.AvVersion;
	}

	private static void WriteCursor(ApplicationDbContext db, int dayKey)
	{
		AdminVersions row = db.AdminVersions.Find(YanportGhostCleanup.CursorParam);
		if (row == null)
		{
			db.AdminVersions.Add(new AdminVersions
			{
				AvParamName = YanportGhostCleanup.CursorParam,
				AvVersion = dayKey,
			});
		}
		else
		{
			row.AvVersion = dayKey;
		}
	}

	public Task StopAsync(CancellationToken stoppingToken)
	{
		_timer?.Change(-1, 0);
		return Task.CompletedTask;
	}

	public void Dispose()
	{
		_timer?.Dispose();
	}
}
