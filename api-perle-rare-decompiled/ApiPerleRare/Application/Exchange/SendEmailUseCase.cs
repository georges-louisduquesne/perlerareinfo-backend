using System;
using ApiPerleRare.Controllers;
using Microsoft.Exchange.WebServices.Data;

namespace ApiPerleRare.Application.Exchange;

/// <summary>
/// Routes CRM mail: non-@perle-rare.com via localhost SMTP, else Exchange.
/// </summary>
public sealed class SendEmailUseCase : ISendEmailUseCase
{
	private readonly IExchangeService _exchange;

	private readonly ILocalhostMailService _localhostMail;

	public SendEmailUseCase(IExchangeService exchange, ILocalhostMailService localhostMail)
	{
		_exchange = exchange;
		_localhostMail = localhostMail;
	}

	public string Execute(Email em)
	{
		try
		{
			if (em.SenderEmail != null && !em.SenderEmail.EndsWith("@perle-rare.com"))
			{
				_localhostMail.SendMail(em.SenderName, em.SenderEmail, em.To, em.Subject, em.HtmlContents);
			}
			else
			{
				_exchange.SendMail(em.To, em.Subject, em.HtmlContents, delegate(EmailMessage e)
				{
					if (!string.IsNullOrWhiteSpace(em.SenderEmail))
					{
						e.Sender = new EmailAddress(em.SenderName, em.SenderEmail);
					}
				});
			}
			return "OK";
		}
		catch (Exception ex)
		{
			return ex.ToString();
		}
	}
}
