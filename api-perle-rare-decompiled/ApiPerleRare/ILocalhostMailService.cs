namespace ApiPerleRare;

public interface ILocalhostMailService
{
	bool SendMail(string senderName, string senderEmail, string to, string subject, string contents);
}
