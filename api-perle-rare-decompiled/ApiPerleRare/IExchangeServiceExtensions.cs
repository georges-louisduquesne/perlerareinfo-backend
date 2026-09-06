using System;
using Newtonsoft.Json;

namespace ApiPerleRare;

public static class IExchangeServiceExtensions
{
	public static void SendWarning(this IExchangeService exchangeService, string subject, string htmlBody)
	{
		try
		{
			exchangeService.SendMail("huberje@yahoo.fr", subject, htmlBody);
		}
		catch
		{
		}
	}

	public static void SendError(this IExchangeService exchangeService, string subject, string htmlBody)
	{
		try
		{
			exchangeService.SendMail("huberje@yahoo.fr", subject, htmlBody);
			exchangeService.SendMail("georges-louis.duquesne@perle-rare.com", subject, htmlBody);
		}
		catch
		{
		}
	}

	public static void LogException(this IExchangeService exchangeService, string subject, object post, Exception ex)
	{
		string htmlBody = "POST : <br/>";
		htmlBody = ((post != null) ? (htmlBody + JsonConvert.SerializeObject(post, Formatting.Indented)) : (htmlBody + "-- aucun post --"));
		htmlBody += "<br/>";
		if (ex != null)
		{
			htmlBody = htmlBody + "<br/>EXCEPTION : <br/>" + ex.ToString();
		}
		exchangeService.SendWarning(subject, htmlBody);
	}
}
