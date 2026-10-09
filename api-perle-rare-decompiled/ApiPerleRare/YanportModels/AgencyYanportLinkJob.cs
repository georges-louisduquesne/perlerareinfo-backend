using System;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace ApiPerleRare.YanportModels;

/// <summary>
/// File d'attente : le PUT/POST agence répond tout de suite, le rattachement
/// des biens publiés se fait ensuite.
/// </summary>
public sealed class AgencyYanportLinkJob
{
	private readonly IBackgroundTaskQueue _queue;

	private readonly IServiceScopeFactory _scopes;

	private readonly ILogger<AgencyYanportLinkJob> _logger;

	public AgencyYanportLinkJob(IBackgroundTaskQueue queue, IServiceScopeFactory scopes, ILogger<AgencyYanportLinkJob> logger)
	{
		_queue = queue;
		_scopes = scopes;
		_logger = logger;
	}

	public void Enqueue(uint agencyId, long previousYanportId, long nextYanportId)
	{
		if (agencyId == 0 || !AgencyYanportLink.ShouldRun(previousYanportId, nextYanportId))
		{
			return;
		}
		_queue.QueueBackgroundWorkItem((CancellationToken ct) => RunAsync(agencyId, previousYanportId, nextYanportId, ct));
	}

	private async Task RunAsync(uint agencyId, long previousYanportId, long nextYanportId, CancellationToken cancellationToken)
	{
		try
		{
			using IServiceScope scope = _scopes.CreateScope();
			ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			MySqlConnection connection = (MySqlConnection)db.Database.GetDbConnection();
			if (connection.State != System.Data.ConnectionState.Open)
			{
				await connection.OpenAsync(cancellationToken);
			}
			AgencyYanportLinkResult result = await AgencyYanportLink.ExecuteAsync(connection, agencyId, previousYanportId, nextYanportId);
			_logger.LogInformation(
				"Id Yanport agence {Agency} {Previous} -> {Next} : ligne {Ensured}/{Removed}, biens liés {Linked}, détachés {Unlinked}",
				agencyId,
				previousYanportId,
				nextYanportId,
				result.FieldRowEnsured,
				result.FieldRowRemoved,
				result.PropertiesLinked,
				result.PropertiesUnlinked);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Rattachement Yanport impossible pour l'agence {Agency}", agencyId);
		}
	}
}
