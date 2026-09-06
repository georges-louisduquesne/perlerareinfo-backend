using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiPerleRare.Services;

public class QueuedHostedService : BackgroundService
{
	private readonly ILogger<QueuedHostedService> _logger;

	public IBackgroundTaskQueue TaskQueue { get; }

	public QueuedHostedService(IBackgroundTaskQueue taskQueue, ILogger<QueuedHostedService> logger)
	{
		TaskQueue = taskQueue;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Queued Hosted Service is running.");
		await BackgroundProcessing(stoppingToken);
	}

	private async Task BackgroundProcessing(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			Func<CancellationToken, Task> workItem = await TaskQueue.DequeueAsync(stoppingToken);
			try
			{
				await workItem(stoppingToken);
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, "Error occurred executing {WorkItem}.", "workItem");
			}
		}
	}

	public override async Task StopAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Queued Hosted Service is stopping.");
		await base.StopAsync(stoppingToken);
	}
}
