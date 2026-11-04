using System;
using ApiPerleRare.ScheduledTasks;
using ApiPerleRare.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiPerleRare.Tasks;

/// <summary>
/// Exécution manuelle : <c>ApiPerleRare -execute ImportFormulairesCandidats</c>
/// </summary>
public class ImportFormulairesCandidatsMailTask : AbstractTask
{
	private readonly IServiceScopeFactory _serviceScopeFactory;

	private readonly IConfiguration _configuration;

	private readonly ILogger<FormulairesCandidatsMailHostedService> _logger;

	public ImportFormulairesCandidatsMailTask(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration, ILogger<FormulairesCandidatsMailHostedService> logger)
	{
		_serviceScopeFactory = serviceScopeFactory;
		_configuration = configuration;
		_logger = logger;
	}

	public override string Description => "Importe les mails de recrutement contact@ vers formulaires_candidats";

	public override void Run()
	{
		string result = FormulairesCandidatsMailImporter.RunOnce(_serviceScopeFactory, _configuration, _logger);
		Console.WriteLine(result);
	}
}
