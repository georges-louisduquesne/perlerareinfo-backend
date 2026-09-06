using ApiPerleRare.Models;
using MySqlConnector;

namespace ApiPerleRare.MigrationTasks;

public abstract class AbstractMigration
{
	public abstract int Version { get; }

	public abstract void Execute(ApplicationDbContext context, MySqlConnection connection);
}
