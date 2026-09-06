using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.Configuration;

namespace ApiPerleRare.Services;

internal class PRExchangeService : IExchangeService
{
	private ExchangeConnectionString _ecs;

	private const string HEAD = "<head><style>html,body{font-family:Verdana,sans-serif;font-size:15px;line-height:1.5}</style></head>";

	private static TimeZoneInfo _parisTimeZone;

	private readonly ExtendedPropertyDefinition _extendedPropEventId = new ExtendedPropertyDefinition(new Guid("{00020329-0000-0000-C000-000000000046}"), "ERefEvenement", MapiPropertyType.Integer);

	public TimeZoneInfo CurrentTimeZone => _parisTimeZone;

	public PRExchangeService(IConfiguration configuration)
	{
		_ecs = new ExchangeConnectionString(configuration.GetConnectionString("Exchange"));
	}

	public bool SendMail(string to, string subject, string contents, Action<EmailMessage> complete = null)
	{
		return SendMail(subject, contents, null, null, null, new string[1] { to }, null, null, null, highImportance: false, copyInSentItems: false, complete);
	}

	public bool SendMail(string subject, string contents, string senderName, string senderEmail, string senderPassword, string[] to, string[] cc, string[] cci, MailAttachment[] attachments, bool highImportance, Action<EmailMessage> complete = null)
	{
		return SendMail(subject, contents, senderName, senderEmail, senderPassword, to, cc, cci, attachments, highImportance, copyInSentItems: true, complete);
	}

	private bool SendMail(string subject, string contents, string senderName, string senderEmail, string senderPassword, string[] to, string[] cc, string[] cci, MailAttachment[] attachments, bool highImportance, bool copyInSentItems, Action<EmailMessage> complete = null)
	{
		if (string.IsNullOrEmpty(_ecs.Server))
		{
			return true;
		}
		if (Startup.IsDevMachine)
		{
			to = to.Select((string v) => "huberje@yahoo.fr").ToArray();
			cc = cc?.Select((string v) => "huberje@yahoo.fr")?.ToArray();
			cci = cci?.Select((string v) => "huberje@yahoo.fr")?.ToArray();
		}
		try
		{
			string html = contents;
			if (!html.Contains("<html>"))
			{
				html = "<html><head><style>html,body{font-family:Verdana,sans-serif;font-size:15px;line-height:1.5}</style></head><body>" + html + "</body></html>";
			}
			if (senderEmail != null && string.IsNullOrWhiteSpace(senderPassword))
			{
				throw new Exception("Aucun mot de passe mail spécifié pour " + senderEmail);
			}
			ExchangeService service = GetExchangeService(senderEmail, senderPassword);
			EmailAddress sender = (string.IsNullOrWhiteSpace(senderName) ? new EmailAddress(senderEmail ?? _ecs.Sender) : new EmailAddress(senderName, senderEmail ?? _ecs.Sender));
			EmailMessage em = new EmailMessage(service);
			em.Subject = subject;
			em.Body = new MessageBody(BodyType.HTML, html);
			em.Sender = sender;
			em.Importance = ((!highImportance) ? Importance.Normal : Importance.High);
			if (to != null)
			{
				string[] array = to;
				foreach (string email in array)
				{
					em.ToRecipients.Add(email);
				}
			}
			if (cc != null)
			{
				string[] array2 = cc;
				foreach (string email2 in array2)
				{
					em.CcRecipients.Add(email2);
				}
			}
			if (cci != null)
			{
				string[] array3 = cci;
				foreach (string email3 in array3)
				{
					em.BccRecipients.Add(email3);
				}
			}
			if (attachments != null)
			{
				foreach (MailAttachment att in attachments)
				{
					FileAttachment fa = em.Attachments.AddFileAttachment(att.FileName, att.Content);
					if (att.IsInLine)
					{
						fa.IsInline = true;
						fa.ContentId = att.ContentId;
					}
				}
			}
			complete?.Invoke(em);
			if (copyInSentItems)
			{
				em.SendAndSaveCopy(WellKnownFolderName.SentItems).Wait();
			}
			else
			{
				em.Send().Wait();
			}
			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
			return false;
		}
	}

	static PRExchangeService()
	{
		_parisTimeZone = TimeZoneInfo.FromSerializedString("Romance Standard Time;60;(UTC+01:00) Bruxelles, Copenhague, Madrid, Paris;Paris, Madrid;Paris, Madrid (heure d'été);[01:01:0001;12:31:9999;60;[0;02:00:00;3;5;0;];[0;03:00:00;10;5;0;];];");
	}

	private ExchangeService GetExchangeService(string userName = null, string password = null)
	{
		ExchangeService service = new ExchangeService(ExchangeVersion.Exchange2013_SP1, _parisTimeZone);
		service.Credentials = new WebCredentials(userName ?? _ecs.Username, password ?? _ecs.Password, _ecs.Domain);
		service.Url = new Uri("https://" + _ecs.Server + "/EWS/Exchange.asmx");
		return service;
	}

	public int GetUnreadEmails(string email, string password)
	{
		ExchangeService service = GetExchangeService(email, password);
		Mailbox mailBox = new Mailbox(email);
		FolderId folderId = new FolderId(WellKnownFolderName.Inbox, mailBox);
		Folder folder = Folder.Bind(service, folderId).Result;
		return folder.UnreadCount;
	}

	public TodayAppointmentsResponse GetTodayAppointments(string email, string password)
	{
		ExchangeService service = GetExchangeService(email, password);
		DateTime startDate = DateTime.Now;
		DateTime endDate = DateTime.Today.AddDays(1.0);
		CalendarFolder calendar = CalendarFolder.Bind(service, WellKnownFolderName.Calendar, new PropertySet()).Result;
		CalendarView cView = new CalendarView(startDate, endDate);
		cView.PropertySet = new PropertySet(ItemSchema.Subject, AppointmentSchema.Start, AppointmentSchema.End);
		FindItemsResults<Appointment> appointments = calendar.FindAppointments(cView).Result;
		Appointment next = appointments.OrderBy((Appointment a) => a.Start).FirstOrDefault();
		return new TodayAppointmentsResponse
		{
			NextDate = next?.Start,
			NextSubject = next?.Subject,
			Count = appointments.Count()
		};
	}

	public async Task<AddAppointmentRes> AddAppointment(string email, string password, RendezVous rendezVous)
	{
		ExchangeService service = GetExchangeService(email, password);
		Appointment appointment = null;
		bool updated = false;
		if (rendezVous.ERefEvenement != 0)
		{
			Item existing = await FindItem(service, rendezVous.ERefEvenement);
			if (existing != null)
			{
				appointment = await Appointment.Bind(service, existing.Id);
				updated = true;
			}
		}
		if (appointment == null)
		{
			appointment = new Appointment(service);
		}
		appointment.Subject = rendezVous.Subject;
		appointment.Body = new MessageBody(BodyType.HTML, rendezVous.HtmlBody);
		appointment.Start = rendezVous.Start;
		appointment.End = rendezVous.End;
		appointment.Location = rendezVous.Location;
		appointment.ReminderMinutesBeforeStart = rendezVous.ReminderMinutesBeforeStart;
		if (rendezVous.ERefEvenement != 0)
		{
			appointment.SetExtendedProperty(_extendedPropEventId, rendezVous.ERefEvenement);
		}
		SendInvitationsMode mode = SendInvitationsMode.SendToNone;
		appointment.OptionalAttendees.Clear();
		if (rendezVous.OptionalAttendees != null && rendezVous.OptionalAttendees.Length != 0)
		{
			string[] optionalAttendees = rendezVous.OptionalAttendees;
			foreach (string attendeeEmail in optionalAttendees)
			{
				appointment.OptionalAttendees.Add(attendeeEmail);
			}
			mode = SendInvitationsMode.SendToAllAndSaveCopy;
		}
		appointment.RequiredAttendees.Clear();
		if (rendezVous.RequiredAttendees != null && rendezVous.RequiredAttendees.Length != 0)
		{
			string[] requiredAttendees = rendezVous.RequiredAttendees;
			foreach (string attendeeEmail2 in requiredAttendees)
			{
				appointment.RequiredAttendees.Add(attendeeEmail2);
			}
			mode = SendInvitationsMode.SendToAllAndSaveCopy;
		}
		if (appointment.Id != null)
		{
			await appointment.Update(ConflictResolutionMode.AlwaysOverwrite);
		}
		else
		{
			await appointment.Save(mode);
		}
		return new AddAppointmentRes
		{
			Id = appointment.Id?.UniqueId,
			Updated = updated
		};
	}

	public async Task<Item> FindItem(ExchangeService service, int eRefEvenement, bool loadProperties = false)
	{
		PropertySet ps = new PropertySet(ItemSchema.Subject, AppointmentSchema.Start, AppointmentSchema.End, ItemSchema.ReminderMinutesBeforeStart, AppointmentSchema.RequiredAttendees, AppointmentSchema.OptionalAttendees, ItemSchema.Body);
		CalendarFolder calendar = CalendarFolder.Bind(service, WellKnownFolderName.Calendar, ps).Result;
		ItemView view = new ItemView(1);
		SearchFilter.IsEqualTo filter = new SearchFilter.IsEqualTo(_extendedPropEventId, eRefEvenement);
		FindItemsResults<Item> res = await calendar.FindItems(filter, view);
		if (loadProperties && res.Count() > 0)
		{
			await service.LoadPropertiesForItems(res, ps);
		}
		return res.FirstOrDefault();
	}

	public async Task<RendezVous> FindAppointmentFromERefEvenement(string email, string password, int eRefEvenement)
	{
		ExchangeService service = GetExchangeService(email, password);
		Item first = await FindItem(service, eRefEvenement, loadProperties: true);
		if (first == null)
		{
			return null;
		}
		Appointment app = (Appointment)first;
		return new RendezVous
		{
			Subject = app.Subject,
			Start = app.Start,
			End = app.End,
			ReminderMinutesBeforeStart = app.ReminderMinutesBeforeStart,
			RequiredAttendees = GetAttendees(app.RequiredAttendees),
			OptionalAttendees = GetAttendees(app.OptionalAttendees),
			ERefEvenement = eRefEvenement,
			HtmlBody = app.Body?.Text
		};
	}

	private string[] GetAttendees(AttendeeCollection attendees)
	{
		if (attendees == null)
		{
			return new string[0];
		}
		return attendees.Select((Attendee a) => a.Address).ToArray();
	}

	public async Task<bool> DeleteAppointmentFromERefEvenement(string email, string password, int eRefEvenement)
	{
		ExchangeService service = GetExchangeService(email, password);
		Item first = await FindItem(service, eRefEvenement);
		if (first == null)
		{
			return false;
		}
		await first.Delete(DeleteMode.HardDelete);
		return true;
	}
}
