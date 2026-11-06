using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace ApiPerleRare.YanportModels;

/// <summary>État lu par la fiche agence pendant le rattachement des biens.</summary>
public sealed class AgencyYanportLinkProgress
{
	public string State { get; set; } = "idle";

	public int Linked { get; set; }

	public int Unlinked { get; set; }
}

/// <summary>
/// File d'attente : le PUT/POST agence répond tout de suite, le rattachement
/// des biens déjà en base se fait ensuite. La fiche interroge <see cref="Snapshot"/>.
/// </summary>
public sealed class AgencyYanportLinkJob
{
	private readonly IBackgroundTaskQueue _queue;

	private readonly IServiceScopeFactory _scopes;

	private readonly ILogger<AgencyYanportLinkJob> _logger;

	private readonly ConcurrentDictionary<uint, ProgressSlot> _slots = new ConcurrentDictionary<uint, ProgressSlot>();

	private int _nextGeneration;

	public AgencyYanportLinkJob(IBackgroundTaskQueue queue, IServiceScopeFactory scopes, ILogger<AgencyYanportLinkJob> logger)
	{
		_queue = queue;
		_scopes = scopes;
		_logger = logger;
	}

	public AgencyYanportLinkProgress Snapshot(uint agencyId)
	{
		if (agencyId == 0 || !_slots.TryGetValue(agencyId, out ProgressSlot slot))
		{
			return new AgencyYanportLinkProgress();
		}
		return new AgencyYanportLinkProgress
		{
			State = slot.State,
			Linked = slot.Linked,
			Unlinked = slot.Unlinked
		};
	}

	public void Enqueue(uint agencyId, long previousYanportId, long nextYanportId)
	{
		if (agencyId == 0 || !AgencyYanportLink.ShouldRun(previousYanportId, nextYanportId))
		{
			return;
		}
		int generation = Interlocked.Increment(ref _nextGeneration);
		_slots[agencyId] = new ProgressSlot
		{
			Generation = generation,
			State = "running"
		};
		_queue.QueueBackgroundWorkItem((CancellationToken ct) => RunAsync(agencyId, previousYanportId, nextYanportId, generation, ct));
	}

	private async Task RunAsync(uint agencyId, long previousYanportId, long nextYanportId, int generation, CancellationToken cancellationToken)
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
			Finish(agencyId, generation, "done", result.PropertiesLinked, result.PropertiesUnlinked);
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
			Finish(agencyId, generation, "error", 0, 0);
			_logger.LogError(ex, "Rattachement Yanport impossible pour l'agence {Agency}", agencyId);
		}
	}

	private void Finish(uint agencyId, int generation, string state, int linked, int unlinked)
	{
		_slots.AddOrUpdate(
			agencyId,
			_ => new ProgressSlot
			{
				Generation = generation,
				State = state,
				Linked = linked,
				Unlinked = unlinked
			},
			(_, slot) =>
			{
				if (slot.Generation != generation)
				{
					return slot;
				}
				return new ProgressSlot
				{
					Generation = generation,
					State = state,
					Linked = linked,
					Unlinked = unlinked
				};
			});
	}

	private sealed class ProgressSlot
	{
		public int Generation { get; set; }

		public string State { get; set; } = "idle";

		public int Linked { get; set; }

		public int Unlinked { get; set; }
	}
}
