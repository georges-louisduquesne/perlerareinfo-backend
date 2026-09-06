using System;
using System.Linq;
using System.Text;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiPerleRare.ScheduledTasks;

public class SendPendingNotifications : AbstractScheduledTask
{
	private readonly ILogger<SendPendingNotifications> _logger;

	private readonly IServiceScopeFactory _serviceScopeFactory;

	private readonly IExchangeService _exchangeService;

	public SendPendingNotifications(ILogger<SendPendingNotifications> logger, IServiceScopeFactory serviceScopeFactory, IExchangeService exchangeService)
	{
		_logger = logger;
		_serviceScopeFactory = serviceScopeFactory;
		_exchangeService = exchangeService;
	}

	public override string Execute(DateTime lastExecution)
	{
		if (Startup.IsDevMachine)
		{
			return "Pas de notif en dév";
		}
		_logger.LogInformation("Envoi des notifications...");
		int loaded = 0;
		int errors = 0;
		int sent = 0;
		StringBuilder logs = new StringBuilder();
		StringBuilder stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler;
		using (IServiceScope scope = _serviceScopeFactory.CreateScope())
		{
			using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			Notifications[] notifs = (from notifications in dbContext.Notifications
				where notifications.NState == -2 && notifications.NPropertyId != null
				orderby notifications.NCreatedOn
				select notifications).Take(100).ToArray();
			loaded = notifs.Length;
			NotificationHelper helper = new NotificationHelper(_exchangeService);
			Notifications[] array = notifs;
			for (int num = 0; num < array.Length; num++)
			{
				Notifications n = array[num];
				Property property = dbContext.Property.AsNoTracking().FirstOrDefault((Property p) => p.PPropertyId == n.NPropertyId);
				if (property == null)
				{
					helper.LogError($"Notification {n.NId} sans property {n.NPropertyId}", "Le bien '" + n.NPropertyId + "' n'existe pas");
					n.NState = -1;
					errors++;
					stringBuilder = logs;
					StringBuilder stringBuilder2 = stringBuilder;
					handler = new StringBuilder.AppendInterpolatedStringHandler(28, 2, stringBuilder);
					handler.AppendLiteral("Notification ");
					handler.AppendFormatted(n.NId);
					handler.AppendLiteral(" sans property ");
					handler.AppendFormatted(n.NPropertyId);
					stringBuilder2.AppendLine(ref handler);
				}
				else
				{
					sbyte newState;
					try
					{
						string conseillerMail = (from conseillersPersonnels in dbContext.ConseillersPersonnels
							where conseillersPersonnels.CpRefConseiller == n.NRefConseiller
							select conseillersPersonnels.CpMel).FirstOrDefault();
						string contactNomFamille = (from cr in dbContext.ContactsRecherche
							where cr.CRefContact == n.NRefContact
							select cr.CNomFamille).FirstOrDefault();
						NotificationContainer nc = new NotificationContainer(helper, n, property, conseillerMail, contactNomFamille);
						newState = nc.SendNotification();
						if (newState == -1)
						{
							stringBuilder = logs;
							StringBuilder stringBuilder3 = stringBuilder;
							handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder);
							handler.AppendLiteral("Echec envoi email ");
							handler.AppendFormatted(n.NId);
							stringBuilder3.AppendLine(ref handler);
							errors++;
						}
						else
						{
							sent++;
						}
					}
					catch (Exception ex)
					{
						newState = -1;
						helper.LogError("Erreur non gérée!", ex.ToString());
						errors++;
						logs.AppendLine(ex.ToString());
					}
					n.NState = newState;
				}
				dbContext.SaveChanges();
				Log($"{sent} envoyés / {errors} erreurs sur {loaded}...");
			}
		}
		_logger.LogInformation("Envoi des notifications OK");
		StringBuilder res = new StringBuilder();
		stringBuilder = res;
		StringBuilder stringBuilder4 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(8, 1, stringBuilder);
		handler.AppendLiteral("Total : ");
		handler.AppendFormatted(loaded);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder = res;
		StringBuilder stringBuilder5 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder);
		handler.AppendLiteral("Envoyés : ");
		handler.AppendFormatted(sent);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder = res;
		StringBuilder stringBuilder6 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder);
		handler.AppendLiteral("Erreurs : ");
		handler.AppendFormatted(errors);
		stringBuilder6.AppendLine(ref handler);
		return res.ToString() + logs.ToString();
	}
}
