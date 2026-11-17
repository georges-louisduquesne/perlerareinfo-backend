using System;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Application.Exchange;
using ApiPerleRare;
using ApiPerleRare.Application.Monitoring;
using ApiPerleRare.Models;
using ApiPerleRare.ScheduledTasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace ApiPerleRare.Services;

/// <summary>
/// Un mail par mois, de support@ vers gld@. Activé seulement par <c>PR_RECRAWL_STOCK_MAIL=on</c>
/// (v2 prod). La démo reste en <c>PR_LOCAL_SAFE</c> et ne l’enregistre pas.
/// </summary>
public class RecrawlStockMailHostedService : IHostedService, IDisposable
{
	private readonly ILogger<RecrawlStockMailHostedService> _logger;

	private readonly IServiceScopeFactory _serviceScopeFactory;

	private readonly IConfiguration _configuration;

	private Timer _timer;

	private int _running;

	public RecrawlStockMailHostedService(ILogger<RecrawlStockMailHostedService> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
	{
		_logger = logger;
		_serviceScopeFactory = serviceScopeFactory;
		_configuration = configuration;
	}

	public Task StartAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("RecrawlStockMail actif (contrôle horaire, envoi après 8 h, heure de Paris).");
		_timer = new Timer(Tick, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(30));
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
			Poll();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "RecrawlStockMail");
		}
		finally
		{
			Interlocked.Exchange(ref _running, 0);
		}
	}

	private void Poll()
	{
		using IServiceScope scope = _serviceScopeFactory.CreateScope();
		using ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		DateTime utc = DateTime.UtcNow;
		int? lastMonth = ReadCursor(db);
		if (!RecrawlStockMail.Due(lastMonth, utc))
		{
			return;
		}
		MySqlConnection connection = (MySqlConnection)db.Database.GetDbConnection();
		RecrawlStockQuery.Counts counts = RecrawlStockQuery.Read(connection);
		IExchangeService exchange = scope.ServiceProvider.GetRequiredService<IExchangeService>();
		IConseillerMailboxLookup mailboxes = scope.ServiceProvider.GetRequiredService<IConseillerMailboxLookup>();
		string password = SupportPassword(mailboxes);
		if (string.IsNullOrWhiteSpace(password))
		{
			_logger.LogWarning("RecrawlStockMail : mot de passe de support@perle-rare.com introuvable.");
			return;
		}
		string body = RecrawlStockMail.Body(counts.Over90, counts.Between30And90);
		bool ok = exchange.SendMail(
			RecrawlStockMail.Subject,
			RecrawlStockMail.ToHtml(body),
			"Perle Rare",
			RecrawlStockMail.FromAddress,
			password,
			new string[1] { RecrawlStockMail.ToAddress },
			null,
			null,
			null,
			highImportance: false);
		if (!ok)
		{
			_logger.LogWarning("RecrawlStockMail : envoi refusé.");
			return;
		}
		WriteCursor(db, RecrawlStockMail.MonthKey(utc));
		_logger.LogInformation("RecrawlStockMail envoyé : {Over90} biens de plus de 90 jours, {Mid} biens de 30 à 90 jours.", counts.Over90, counts.Between30And90);
	}

	private string SupportPassword(IConseillerMailboxLookup mailboxes)
	{
		string configured = SendEmailUseCase.SupportPasswordFrom(_configuration);
		if (!string.IsNullOrWhiteSpace(configured))
		{
			return configured;
		}
		try
		{
			ConseillerMailbox mailbox = mailboxes?.GetByEmail(RecrawlStockMail.FromAddress);
			if (mailbox != null && string.Equals((mailbox.Email ?? "").Trim(), RecrawlStockMail.FromAddress, StringComparison.OrdinalIgnoreCase))
			{
				return mailbox.Password;
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "RecrawlStockMail : lecture de la boîte support impossible.");
		}
		return null;
	}

	private static int? ReadCursor(ApplicationDbContext db)
	{
		AdminVersions row = db.AdminVersions.Find(RecrawlStockMail.CursorParam);
		if (row == null)
		{
			return null;
		}
		return row.AvVersion;
	}

	private static void WriteCursor(ApplicationDbContext db, int monthKey)
	{
		AdminVersions row = db.AdminVersions.Find(RecrawlStockMail.CursorParam);
		if (row == null)
		{
			db.AdminVersions.Add(new AdminVersions
			{
				AvParamName = RecrawlStockMail.CursorParam,
				AvVersion = monthKey,
			});
		}
		else
		{
			row.AvVersion = monthKey;
		}
		db.SaveChanges();
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
