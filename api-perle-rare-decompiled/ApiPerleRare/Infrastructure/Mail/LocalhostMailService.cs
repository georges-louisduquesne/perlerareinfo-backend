using System;
using System.IO;
using System.Net.Mail;

namespace ApiPerleRare.Infrastructure.Mail;

public class LocalhostMailService : ILocalhostMailService
{
	public bool SendMail(string senderName, string senderEmail, string to, string subject, string contents, string cc = null, MailAttachment[] attachments = null)
	{
		MailMessage mailMessage = new MailMessage();
		MailAddress fromAddress = new MailAddress(senderEmail, senderName);
		mailMessage.From = fromAddress;
		mailMessage.To.Add(to);
		if (!string.IsNullOrWhiteSpace(cc))
		{
			foreach (string address in cc.Split(new[] { ';', ',', '\n' }, StringSplitOptions.RemoveEmptyEntries))
			{
				string trimmed = address.Trim();
				if (trimmed.Length > 0)
				{
					mailMessage.CC.Add(trimmed);
				}
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
		mailMessage.Body = contents;
		mailMessage.IsBodyHtml = true;
		mailMessage.Subject = subject;
		SmtpClient smtpClient = new SmtpClient();
		smtpClient.Host = "localhost";
		smtpClient.Send(mailMessage);
		return true;
	}
}
