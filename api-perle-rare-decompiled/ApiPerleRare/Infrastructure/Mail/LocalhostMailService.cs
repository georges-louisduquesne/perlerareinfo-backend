using System;
using System.IO;
using System.Net.Mail;
using System.Text;
using ApiPerleRare.Application.Exchange;

namespace ApiPerleRare.Infrastructure.Mail;

public class LocalhostMailService : ILocalhostMailService
{
	public bool SendMail(string senderName, string senderEmail, string to, string subject, string contents, string cc = null, MailAttachment[] attachments = null)
	{
		using MailMessage mailMessage = BuildMessage(senderName, senderEmail, to, subject, contents, cc, attachments);
		using SmtpClient smtpClient = new SmtpClient("localhost");
		smtpClient.Send(mailMessage);
		return true;
	}

	internal static MailMessage BuildMessage(string senderName, string senderEmail, string to, string subject, string contents, string cc = null, MailAttachment[] attachments = null)
	{
		MailMessage mailMessage = new MailMessage();
		mailMessage.From = string.IsNullOrWhiteSpace(senderName)
			? new MailAddress(senderEmail)
			: new MailAddress(senderEmail, senderName, Encoding.UTF8);
		foreach (string address in SendEmailUseCase.SplitAddresses(to))
		{
			mailMessage.To.Add(address);
		}
		if (!string.IsNullOrWhiteSpace(cc))
		{
			foreach (string address in SendEmailUseCase.SplitAddresses(cc))
			{
				mailMessage.CC.Add(address);
			}
		}
		if (attachments != null)
		{
			foreach (MailAttachment att in attachments)
			{
				if (att?.Content == null || att.Content.Length == 0)
				{
					continue;
				}
				MemoryStream stream = new MemoryStream(att.Content);
				string name = string.IsNullOrWhiteSpace(att.FileName) ? "piece-jointe" : att.FileName;
				mailMessage.Attachments.Add(new Attachment(stream, name));
			}
		}
		mailMessage.HeadersEncoding = Encoding.UTF8;
		mailMessage.SubjectEncoding = Encoding.UTF8;
		mailMessage.BodyEncoding = Encoding.UTF8;
		mailMessage.Body = ExchangeMailHtml.Prepare(contents);
		mailMessage.IsBodyHtml = true;
		mailMessage.Subject = subject ?? string.Empty;
		return mailMessage;
	}
}
