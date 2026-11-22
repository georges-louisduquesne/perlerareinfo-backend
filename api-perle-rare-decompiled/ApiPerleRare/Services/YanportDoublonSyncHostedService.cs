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
/// Lot 2 — Synchronisation quotidienne de <c>property.P_DoublonPropertyId</c> via <c>hit.properties[]</c> Yanport.
/// Activé uniquement par <c>PR_YANPORT_DOUBLON_SYNC=on</c>, indépendant de <c>PR_SCHEDULED_TASKS</c>.
/// La démo reste en <c>PR_LOCAL_SAFE</c> et ne l'enregistre pas.
/// </summary>
public class YanportDoublonSyncHostedService : IHostedService, IDisposable
{
	private readonly ILogger<YanportDoublonSyncHostedService> _logger;

	private readonly IServiceScopeFactory _serviceScopeFactory;

	private readonly IYanportService _yanportService;

	private Timer _timer;

	private int _running;

	public YanportDoublonSyncHostedService(
		ILogger<YanportDoublonSyncHostedService> logger,
		IServiceScopeFactory serviceScopeFactory,
		IYanportService yanportService)
	{
		_logger = logger;
		_serviceScopeFactory = serviceScopeFactory;
		_yanportService = yanportService;
	}

	public Task StartAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("YanportDoublonSync actif (contrôle toutes les 30 min, exécution quotidienne après {Hour} h heure de Paris).", YanportDoublonSync.RunHourParis);
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
			_logger.LogError(ex, "YanportDoublonSync");
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
			_logger.LogWarning("YanportDoublonSync : token Yanport absent, exécution ignorée.");
			return;
		}

		DateTime utcStart = DateTime.UtcNow;
		DateTime parisStart = YanportDoublonSync.ToParisTime(utcStart);
		List<YanportDoublonSync.ActiveProperty> actives;

		using (IServiceScope readScope = _serviceScopeFactory.CreateScope())
		using (ApplicationDbContext db = readScope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
		{
			int? lastDay = ReadCursor(db);
			if (!YanportDoublonSync.Due(lastDay, utcStart))
			{
				return;
			}
			actives = LoadActiveProperties(db);
		}

		Stopwatch sw = Stopwatch.StartNew();
		Dictionary<string, IReadOnlyList<string>> relatedById = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
		int totalBatches = 0;
		int failedBatches = 0;

		for (int offset = 0; offset < actives.Count; offset += YanportDoublonSync.BatchSize)
		{
			int count = Math.Min(YanportDoublonSync.BatchSize, actives.Count - offset);
			List<string> batchIds = new List<string>(count);
			for (int i = 0; i < count; i++)
			{
				batchIds.Add(actives[offset + i].PropertyId);
			}
			totalBatches++;

			if (!TryFetchBatchWithRetry(batchIds, out Dictionary<string, IReadOnlyList<string>> batchRelated))
			{
				failedBatches++;
				continue;
			}

			foreach (KeyValuePair<string, IReadOnlyList<string>> kv in batchRelated)
			{
				relatedById[kv.Key] = kv.Value;
			}
		}

		if (!YanportDoublonSync.IsBatchSuccessAcceptable(totalBatches, failedBatches))
		{
			string msg = $"Abandon : {failedBatches}/{totalBatches} lots Yanport en échec (< 95 % de réussite). Aucune écriture effectuée.";
			_logger.LogWarning("YanportDoublonSync : {Msg}", msg);
			WriteTaskLog(parisStart, msg);
			return;
		}

		YanportDoublonSync.SyncPlan plan = YanportDoublonSync.ComputeSyncPlan(actives, relatedById);

		if (YanportDoublonSync.ShouldAbortForAnomaly(actives.Count, plan.TotalDuplicatesInClusters))
		{
			double ratio = actives.Count > 0 ? (double)plan.TotalDuplicatesInClusters / actives.Count : 0;
			string msg = $"Abandon anomalie : {plan.TotalDuplicatesInClusters}/{actives.Count} doublons ({ratio:P1} > 50 %). Aucune écriture effectuée.";
			_logger.LogWarning("YanportDoublonSync : {Msg}", msg);
			WriteTaskLog(parisStart, msg);
			return;
		}

		int linked = 0;
		int cleared = 0;
		using (IServiceScope writeScope = _serviceScopeFactory.CreateScope())
		using (ApplicationDbContext db = writeScope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
		{
			linked = ApplyUpdates(db, plan.ToLink);
			cleared = ApplyUpdates(db, plan.ToClear);
			WriteCursor(db, YanportDoublonSync.DayKey(utcStart));
		}

		string summary = $"Actifs={actives.Count}, groupes={plan.ClustersCount}, doublons_actifs={plan.TotalDuplicatesInClusters}, lies={linked}, delies={cleared}, inchanges={plan.UnchangedCount}, lots_echec={failedBatches}/{totalBatches}";
		_logger.LogInformation("YanportDoublonSync terminé en {Sec:F1}s : {Summary}", sw.Elapsed.TotalSeconds, summary);
		WriteTaskLog(parisStart, summary);
	}

	private bool TryFetchBatchWithRetry(IReadOnlyList<string> batchIds, out Dictionary<string, IReadOnlyList<string>> related)
	{
		related = null;
		string url = YanportDoublonSync.BuildBatchUrl(batchIds);

		for (int attempt = 0; attempt < 3; attempt++)
		{
			try
			{
				string raw = _yanportService.Get(0, url);
				if (YanportDoublonSync.TryParseBatchRelatedIds(raw, out related))
				{
					return true;
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "YanportDoublonSync : erreur lot (tentative {Attempt}/3)", attempt + 1);
			}
			if (attempt < 2)
			{
				Thread.Sleep(1500 * (attempt + 1));
			}
		}
		return false;
	}

	private static List<YanportDoublonSync.ActiveProperty> LoadActiveProperties(ApplicationDbContext db)
	{
		List<YanportDoublonSync.ActiveProperty> result = new List<YanportDoublonSync.ActiveProperty>();
		MySqlConnection conn = (MySqlConnection)db.Database.GetDbConnection();
		if (conn.State != ConnectionState.Open)
		{
			conn.Open();
		}
		using MySqlCommand cmd = conn.CreateCommand();
		cmd.CommandText = YanportDoublonSync.SelectActiveSql;
		cmd.CommandTimeout = 60;
		using MySqlDataReader reader = cmd.ExecuteReader();
		while (reader.Read())
		{
			string id = reader.IsDBNull(0) ? null : reader.GetString(0)?.Trim();
			if (string.IsNullOrEmpty(id))
			{
				continue;
			}
			string tx = reader.IsDBNull(1) ? null : reader.GetString(1);
			DateTime? debut = reader.IsDBNull(2) ? null : SafeGetDateTime(reader, 2);
			DateTime? crea = reader.IsDBNull(3) ? null : SafeGetDateTime(reader, 3);
			string doublon = reader.IsDBNull(4) ? null : reader.GetString(4);
			result.Add(new YanportDoublonSync.ActiveProperty(id, tx, debut, crea, doublon));
		}
		return result;
	}

	private static DateTime? SafeGetDateTime(MySqlDataReader reader, int ordinal)
	{
		try
		{
			DateTime dt = reader.GetDateTime(ordinal);
			return dt.Year >= 1000 ? dt : null;
		}
		catch
		{
			return null;
		}
	}

	private static int ApplyUpdates(ApplicationDbContext db, IReadOnlyList<YanportDoublonSync.DoublonUpdate> updates)
	{
		if (updates.Count == 0)
		{
			return 0;
		}
		MySqlConnection conn = (MySqlConnection)db.Database.GetDbConnection();
		if (conn.State != ConnectionState.Open)
		{
			conn.Open();
		}
		int updated = 0;
		using MySqlTransaction tx = conn.BeginTransaction();
		using MySqlCommand cmd = conn.CreateCommand();
		cmd.Transaction = tx;
		cmd.CommandText = YanportDoublonSync.UpdateDoublonSql;
		MySqlParameter pId = cmd.Parameters.Add("@id", MySqlDbType.VarChar, 40);
		MySqlParameter pDoublon = cmd.Parameters.Add("@doublonId", MySqlDbType.VarChar, 40);

		foreach (YanportDoublonSync.DoublonUpdate u in updates)
		{
			pId.Value = u.PropertyId;
			pDoublon.Value = string.IsNullOrWhiteSpace(u.TargetDoublonPropertyId)
				? (object)DBNull.Value
				: u.TargetDoublonPropertyId;
			updated += cmd.ExecuteNonQuery();
		}
		tx.Commit();
		return updated;
	}

	private static int? ReadCursor(ApplicationDbContext db)
	{
		AdminVersions row = db.AdminVersions.Find(YanportDoublonSync.CursorParam);
		return row?.AvVersion;
	}

	private static void WriteCursor(ApplicationDbContext db, int dayKey)
	{
		AdminVersions row = db.AdminVersions.Find(YanportDoublonSync.CursorParam);
		if (row == null)
		{
			db.AdminVersions.Add(new AdminVersions
			{
				AvParamName = YanportDoublonSync.CursorParam,
				AvVersion = dayKey,
			});
		}
		else
		{
			row.AvVersion = dayKey;
		}
		db.SaveChanges();
	}

	private void WriteTaskLog(DateTime parisStart, string message)
	{
		try
		{
			using IServiceScope scope = _serviceScopeFactory.CreateScope();
			using ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			db.ScheduledTaskLogs.Add(new ScheduledTaskLogs
			{
				StlNom = YanportDoublonSync.TaskLogName,
				StlDateDebut = parisStart,
				StlDateFin = YanportDoublonSync.ToParisTime(DateTime.UtcNow),
				StlResults = message.Length > 1000 ? message.Substring(0, 1000) : message,
			});
			db.SaveChanges();
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "YanportDoublonSync : impossible d'écrire dans scheduled_task_logs");
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
