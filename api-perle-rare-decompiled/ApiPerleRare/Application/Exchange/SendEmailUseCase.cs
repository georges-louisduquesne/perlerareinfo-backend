using System;
using System.Linq;
using ApiPerleRare.Controllers;

namespace ApiPerleRare.Application.Exchange;

/// <summary>
/// Routes CRM mail: non-@perle-rare.com via localhost SMTP, else Exchange
/// with the conseiller mailbox (copy in Sent Items), like demo/prod.
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
			if (em == null || string.IsNullOrWhiteSpace(em.To) || string.IsNullOrWhiteSpace(em.Subject))
			{
				return "Destinataire ou objet manquant.";
			}
			string[] to = SplitAddresses(em.To);
			if (to.Length == 0)
			{
				return "Destinataire ou objet manquant.";
			}
			bool sent;
			if (em.SenderEmail != null && !em.SenderEmail.EndsWith("@perle-rare.com", StringComparison.OrdinalIgnoreCase))
			{
				sent = _localhostMail.SendMail(em.SenderName, em.SenderEmail, string.Join(";", to), em.Subject, em.HtmlContents);
			}
			else
			{
				sent = _exchange.SendMail(
					em.Subject,
					em.HtmlContents ?? "",
					em.SenderName,
					em.SenderEmail,
					em.SenderPassword,
					to,
					null,
					null,
					null,
					em.HighImportance);
			}
			return sent ? "OK" : "L’envoi Exchange a échoué.";
		}
		catch (Exception ex)
		{
			return ex.ToString();
		}
	}

	internal static string[] SplitAddresses(string raw)
	{
		return (raw ?? "")
			.Split(new[] { ';', ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
			.Select(part => part.Trim())
			.Where(part => part.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}
}
