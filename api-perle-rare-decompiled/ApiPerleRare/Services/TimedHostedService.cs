using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using ApiPerleRare.ScheduledTasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiPerleRare.Services;

public class TimedHostedService : IHostedService, IDisposable
{
	private readonly ILogger<TimedHostedService> _logger;

	private readonly IServiceScopeFactory _serviceScopeFactory;

	private Type[] _availableTaskTypes;

	private Timer _timer;

	private DateTime _lastExecution = new DateTime(2000, 1, 1);

	private bool _executing = false;

	public TimedHostedService(ILogger<TimedHostedService> logger, IServiceScopeFactory serviceScopeFactory)
	{
		_logger = logger;
		_serviceScopeFactory = serviceScopeFactory;
	}

	public Task StartAsync(CancellationToken stoppingToken)
	{
		_availableTaskTypes = (from t in typeof(AbstractScheduledTask).Assembly.GetTypes()
			where !t.IsAbstract && typeof(AbstractScheduledTask).IsAssignableFrom(t)
			select t).ToArray();
		_logger.LogInformation($"Timed Hosted Service running ({_availableTaskTypes.Length} tasks).");
		_timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(30.0));
		return Task.CompletedTask;
	}

	private void DoWork(object state)
	{
		if (_executing || AbstractScheduledTask.IsPauseTime)
		{
			return;
		}
		_executing = true;
		DateTime execution = DateTime.Now;
		try
		{
			using IServiceScope scope = _serviceScopeFactory.CreateScope();
			using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			Type[] availableTaskTypes = _availableTaskTypes;
			foreach (Type taskType in availableTaskTypes)
			{
				ScheduledTaskLogs log = new ScheduledTaskLogs
				{
					StlNom = taskType.Name,
					StlDateDebut = DateTime.Now
				};
				dbContext.ScheduledTaskLogs.Add(log);
				dbContext.SaveChanges();
				try
				{
					AbstractScheduledTask task = (AbstractScheduledTask)ActivatorUtilities.CreateInstance(scope.ServiceProvider, taskType);
					task.Init(dbContext, log.StlId);
					_logger.LogInformation("Tâche '" + taskType.FullName + "' instanciée avec succès");
					string res = task.Execute(_lastExecution);
					log.StlResults = res.Truncate(1000);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Erreur tâche '" + taskType.FullName + "'");
					log.StlResults = ex.ToString().Truncate(1000);
				}
				log.StlDateFin = DateTime.Now;
				dbContext.SaveChanges();
			}
			_lastExecution = execution;
		}
		finally
		{
			_executing = false;
		}
	}

	public Task StopAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Timed Hosted Service is stopping.");
		_timer?.Change(-1, 0);
		return Task.CompletedTask;
	}

	public void Dispose()
	{
		_timer?.Dispose();
	}
}
