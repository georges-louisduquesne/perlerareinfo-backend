using ApiPerleRare.ScheduledTasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiPerleRare.Tasks;

public class TestYanportScheduleTask : AbstractTask
{
	private YanportScheduledTask _task;

	public override string Description => "Test de la tâche planifiée 'YanportScheduleTask'";

	public TestYanportScheduleTask(IYanportService yanportService, ILogger<YanportScheduledTask> logger, IServiceScopeFactory serviceScopeFactory, IWebSiteService webSiteService, IExchangeService exchangeService)
	{
		_task = new YanportScheduledTask(yanportService, logger, serviceScopeFactory, webSiteService, exchangeService);
	}

	public override void Run()
	{
		_task.Run();
	}
}
