using System.Net.Mail;

namespace ApiPerleRare.Infrastructure.Mail;

public class LocalhostMailService : ILocalhostMailService
{
	public bool SendMail(string senderName, string senderEmail, string to, string subject, string contents)
	{
		MailMessage mailMessage = new MailMessage();
		MailAddress fromAddress = new MailAddress(senderEmail, senderName);
		mailMessage.From = fromAddress;
		mailMessage.To.Add(to);
		mailMessage.Body = contents;
		mailMessage.IsBodyHtml = true;
		mailMessage.Subject = subject;
		SmtpClient smtpClient = new SmtpClient();
		smtpClient.Host = "localhost";
		smtpClient.Send(mailMessage);
		return true;
	}
}
