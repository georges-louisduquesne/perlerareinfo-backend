using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using ApiPerleRare.Application.Exchange;
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

	private static readonly string[] RoomMailboxEmails = new string[2] { "grande_salle@perle-rare.com", "salon@perle-rare.com" };

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
			Console.WriteLine("Exchange: chaîne de connexion vide — envoi ignoré.");
			return false;
		}
		// Service-account / jobs on a dev box stay redirected. CRM compose with
		// the conseiller password keeps the real recipients (localhost = demo/prod).
		if (Startup.IsDevMachine && string.IsNullOrWhiteSpace(senderPassword))
		{
			to = to.Select((string v) => "florimond@lapotionstudio.com").ToArray();
			cc = cc?.Select((string v) => "florimond@lapotionstudio.com")?.ToArray();
			cci = cci?.Select((string v) => "florimond@lapotionstudio.com")?.ToArray();
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
		service.Credentials = new WebCredentials(userName ?? _ecs.Username, password ?? _ecs.Password, _ecs.Domain ?? "");
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

	public bool IsUserAvailable(string callerEmail, string callerPassword, string targetEmail, DateTime start, DateTime end)
	{
		if (string.IsNullOrWhiteSpace(targetEmail) || end <= start)
		{
			return false;
		}
		// GetUserAvailability n'accepte qu'une fenêtre d'au moins 24 h, de minuit à minuit.
		// Le créneau demandé est filtré ensuite sur les événements de la journée.
		DateTime slotStart = DateTime.SpecifyKind(start, DateTimeKind.Unspecified);
		DateTime slotEnd = DateTime.SpecifyKind(end, DateTimeKind.Unspecified);
		DateTime windowStart = slotStart.Date;
		DateTime windowEnd = slotEnd.Date;
		if (slotEnd.TimeOfDay > TimeSpan.Zero || windowEnd <= windowStart)
		{
			windowEnd = windowEnd.AddDays(1);
		}
		if ((windowEnd - windowStart).TotalHours < 24)
		{
			windowEnd = windowStart.AddDays(1);
		}
		ExchangeService service = GetExchangeService(callerEmail, callerPassword);
		List<AttendeeInfo> attendees = new List<AttendeeInfo>
		{
			new AttendeeInfo(targetEmail.Trim())
		};
		GetUserAvailabilityResults results = service.GetUserAvailability(attendees, new TimeWindow(windowStart, windowEnd), AvailabilityData.FreeBusy).Result;
		if (results?.AttendeesAvailability == null || results.AttendeesAvailability.Count == 0)
		{
			return true;
		}
		AttendeeAvailability availability = results.AttendeesAvailability[0];
		if (availability.CalendarEvents == null)
		{
			return availability.ErrorCode == ServiceError.NoError;
		}
		foreach (CalendarEvent ev in availability.CalendarEvents)
		{
			LegacyFreeBusyStatus status = ev.FreeBusyStatus;
			if (status == LegacyFreeBusyStatus.Free || status == LegacyFreeBusyStatus.NoData)
			{
				continue;
			}
			DateTime evStart = DateTime.SpecifyKind(ev.StartTime, DateTimeKind.Unspecified);
			DateTime evEnd = DateTime.SpecifyKind(ev.EndTime, DateTimeKind.Unspecified);
			if (evStart < slotEnd && evEnd > slotStart)
			{
				return false;
			}
		}
		return true;
	}

	public List<SalonCalendarEvent> GetSalonCalendar(string callerEmail, string callerPassword, DateTime start, DateTime end)
	{
		if (end <= start)
		{
			return new List<SalonCalendarEvent>();
		}
		DateTime windowStart = DateTime.SpecifyKind(start, DateTimeKind.Unspecified);
		DateTime windowEnd = DateTime.SpecifyKind(end, DateTimeKind.Unspecified);
		ExchangeService service = GetExchangeService(callerEmail, callerPassword);
		(string Room, string Email)[] rooms = new (string, string)[]
		{
			("panoramique", "grande_salle@perle-rare.com"),
			("bis", "salon@perle-rare.com")
		};
		List<SalonCalendarEvent> events = new List<SalonCalendarEvent>();
		foreach ((string room, string email) in rooms)
		{
			events.AddRange(ReadSalonFreeBusy(service, room, email, windowStart, windowEnd));
		}
		events.Sort((SalonCalendarEvent a, SalonCalendarEvent b) => a.Start.CompareTo(b.Start));
		return events;
	}

	private List<SalonCalendarEvent> ReadSalonFreeBusy(ExchangeService service, string room, string email, DateTime start, DateTime end)
	{
		DateTime windowStart = start.Date;
		DateTime windowEnd = end.Date;
		if (end.TimeOfDay > TimeSpan.Zero || windowEnd <= windowStart)
		{
			windowEnd = windowEnd.AddDays(1);
		}
		if ((windowEnd - windowStart).TotalHours < 24)
		{
			windowEnd = windowStart.AddDays(1);
		}
		AvailabilityOptions options = new AvailabilityOptions
		{
			RequestedFreeBusyView = FreeBusyViewType.Detailed
		};
		List<AttendeeInfo> attendees = new List<AttendeeInfo>
		{
			new AttendeeInfo(email)
		};
		GetUserAvailabilityResults results = service.GetUserAvailability(attendees, new TimeWindow(windowStart, windowEnd), AvailabilityData.FreeBusy, options).Result;
		List<SalonCalendarEvent> list = new List<SalonCalendarEvent>();
		if (results?.AttendeesAvailability == null || results.AttendeesAvailability.Count == 0)
		{
			return list;
		}
		AttendeeAvailability availability = results.AttendeesAvailability[0];
		if (availability.CalendarEvents == null)
		{
			return list;
		}
		foreach (CalendarEvent ev in availability.CalendarEvents)
		{
			if (ev.FreeBusyStatus == LegacyFreeBusyStatus.Free || ev.FreeBusyStatus == LegacyFreeBusyStatus.NoData)
			{
				continue;
			}
			DateTime evStart = AsWallClock(ev.StartTime);
			DateTime evEnd = AsWallClock(ev.EndTime);
			if (evEnd <= start || evStart >= end)
			{
				continue;
			}
			list.Add(new SalonCalendarEvent
			{
				Room = room,
				Subject = ev.Details?.Subject ?? string.Empty,
				Start = evStart,
				End = evEnd,
				OrganizerEmail = string.Empty,
				OrganizerName = string.Empty
			});
		}
		return list;
	}

	private DateTime AsWallClock(DateTime value)
	{
		if (value.Kind == DateTimeKind.Utc && _parisTimeZone != null)
		{
			value = TimeZoneInfo.ConvertTimeFromUtc(value, _parisTimeZone);
		}
		return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
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
		FolderId calendarId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(email));
		Appointment appointment = null;
		Appointment staleCopy = null;
		bool updated = false;
		if (rendezVous.ERefEvenement != 0)
		{
			Item existing = await FindItem(service, rendezVous.ERefEvenement, calendarId);
			if (existing != null)
			{
				Appointment bound = await Appointment.Bind(service, existing.Id, new PropertySet(BasePropertySet.FirstClassProperties, AppointmentSchema.Organizer));
				if (OrganizerIsMailbox(bound, email))
				{
					appointment = bound;
					updated = true;
				}
				else
				{
					// Copie reçue : ne pas la supprimer avant d'avoir créé l'exemplaire organisateur.
					staleCopy = bound;
				}
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
		if (AddSmtpAttendees(appointment.OptionalAttendees, rendezVous.OptionalAttendees))
		{
			mode = SendInvitationsMode.SendToAllAndSaveCopy;
		}
		if (AddSmtpAttendees(appointment.RequiredAttendees, rendezVous.RequiredAttendees))
		{
			mode = SendInvitationsMode.SendToAllAndSaveCopy;
		}
		LegacyFreeBusyStatus busyStatus = ToLegacyFreeBusyStatus(rendezVous.LegacyFreeBusyStatus);
		appointment.LegacyFreeBusyStatus = busyStatus;
		// Le premier enregistrement fige l’organisateur en adresse EX. On pose l’adresse SMTP
		// avant l’envoi, pour qu’Angular et FF n’exposent plus le legacy DN au destinataire.
		StampSmtpOrganizer(appointment, email);
		if (appointment.Id != null)
		{
			await appointment.Update(ConflictResolutionMode.AlwaysOverwrite, SendInvitationsOrCancellationsMode.SendToAllAndSaveCopy);
		}
		else if (mode == SendInvitationsMode.SendToNone)
		{
			await appointment.Save(calendarId, mode);
		}
		else
		{
			// Save(FolderId, SendToAll) ne conserve pas la copie organisateur sur ce serveur Exchange.
			await appointment.Save(SendInvitationsMode.SendToNone);
			StampSmtpOrganizer(appointment, email);
			await appointment.Update(ConflictResolutionMode.AlwaysOverwrite, SendInvitationsOrCancellationsMode.SendToAllAndSaveCopy);
		}
		await EnsureLegacyFreeBusyStatus(service, appointment, busyStatus);
		if (staleCopy != null && staleCopy.Id != null)
		{
			try
			{
				await staleCopy.Delete(DeleteMode.HardDelete);
			}
			catch
			{
			}
		}
		return new AddAppointmentRes
		{
			Id = appointment.Id?.UniqueId,
			Updated = updated
		};
	}

	private static LegacyFreeBusyStatus ToLegacyFreeBusyStatus(string value)
	{
		switch ((value ?? string.Empty).Trim().ToLowerInvariant())
		{
		case "free":
		case "0":
		case "disponible":
			return LegacyFreeBusyStatus.Free;
		case "tentative":
			return LegacyFreeBusyStatus.Tentative;
		case "oof":
		case "outofoffice":
			return LegacyFreeBusyStatus.OOF;
		default:
			return LegacyFreeBusyStatus.Busy;
		}
	}

	private static async System.Threading.Tasks.Task EnsureLegacyFreeBusyStatus(ExchangeService service, Appointment appointment, LegacyFreeBusyStatus status)
	{
		if (status == LegacyFreeBusyStatus.Busy || appointment?.Id == null)
		{
			return;
		}
		try
		{
			Appointment saved = await Appointment.Bind(service, appointment.Id, new PropertySet(BasePropertySet.IdOnly, AppointmentSchema.LegacyFreeBusyStatus));
			if (saved.LegacyFreeBusyStatus == status)
			{
				return;
			}
			saved.LegacyFreeBusyStatus = status;
			await saved.Update(ConflictResolutionMode.AlwaysOverwrite, SendInvitationsOrCancellationsMode.SendToNone);
		}
		catch
		{
		}
	}

	private static bool OrganizerIsMailbox(Appointment appointment, string email)
	{
		string organizer = appointment?.Organizer?.Address?.Trim() ?? string.Empty;
		if (organizer.Length == 0 || string.IsNullOrWhiteSpace(email) || organizer.IndexOf('@') < 0)
		{
			return true;
		}
		return string.Equals(organizer, email.Trim(), StringComparison.OrdinalIgnoreCase);
	}

	public Task<Item> FindItem(ExchangeService service, int eRefEvenement, bool loadProperties = false)
	{
		return FindItem(service, eRefEvenement, new FolderId(WellKnownFolderName.Calendar), loadProperties);
	}

	public async Task<Item> FindItem(ExchangeService service, int eRefEvenement, FolderId calendarId, bool loadProperties = false)
	{
		PropertySet ps = new PropertySet(ItemSchema.Subject, AppointmentSchema.Start, AppointmentSchema.End, AppointmentSchema.Location, ItemSchema.ReminderMinutesBeforeStart, AppointmentSchema.RequiredAttendees, AppointmentSchema.OptionalAttendees, ItemSchema.Body);
		CalendarFolder calendar = CalendarFolder.Bind(service, calendarId, ps).Result;
		ItemView view = new ItemView(1);
		SearchFilter.IsEqualTo filter = new SearchFilter.IsEqualTo(_extendedPropEventId, eRefEvenement);
		FindItemsResults<Item> res = await calendar.FindItems(filter, view);
		if (loadProperties && res.Count() > 0)
		{
			await service.LoadPropertiesForItems(res, ps);
		}
		return res.FirstOrDefault();
	}

	public async Task<RendezVous> FindAppointmentFromERefEvenement(string email, string password, int eRefEvenement, string appointmentId = null)
	{
		ExchangeService service = GetExchangeService(email, password);
		Item first = await FindItem(service, eRefEvenement, loadProperties: true);
		if (first == null && !string.IsNullOrWhiteSpace(appointmentId))
		{
			try
			{
				PropertySet ps = new PropertySet(ItemSchema.Subject, AppointmentSchema.Start, AppointmentSchema.End, AppointmentSchema.Location, ItemSchema.ReminderMinutesBeforeStart, AppointmentSchema.RequiredAttendees, AppointmentSchema.OptionalAttendees, ItemSchema.Body);
				first = await Appointment.Bind(service, new ItemId(appointmentId), ps);
			}
			catch (ServiceResponseException)
			{
				return null;
			}
		}
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
			Location = app.Location,
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
		return attendees.Select(AttendeeSmtp).Where((string address) => address.Length > 0).ToArray();
	}

	private static string AttendeeSmtp(Attendee attendee)
	{
		string fromAddress = ExchangeOrganizerIdentity.SmtpOrEmpty(attendee?.Address);
		if (fromAddress.Length > 0)
		{
			return fromAddress;
		}
		return ExchangeOrganizerIdentity.SmtpOrEmpty(attendee?.Name);
	}

	private static bool AddSmtpAttendees(AttendeeCollection attendees, string[] emails)
	{
		attendees.Clear();
		if (emails == null || emails.Length == 0)
		{
			return false;
		}
		bool any = false;
		foreach (string raw in emails)
		{
			string smtp = ExchangeOrganizerIdentity.SmtpOrEmpty(raw);
			if (smtp.Length == 0)
			{
				continue;
			}
			Attendee attendee = new Attendee(smtp);
			attendee.RoutingType = "SMTP";
			attendees.Add(attendee);
			any = true;
		}
		return any;
	}

	private static readonly ExtendedPropertyDefinition SentRepresentingName = new ExtendedPropertyDefinition(66, MapiPropertyType.String);

	private static readonly ExtendedPropertyDefinition SentRepresentingAddressType = new ExtendedPropertyDefinition(100, MapiPropertyType.String);

	private static readonly ExtendedPropertyDefinition SentRepresentingEmail = new ExtendedPropertyDefinition(101, MapiPropertyType.String);

	private static readonly ExtendedPropertyDefinition SentRepresentingSmtp = new ExtendedPropertyDefinition(23810, MapiPropertyType.String);

	private static readonly ExtendedPropertyDefinition SentRepresentingSearchKey = new ExtendedPropertyDefinition(59, MapiPropertyType.Binary);

	private static readonly ExtendedPropertyDefinition SentRepresentingEntryId = new ExtendedPropertyDefinition(65, MapiPropertyType.Binary);

	private static readonly ExtendedPropertyDefinition SenderName = new ExtendedPropertyDefinition(3098, MapiPropertyType.String);

	private static readonly ExtendedPropertyDefinition SenderAddressType = new ExtendedPropertyDefinition(3102, MapiPropertyType.String);

	private static readonly ExtendedPropertyDefinition SenderEmail = new ExtendedPropertyDefinition(3103, MapiPropertyType.String);

	private static readonly ExtendedPropertyDefinition SenderSmtp = new ExtendedPropertyDefinition(23809, MapiPropertyType.String);

	private static readonly ExtendedPropertyDefinition SenderSearchKey = new ExtendedPropertyDefinition(3101, MapiPropertyType.Binary);

	private static readonly ExtendedPropertyDefinition SenderEntryId = new ExtendedPropertyDefinition(3097, MapiPropertyType.Binary);

	private static void StampSmtpOrganizer(Appointment appointment, string email)
	{
		string smtp = ExchangeOrganizerIdentity.SmtpOrEmpty(email);
		if (appointment == null || smtp.Length == 0)
		{
			return;
		}
		if (appointment.Id != null)
		{
			appointment.RemoveExtendedProperty(SentRepresentingEntryId);
			appointment.RemoveExtendedProperty(SenderEntryId);
		}
		byte[] searchKey = Encoding.ASCII.GetBytes("SMTP:" + smtp.ToUpperInvariant() + "\0");
		appointment.SetExtendedProperty(SentRepresentingName, smtp);
		appointment.SetExtendedProperty(SentRepresentingAddressType, "SMTP");
		appointment.SetExtendedProperty(SentRepresentingEmail, smtp);
		appointment.SetExtendedProperty(SentRepresentingSmtp, smtp);
		appointment.SetExtendedProperty(SentRepresentingSearchKey, searchKey);
		appointment.SetExtendedProperty(SenderName, smtp);
		appointment.SetExtendedProperty(SenderAddressType, "SMTP");
		appointment.SetExtendedProperty(SenderEmail, smtp);
		appointment.SetExtendedProperty(SenderSmtp, smtp);
		appointment.SetExtendedProperty(SenderSearchKey, searchKey);
	}

	public async Task<bool> DeleteAppointmentFromERefEvenement(string email, string password, int eRefEvenement, string appointmentId = null)
	{
		ExchangeService service = GetExchangeService(email, password);
		FolderId calendarId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(email));
		bool deleted = await DeleteInCalendar(service, email, eRefEvenement, calendarId, appointmentId);
		foreach (string roomEmail in RoomMailboxEmails)
		{
			if (string.Equals(roomEmail, email, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			try
			{
				FolderId roomCalendar = new FolderId(WellKnownFolderName.Calendar, new Mailbox(roomEmail));
				if (await DeleteInCalendar(service, roomEmail, eRefEvenement, roomCalendar, null))
				{
					deleted = true;
				}
			}
			catch
			{
				// La salle n'est pas toujours accessible ; l'annulation organisateur la libère.
			}
		}
		return deleted;
	}

	private async Task<bool> DeleteInCalendar(ExchangeService service, string mailboxEmail, int eRefEvenement, FolderId calendarId, string appointmentId)
	{
		Appointment appointment = null;
		Item first = await FindItem(service, eRefEvenement, calendarId);
		PropertySet props = new PropertySet(BasePropertySet.FirstClassProperties, AppointmentSchema.Organizer, AppointmentSchema.IsMeeting);
		if (first != null)
		{
			appointment = await Appointment.Bind(service, first.Id, props);
		}
		else if (!string.IsNullOrWhiteSpace(appointmentId))
		{
			try
			{
				appointment = await Appointment.Bind(service, new ItemId(appointmentId), props);
			}
			catch (ServiceResponseException)
			{
				return false;
			}
		}
		if (appointment == null)
		{
			return false;
		}
		if (appointment.IsMeeting && OrganizerIsMailbox(appointment, mailboxEmail))
		{
			await appointment.Delete(DeleteMode.MoveToDeletedItems, SendCancellationsMode.SendToAllAndSaveCopy);
		}
		else
		{
			await appointment.Delete(DeleteMode.HardDelete);
		}
		return true;
	}
}
