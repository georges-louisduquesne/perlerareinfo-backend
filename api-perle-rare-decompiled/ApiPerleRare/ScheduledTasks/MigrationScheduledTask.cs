using System;
using System.Data;
using System.Linq;
using ApiPerleRare.MigrationTasks;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace ApiPerleRare.ScheduledTasks;

public class MigrationScheduledTask : AbstractScheduledTask
{
	private readonly ILogger<MigrationScheduledTask> _logger;

	private readonly IServiceScopeFactory _serviceScopeFactory;

	private AbstractMigration[] _migrations;

	public MigrationScheduledTask(ILogger<MigrationScheduledTask> logger, IServiceScopeFactory serviceScopeFactory)
	{
		_logger = logger;
		_serviceScopeFactory = serviceScopeFactory;
	}

	public override string Execute(DateTime lastExecution)
	{
		if (lastExecution.Date == DateTime.Today)
		{
			return "Déjà fait aujourd'hui";
		}
		if (_migrations == null)
		{
			_migrations = (from t in typeof(AbstractMigration).Assembly.GetTypes()
				where typeof(AbstractMigration).IsAssignableFrom(t) && !t.IsAbstract
				select (AbstractMigration)Activator.CreateInstance(t)).ToArray();
		}
		using (IServiceScope scope = _serviceScopeFactory.CreateScope())
		{
			using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			MySqlConnection connection = (MySqlConnection)dbContext.Database.GetDbConnection();
			if (connection.State != ConnectionState.Open)
			{
				connection.Open();
			}
			AdminVersions[] versions = dbContext.AdminVersions.Where((AdminVersions v) => v.AvParamName.StartsWith("mig_")).ToArray();
			AbstractMigration[] migrations = _migrations;
			foreach (AbstractMigration mig in migrations)
			{
				string key = "mig_" + mig.GetType().Name.ToLower();
				AdminVersions migVersion = versions.FirstOrDefault((AdminVersions v) => v.AvParamName == key);
				if (migVersion == null || migVersion.AvVersion < mig.Version)
				{
					mig.Execute(dbContext, connection);
					if (migVersion == null)
					{
						dbContext.AdminVersions.Add(new AdminVersions
						{
							AvParamName = key,
							AvVersion = mig.Version
						});
					}
					else
					{
						migVersion.AvVersion = mig.Version;
					}
					dbContext.SaveChanges();
				}
			}
		}
		return "ok";
	}
}
