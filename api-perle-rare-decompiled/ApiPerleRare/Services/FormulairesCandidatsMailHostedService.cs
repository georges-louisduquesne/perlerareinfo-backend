using System;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using ApiPerleRare.ScheduledTasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiPerleRare.Services;

/// <summary>
/// Lit contact@ et enregistre les formulaires candidats réseaux sociaux.
/// Activé via <c>PR_FORMULAIRES_CANDIDATS_MAIL</c> (défaut : on), y compris en démo / local.
/// </summary>
public class FormulairesCandidatsMailHostedService : IHostedService, IDisposable
{
	private static DateTime _nextPollUtc = DateTime.MinValue;

	private readonly ILogger<FormulairesCandidatsMailHostedService> _logger;

	private readonly IServiceScopeFactory _serviceScopeFactory;

	private readonly IConfiguration _configuration;

	private Timer _timer;

	private int _running;

	public FormulairesCandidatsMailHostedService(ILogger<FormulairesCandidatsMailHostedService> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
	{
		_logger = logger;
		_serviceScopeFactory = serviceScopeFactory;
		_configuration = configuration;
	}

	public Task StartAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("FormulairesCandidatsMail actif (poll ~2 min).");
		_timer = new Timer(Tick, null, TimeSpan.FromSeconds(15.0), TimeSpan.FromSeconds(30.0));
		return Task.CompletedTask;
	}

	private void Tick(object state)
	{
		if (Interlocked.Exchange(ref _running, 1) == 1)
		{
			return;
		}
		try
		{
			if (AbstractScheduledTask.IsPauseTime)
			{
				return;
			}
			if (DateTime.UtcNow < _nextPollUtc)
			{
				return;
			}
			_nextPollUtc = DateTime.UtcNow.AddMinutes(2);
			DateTime started = DateTime.Now;
			string result = FormulairesCandidatsMailImporter.RunOnce(_serviceScopeFactory, _configuration, _logger);
			WriteLog(started, DateTime.Now, result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "FormulairesCandidatsMail");
			WriteLog(DateTime.Now, DateTime.Now, ex.Message);
		}
		finally
		{
			Interlocked.Exchange(ref _running, 0);
		}
	}

	private void WriteLog(DateTime start, DateTime end, string message)
	{
		if (message != null && message.Length > 1000)
		{
			message = message.Substring(0, 1000);
		}
		try
		{
			using IServiceScope scope = _serviceScopeFactory.CreateScope();
			using ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			db.ScheduledTaskLogs.Add(new ScheduledTaskLogs
			{
				StlNom = FormulairesCandidatsMailJob.LogName,
				StlDateDebut = start,
				StlDateFin = end,
				StlResults = message
			});
			db.SaveChanges();
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Impossible d'écrire scheduled_task_logs pour FormulairesCandidatsMail");
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
