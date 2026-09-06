using System;
using System.Collections.Generic;
using System.Data;
using ApiPerleRare.Models;
using ApiPerleRare.YanportModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace ApiPerleRare.ScheduledTasks;

public class CheckPropertyStates : AbstractScheduledTask
{
	private class PropInfo
	{
		public string Id { get; set; }

		public string Annonceurs { get; set; }
	}

	private readonly ILogger<CheckPropertyStates> _logger;

	private readonly IServiceScopeFactory _serviceScopeFactory;

	public CheckPropertyStates(ILogger<CheckPropertyStates> logger, IServiceScopeFactory serviceScopeFactory)
	{
		_logger = logger;
		_serviceScopeFactory = serviceScopeFactory;
	}

	public override string Execute(DateTime lastExecution)
	{
		using IServiceScope scope = _serviceScopeFactory.CreateScope();
		using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		MySqlConnection connection = (MySqlConnection)dbContext.Database.GetDbConnection();
		if (connection.State != ConnectionState.Open)
		{
			connection.Open();
		}
		List<PropInfo> list = new List<PropInfo>();
		using (MySqlCommand cmd = connection.CreateCommand())
		{
			cmd.CommandText = "SELECT p.P_PropertyId, p.P_Annonceurs FROM property p WHERE p.P_State<>1 AND p.P_Annonceurs IS NOT NULL AND P_DateFin IS NULL ORDER BY p.P_DateLastUpdate DESC LIMIT 1000";
			using MySqlDataReader reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				list.Add(new PropInfo
				{
					Id = reader.GetString(0),
					Annonceurs = reader.GetString(1)
				});
			}
		}
		foreach (PropInfo p in list)
		{
			AnnonceursHelper.Check(connection, p.Id, p.Annonceurs);
			using MySqlCommand cmd2 = connection.CreateCommand();
			cmd2.CommandText = "UPDATE property SET P_State=1 WHERE P_PropertyId=@id";
			cmd2.Parameters.AddWithValue("@id", p.Id);
			cmd2.ExecuteNonQuery();
		}
		return $"{list.Count} properties updated";
	}
}
