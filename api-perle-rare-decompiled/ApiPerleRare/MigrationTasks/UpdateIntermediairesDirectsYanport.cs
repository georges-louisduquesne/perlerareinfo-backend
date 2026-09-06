using ApiPerleRare.Models;
using MySqlConnector;

namespace ApiPerleRare.MigrationTasks;

public class UpdateIntermediairesDirectsYanport : AbstractMigration
{
	public override int Version => 2;

	public override void Execute(ApplicationDbContext context, MySqlConnection connection)
	{
		using (MySqlCommand cmd = connection.CreateCommand())
		{
			cmd.CommandText = "SELECT ii.I_RefIntermediaire, ii.I_IdYanport FROM intermediaires_directs ii\r\nLEFT JOIN intermediaires_directs_yanport idy ON ii.I_RefIntermediaire=idy.I_RefIntermediaire\r\nWHERE ii.I_IdYanport IS NOT NULL AND ii.I_IdYanport <> '' AND idy.I_YanportId IS NULL";
			using MySqlDataReader reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				uint id = reader.GetUInt32(0);
				ulong yanport = reader.GetUInt64(1);
				context.IntermediairesDirectsYanport.Add(new IntermediairesDirectsYanport
				{
					IRefIntermediaire = id,
					IYanportId = yanport,
					ISource = "init"
				});
			}
		}
		context.SaveChanges();
	}
}
