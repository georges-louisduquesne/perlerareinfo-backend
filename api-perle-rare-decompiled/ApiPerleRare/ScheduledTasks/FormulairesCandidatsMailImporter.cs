using System;
using System.Linq;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiPerleRare.ScheduledTasks;

internal static class FormulairesCandidatsMailImporter
{
	internal static bool TryGetMailbox(IConfiguration configuration, out ExchangeConnectionString mailbox, out string error)
	{
		mailbox = null;
		error = null;
		string raw = configuration.GetConnectionString("ExchangeContact");
		if (string.IsNullOrWhiteSpace(raw))
		{
			error = "ExchangeContact absent";
			return false;
		}
		ExchangeConnectionString parsed = new ExchangeConnectionString(raw);
		if (string.IsNullOrWhiteSpace(parsed.Username) || string.IsNullOrWhiteSpace(parsed.Password) || string.IsNullOrWhiteSpace(parsed.Server))
		{
			error = "ExchangeContact incomplet";
			return false;
		}
		mailbox = parsed;
		return true;
	}

	internal static string RunOnce(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger logger)
	{
		if (!TryGetMailbox(configuration, out ExchangeConnectionString mailbox, out string configError))
		{
			return configError;
		}
		try
		{
			using IServiceScope scope = scopeFactory.CreateScope();
			using ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			int inserted = ImportMailbox(mailbox, db);
			return "formulaires candidats +" + inserted;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Import formulaires candidats échoué");
			return "erreur: " + ex.Message;
		}
	}

	internal static int ImportMailbox(ExchangeConnectionString mailbox, ApplicationDbContext db)
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
