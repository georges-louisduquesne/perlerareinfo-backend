using System;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Application.Reseaux;
using ApiPerleRare.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiPerleRare.Services;

/// <summary>
/// Remet le solde réseaux au quota le lundi, heure de Paris.
/// Activé sauf si <c>PR_RESEAUX_QUOTA_RESET=off</c>. Indépendant du bundle de l'ancienne API.
/// </summary>
public class ReseauxQuotaResetHostedService : IHostedService, IDisposable
{
	private readonly ILogger<ReseauxQuotaResetHostedService> _logger;

	private readonly IServiceScopeFactory _serviceScopeFactory;

	private Timer _timer;

	private int _running;

	public ReseauxQuotaResetHostedService(ILogger<ReseauxQuotaResetHostedService> logger, IServiceScopeFactory serviceScopeFactory)
	{
		_logger = logger;
		_serviceScopeFactory = serviceScopeFactory;
	}

	public Task StartAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("ReseauxQuotaReset actif (contrôle ~10 min, lundi heure de Paris).");
		_timer = new Timer(Tick, null, TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(10));
		return Task.CompletedTask;
	}

	private void Tick(object state)
	{
		if (Interlocked.Exchange(ref _running, 1) == 1)
		{
			return;
		}
		_ = RunAsync();
	}

	private async Task RunAsync()
	{
		try
		{
			using IServiceScope scope = _serviceScopeFactory.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			if (!await ReseauxQuotaStore.SchemaReady(db, CancellationToken.None))
			{
				return;
			}
			DateTime parisNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ReseauxSociauxLogic.ParisTimeZone());
			string result = await ReseauxQuotaReset.ApplyAsync(db, parisNow, CancellationToken.None);
			if (result == "reset")
			{
				_logger.LogInformation("ReseauxQuotaReset : soldes remis au quota.");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "ReseauxQuotaReset");
		}
		finally
		{
			Interlocked.Exchange(ref _running, 0);
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
