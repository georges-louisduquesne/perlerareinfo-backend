using System;
using System.Collections.Generic;
using System.Text.Json;
using ApiPerleRare.Application.Exchange;
using ApiPerleRare.Controllers;
using Microsoft.Exchange.WebServices.Data;
using Xunit;

namespace ApiPerleRare.Tests;

public class SendEmailAttachmentTests
{
	[Fact]
	public void Exchange_send_forwards_attachments_and_cc()
	{
		CapturingExchange exchange = new CapturingExchange();
		SendEmailUseCase useCase = new SendEmailUseCase(exchange, new NoopLocalhostMail(), new StoredMailbox("secret"));
		byte[] pdf = new byte[] { 0x25, 0x50, 0x44, 0x46 };
		string result = useCase.Execute(new Email
		{
			SenderEmail = "ada@perle-rare.com",
			SenderName = "Ada",
			To = "client@example.com",
			Cc = "copie@example.com",
			Subject = "Dossier",
			HtmlContents = "<p>Bonjour</p>",
			Attachments = new[]
			{
				new EmailAttachment
				{
					FileName = "mandat.pdf",
					ContentType = "application/pdf",
					ContentBase64 = Convert.ToBase64String(pdf)
				}
			}
		}, 42);

		Assert.Equal("OK", result);
		Assert.Equal(1, exchange.SendCount);
		Assert.NotNull(exchange.LastAttachments);
		Assert.Single(exchange.LastAttachments);
		Assert.Equal("mandat.pdf", exchange.LastAttachments[0].FileName);
		Assert.Equal(pdf, exchange.LastAttachments[0].Content);
		Assert.Equal(new[] { "copie@example.com" }, exchange.LastCc);
	}

	[Fact]
	public void Unreadable_attachment_is_not_sent_without_the_file()
	{
		CapturingExchange exchange = new CapturingExchange();
		SendEmailUseCase useCase = new SendEmailUseCase(exchange, new NoopLocalhostMail(), new StoredMailbox("secret"));
		string result = useCase.Execute(new Email
		{
			SenderEmail = "ada@perle-rare.com",
			To = "client@example.com",
			Subject = "Dossier",
			HtmlContents = "<p>Bonjour</p>",
			Attachments = new[]
			{
				new EmailAttachment
				{
					FileName = "mandat.pdf",
					ContentBase64 = "%%%"
				}
			}
		}, 42);

		Assert.NotEqual("OK", result);
		Assert.Equal(0, exchange.SendCount);
	}

	[Fact]
	public void Invoice_from_support_authenticates_as_the_support_mailbox_for_an_admin()
	{
		CapturingExchange exchange = new CapturingExchange();
		SendEmailUseCase useCase = new SendEmailUseCase(exchange, new NoopLocalhostMail(), new StoredMailbox("secret", "support@perle-rare.com", "support-secret"));
		string result = useCase.Execute(new Email
		{
			SenderEmail = "support@perle-rare.com",
			SenderName = "L'équipe Support",
			To = "marta.clery@perle-rare.com",
			Subject = "Facture d'honoraires de Thomas WOLFE",
			HtmlContents = "<p>Bonjour Marta.</p>",
			Attachments = new[]
			{
				new EmailAttachment
				{
					FileName = "Facture honoraires Thomas WOLFE.pdf",
					ContentType = "application/pdf",
					ContentBase64 = Convert.ToBase64String(new byte[] { 0x25, 0x50, 0x44, 0x46 })
				}
			}
		}, 42, callerIsAdmin: true);

		Assert.Equal("OK", result);
		Assert.Equal(1, exchange.SendCount);
		Assert.Equal("support@perle-rare.com", exchange.LastSenderEmail);
		Assert.Null(exchange.LastLoginEmail);
		Assert.Equal("support-secret", exchange.LastPassword);
		Assert.Equal("Facture honoraires Thomas WOLFE.pdf", exchange.LastAttachments[0].FileName);
	}

	[Fact]
	public void Invoice_from_support_is_refused_for_a_non_admin()
	{
		CapturingExchange exchange = new CapturingExchange();
		SendEmailUseCase useCase = new SendEmailUseCase(exchange, new NoopLocalhostMail(), new StoredMailbox("secret", "support@perle-rare.com", "support-secret"));
		string result = useCase.Execute(new Email
		{
			SenderEmail = "support@perle-rare.com",
			SenderName = "L'équipe Support",
			To = "marta.clery@perle-rare.com",
			Subject = "Facture d'honoraires",
			HtmlContents = "<p>Bonjour</p>"
		}, 42, callerIsAdmin: false);

		Assert.Equal(ExchangeSendFailure.Denied("support@perle-rare.com"), result);
		Assert.Equal(0, exchange.SendCount);
	}

	[Fact]
	public void Mail_without_attachment_still_sends()
	{
		CapturingExchange exchange = new CapturingExchange();
		SendEmailUseCase useCase = new SendEmailUseCase(exchange, new NoopLocalhostMail(), new StoredMailbox("secret"));
		string result = useCase.Execute(new Email
		{
			SenderEmail = "ada@perle-rare.com",
			To = "client@example.com",
			Subject = "Bonjour",
			HtmlContents = "<p>Bonjour</p>"
		}, 42);

		Assert.Equal("OK", result);
		Assert.Equal(1, exchange.SendCount);
		Assert.Null(exchange.LastAttachments);
		Assert.Null(exchange.LastCc);
	}

	[Fact]
	public void Localhost_send_forwards_attachments()
	{
		CapturingLocalhostMail localhost = new CapturingLocalhostMail();
		SendEmailUseCase useCase = new SendEmailUseCase(new CapturingExchange(), localhost, new StoredMailbox("secret"));
		byte[] pdf = new byte[] { 0x25, 0x50, 0x44, 0x46 };
		string result = useCase.Execute(new Email
		{
			SenderEmail = "ada@gmail.com",
			SenderName = "Ada",
			To = "client@example.com",
			Subject = "Dossier",
			HtmlContents = "<p>Bonjour</p>",
			Attachments = new[]
			{
				new EmailAttachment
				{
					FileName = "mandat.pdf",
					ContentType = "application/pdf",
					ContentBase64 = Convert.ToBase64String(pdf)
				}
			}
		}, 42);

		Assert.Equal("OK", result);
		Assert.Equal(1, localhost.SendCount);
		Assert.NotNull(localhost.LastAttachments);
		Assert.Single(localhost.LastAttachments);
		Assert.Equal("mandat.pdf", localhost.LastAttachments[0].FileName);
		Assert.Equal(pdf, localhost.LastAttachments[0].Content);
	}

	[Fact]
	public void Compose_json_binds_attachment_bytes()
	{
		string json = "{\"senderEmail\":\"ada@perle-rare.com\",\"to\":\"client@example.com\",\"subject\":\"Dossier\",\"htmlContents\":\"<p>Bonjour</p>\",\"cc\":\"copie@example.com\",\"attachments\":[{\"fileName\":\"mandat.pdf\",\"contentType\":\"application/pdf\",\"contentBase64\":\"JVBERg==\"}]}";
		Email email = JsonSerializer.Deserialize<Email>(json, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		});
		Assert.NotNull(email.Attachments);
		Assert.Single(email.Attachments);
		Assert.Equal("mandat.pdf", email.Attachments[0].FileName);
		Assert.Equal("application/pdf", email.Attachments[0].ContentType);
		Assert.Equal("JVBERg==", email.Attachments[0].ContentBase64);
		Assert.Equal("copie@example.com", email.Cc);
	}

	private sealed class StoredMailbox : IConseillerMailboxLookup
	{
		private readonly string _password;

		private readonly string _sharedEmail;

		private readonly string _sharedPassword;

		public StoredMailbox(string password, string sharedEmail = null, string sharedPassword = null)
		{
			_password = password;
			_sharedEmail = sharedEmail;
			_sharedPassword = sharedPassword;
		}

		public ConseillerMailbox GetByConseillerId(int conseillerId)
		{
			return new ConseillerMailbox
			{
				Email = "ada@perle-rare.com",
				Password = _password,
				DisplayName = "Ada"
			};
		}

		public System.Threading.Tasks.Task<ConseillerMailbox> GetByConseillerIdAsync(int conseillerId)
		{
			return System.Threading.Tasks.Task.FromResult(GetByConseillerId(conseillerId));
		}

		public ConseillerMailbox GetByEmail(string email)
		{
			if (!string.IsNullOrWhiteSpace(_sharedEmail)
				&& string.Equals((email ?? string.Empty).Trim(), _sharedEmail, StringComparison.OrdinalIgnoreCase))
			{
				return new ConseillerMailbox
				{
					Email = _sharedEmail,
					Password = _sharedPassword,
					DisplayName = "Support"
				};
			}
			throw new Exception("Aucun conseiller pour cette adresse");
		}
	}

	private sealed class CapturingLocalhostMail : ILocalhostMailService
	{
		public int SendCount { get; private set; }

		public MailAttachment[] LastAttachments { get; private set; }

		public bool SendMail(string senderName, string senderEmail, string to, string subject, string contents, string cc = null, MailAttachment[] attachments = null)
		{
			SendCount++;
			LastAttachments = attachments;
			return true;
		}
	}

	private sealed class NoopLocalhostMail : ILocalhostMailService
	{
		public bool SendMail(string senderName, string senderEmail, string to, string subject, string contents, string cc = null, MailAttachment[] attachments = null)
		{
			return true;
		}
	}

	private sealed class CapturingExchange : IExchangeService
	{
		public int SendCount { get; private set; }

		public MailAttachment[] LastAttachments { get; private set; }

		public string[] LastCc { get; private set; }

		public string LastSenderEmail { get; private set; }

		public string LastLoginEmail { get; private set; }

		public string LastPassword { get; private set; }

		public TimeZoneInfo CurrentTimeZone => TimeZoneInfo.Utc;

		public bool SendMail(string to, string subject, string contents, Action<EmailMessage> complete = null)
		{
			throw new NotImplementedException();
		}

		public bool SendMail(string subject, string contents, string senderName, string senderEmail, string senderPassword, string[] to, string[] cc, string[] cci, MailAttachment[] attachments, bool highImportance, Action<EmailMessage> complete = null, string loginEmail = null)
		{
			SendCount++;
			LastCc = cc;
			LastAttachments = attachments;
			LastSenderEmail = senderEmail;
			LastLoginEmail = loginEmail;
			LastPassword = senderPassword;
			return true;
		}

		public int GetUnreadEmails(string email, string password)
		{
			throw new NotImplementedException();
		}

		public bool IsUserAvailable(string callerEmail, string callerPassword, string targetEmail, DateTime start, DateTime end)
		{
			throw new NotImplementedException();
		}

		public List<SalonCalendarEvent> GetSalonCalendar(string callerEmail, string callerPassword, DateTime start, DateTime end)
		{
			throw new NotImplementedException();
		}

		public TodayAppointmentsResponse GetTodayAppointments(string email, string password)
		{
			throw new NotImplementedException();
		}

		public List<ExchangeAppointmentItem> GetAppointments(string email, string password, DateTime from, DateTime to)
		{
			throw new NotImplementedException();
		}

		public System.Threading.Tasks.Task<AddAppointmentRes> AddAppointment(string email, string password, RendezVous rendezVous)
		{
			throw new NotImplementedException();
		}

		public System.Threading.Tasks.Task<RendezVous> FindAppointmentFromERefEvenement(string cpMel, string cpMelMotDePasse, int eRefEvenement, string appointmentId = null)
		{
			throw new NotImplementedException();
		}

		public System.Threading.Tasks.Task<bool> DeleteAppointmentFromERefEvenement(string cpMel, string cpMelMotDePasse, int eRefEvenement, string appointmentId = null)
		{
			throw new NotImplementedException();
		}

		public System.Threading.Tasks.Task<bool> DeleteAppointmentById(string cpMel, string cpMelMotDePasse, string appointmentId)
		{
			throw new NotImplementedException();
		}
	}
}
