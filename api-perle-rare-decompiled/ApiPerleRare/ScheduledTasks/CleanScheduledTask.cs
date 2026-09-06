using System;
using System.Data;
using System.Data.Common;
using System.Text;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPerleRare.ScheduledTasks;

public class CleanScheduledTask : AbstractScheduledTask
{
	private readonly IServiceScopeFactory _serviceScopeFactory;

	public CleanScheduledTask(IServiceScopeFactory serviceScopeFactory)
	{
		_serviceScopeFactory = serviceScopeFactory;
	}

	public override string Execute(DateTime lastExecution)
	{
		if (lastExecution.Hour == DateTime.Now.Hour || DateTime.Now.Hour == 2)
		{
			return "-";
		}
		StringBuilder logs = new StringBuilder();
		using (IServiceScope scope = _serviceScopeFactory.CreateScope())
		{
			using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			using DbConnection conn = dbContext.Database.GetDbConnection();
			if (conn.State != ConnectionState.Open)
			{
				conn.Open();
			}
			using (DbCommand cmd = conn.CreateCommand())
			{
				cmd.CommandText = "update url_search SET us_en_cours=0  WHERE us_moteur=0 AND us_suspendu=1 AND us_en_cours=1 AND us_prochain_passage < DATE_ADD(NOW(),INTERVAL -2 hour)";
				int nb = cmd.ExecuteNonQuery();
				logs.AppendLine($"{nb} url_search maj");
			}
			if (lastExecution.Date == DateTime.Today)
			{
				return logs.ToString();
			}
			using (DbCommand cmd2 = conn.CreateCommand())
			{
				cmd2.CommandText = "DELETE FROM url_logs WHERE ul_date < DATE_ADD(NOW(),INTERVAL -60 day) ";
				int nb2 = cmd2.ExecuteNonQuery();
				logs.AppendLine($"{nb2} url_logs supprimés");
			}
			using (DbCommand cmd3 = conn.CreateCommand())
			{
				cmd3.CommandText = "DELETE FROM scheduled_task_logs WHERE STL_DateDebut < DATE_ADD(NOW(),INTERVAL -60 day) ";
				int nb3 = cmd3.ExecuteNonQuery();
				logs.AppendLine($"{nb3} scheduled_task_logs supprimés");
			}
			string isExpiredFilter = "AG_TimeEndY>0 AND AG_DateFin IS NULL";
			int expired_nb;
			DateTime? expired_last;
			DateTime? expired_first;
			using (DbCommand cmd4 = conn.CreateCommand())
			{
				cmd4.CommandText = "SELECT COUNT(*) as nb, MAX(AG_TimeY) as last, MIN(AG_TimeY) as first FROM annonces_globales WHERE " + isExpiredFilter;
				using DbDataReader reader = cmd4.ExecuteReader();
				reader.Read();
				expired_nb = reader.GetInt32(0);
				expired_last = (reader.IsDBNull(1) ? ((DateTime?)null) : new DateTime?(reader.GetDateTime(1)));
				expired_first = (reader.IsDBNull(2) ? ((DateTime?)null) : new DateTime?(reader.GetDateTime(2)));
			}
			if (expired_nb > 0)
			{
				IExchangeService exchangeService = scope.ServiceProvider.GetService<IExchangeService>();
				using (DbCommand cmd5 = conn.CreateCommand())
				{
					cmd5.CommandText = "update annonces_globales SET AG_DateFin=NOW() WHERE " + isExpiredFilter;
					expired_nb = cmd5.ExecuteNonQuery();
				}
				exchangeService.SendError($"Erreur annonces expirées ({expired_nb})", $"{expired_nb} annonces expirés corrigées.<br/>{expired_first.Value:G} <= AG_TimeY <= {expired_last.Value:G}");
			}
			using DbCommand cmd6 = conn.CreateCommand();
			cmd6.CommandText = "DELETE FROM photos_annonces WHERE PA_IdPropertyYanport NOT IN (SELECT AG_IdPropertyYanport FROM annonces_globales)";
			int nb4 = cmd6.ExecuteNonQuery();
			logs.AppendLine($"{nb4} photos_annonces supprimés");
		}
		return logs.ToString();
	}
}
