using System;
using System.Threading.Tasks;
using ApiPerleRare.Controllers;
using Microsoft.Exchange.WebServices.Data;

namespace ApiPerleRare;

public interface IExchangeService
{
	TimeZoneInfo CurrentTimeZone { get; }

	bool SendMail(string to, string subject, string contents, Action<EmailMessage> complete = null);

	bool SendMail(string subject, string contents, string senderName, string senderEmail, string senderPassword, string[] to, string[] cc, string[] cci, MailAttachment[] attachments, bool highImportance, Action<EmailMessage> complete = null);

	int GetUnreadEmails(string email, string password);

	bool IsUserAvailable(string callerEmail, string callerPassword, string targetEmail, DateTime start, DateTime end);

	System.Collections.Generic.List<SalonCalendarEvent> GetSalonCalendar(string callerEmail, string callerPassword, DateTime start, DateTime end);

	TodayAppointmentsResponse GetTodayAppointments(string email, string password);

	Task<AddAppointmentRes> AddAppointment(string email, string password, RendezVous rendezVous);

	Task<RendezVous> FindAppointmentFromERefEvenement(string cpMel, string cpMelMotDePasse, int eRefEvenement, string appointmentId = null);

	Task<bool> DeleteAppointmentFromERefEvenement(string cpMel, string cpMelMotDePasse, int eRefEvenement, string appointmentId = null);
}
