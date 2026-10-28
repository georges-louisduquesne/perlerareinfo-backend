using System;
using System.Linq;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiPerleRare.ScheduledTasks;

/// <summary>
/// Lit la boîte contact@ et enregistre les nouveaux formulaires de recrutement.
/// Le mot de passe reste dans ConnectionStrings:ExchangeContact (hors git).
/// </summary>
public class FormulairesCandidatsMailScheduledTask : AbstractScheduledTask
{
	private static DateTime _nextPollUtc = DateTime.MinValue;

	private readonly IServiceScopeFactory _serviceScopeFactory;

	private readonly IConfiguration _configuration;

	private readonly ILogger<FormulairesCandidatsMailScheduledTask> _logger;

	public FormulairesCandidatsMailScheduledTask(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration, ILogger<FormulairesCandidatsMailScheduledTask> logger)
	{
		_serviceScopeFactory = serviceScopeFactory;
		_configuration = configuration;
		_logger = logger;
	}

	public override string Execute(DateTime lastExecution)
	{
		if (DateTime.UtcNow < _nextPollUtc)
		{
			return "-";
		}
		_nextPollUtc = DateTime.UtcNow.AddMinutes(2);
		string raw = _configuration.GetConnectionString("ExchangeContact");
		if (string.IsNullOrWhiteSpace(raw))
		{
			return "ExchangeContact absent";
		}
		ExchangeConnectionString mailbox = new ExchangeConnectionString(raw);
		if (string.IsNullOrWhiteSpace(mailbox.Username) || string.IsNullOrWhiteSpace(mailbox.Password) || string.IsNullOrWhiteSpace(mailbox.Server))
		{
			return "ExchangeContact incomplet";
		}
		try
		{
			int inserted = Import(mailbox);
			return "formulaires candidats +" + inserted;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Import formulaires candidats échoué");
			return "erreur: " + ex.Message;
		}
	}

	private int Import(ExchangeConnectionString mailbox)
	{
		ExchangeService service = new ExchangeService(ExchangeVersion.Exchange2013_SP1);
		service.Credentials = new WebCredentials(mailbox.Username, mailbox.Password, mailbox.Domain ?? "");
		service.Url = new Uri("https://" + mailbox.Server + "/EWS/Exchange.asmx");
		SearchFilter filter = new SearchFilter.ContainsSubstring(ItemSchema.Subject, FormulaireCandidatMail.SubjectMarker);
		ItemView view = new ItemView(40);
		view.OrderBy.Add(ItemSchema.DateTimeReceived, SortDirection.Descending);
		FindItemsResults<Item> found = service.FindItems(WellKnownFolderName.Inbox, filter, view).GetAwaiter().GetResult();
		if (found.Items.Count == 0)
		{
			return 0;
		}
		PropertySet props = new PropertySet(BasePropertySet.FirstClassProperties)
		{
			RequestedBodyType = BodyType.Text
		};
		service.LoadPropertiesForItems(found, props).GetAwaiter().GetResult();
		using IServiceScope scope = _serviceScopeFactory.CreateScope();
		using ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		int inserted = 0;
		foreach (Item item in found)
		{
			if (item is not EmailMessage email)
			{
				continue;
			}
			if (!FormulaireCandidatMail.IsRecruitmentSubject(email.Subject))
			{
				continue;
			}
			string messageId = email.InternetMessageId;
			if (string.IsNullOrWhiteSpace(messageId))
			{
				messageId = email.Id?.UniqueId;
			}
			if (string.IsNullOrWhiteSpace(messageId))
			{
				continue;
			}
			if (db.FormulairesCandidats.Any((FormulairesCandidats row) => row.FcMessageId == messageId))
			{
				continue;
			}
			DateTime paris = TypeformPartialLeadMapper.ToParisWallClock(DateTime.SpecifyKind(email.DateTimeReceived, DateTimeKind.Utc));
			string from = email.From == null ? "" : email.From.ToString();
			FormulairesCandidats form = FormulaireCandidatMail.Parse(messageId, paris, from, email.Subject, email.Body?.Text);
			if (form == null)
			{
				continue;
			}
			db.FormulairesCandidats.Add(form);
			inserted++;
		}
		if (inserted > 0)
		{
			db.SaveChanges();
		}
		return inserted;
	}
}
