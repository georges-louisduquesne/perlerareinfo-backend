using System;
using System.Collections.Generic;
using System.Linq;
using ApiPerleRare.Controllers;

namespace ApiPerleRare.Application.Exchange;

/// <summary>
/// Routes CRM mail: non-@perle-rare.com via localhost SMTP, else Exchange
/// with the conseiller mailbox (copy in Sent Items), like demo/prod.
/// </summary>
public sealed class SendEmailUseCase : ISendEmailUseCase
{
	internal const string MissingMailboxPassword = "Mot de passe Exchange du conseiller introuvable.";

	internal const string UnreadableAttachment = "Impossible de lire une pièce jointe.";

	private readonly IExchangeService _exchange;

	private readonly ILocalhostMailService _localhostMail;

	private readonly IConseillerMailboxLookup _mailboxes;

	public SendEmailUseCase(IExchangeService exchange, ILocalhostMailService localhostMail, IConseillerMailboxLookup mailboxes)
	{
		_exchange = exchange;
		_localhostMail = localhostMail;
		_mailboxes = mailboxes;
	}

	public string Execute(Email em, int? conseillerId)
	{
		try
		{
			string blocked = ApplyStoredMailboxPassword(em, conseillerId, _mailboxes);
			if (blocked != null)
			{
				return blocked;
			}
			if (em == null || string.IsNullOrWhiteSpace(em.To) || string.IsNullOrWhiteSpace(em.Subject))
			{
				return "Destinataire ou objet manquant.";
			}
			string[] to = SplitAddresses(em.To);
			if (to.Length == 0)
			{
				return "Destinataire ou objet manquant.";
			}
			if (!TryReadAttachments(em.Attachments, out MailAttachment[] attachments, out string attachmentError))
			{
				return attachmentError;
			}
			string[] cc = string.IsNullOrWhiteSpace(em.Cc) ? null : SplitAddresses(em.Cc);
			if (cc != null && cc.Length == 0)
			{
				cc = null;
			}
			bool sent;
			if (em.SenderEmail != null && !em.SenderEmail.EndsWith("@perle-rare.com", StringComparison.OrdinalIgnoreCase))
			{
				sent = _localhostMail.SendMail(
					em.SenderName,
					em.SenderEmail,
					string.Join(";", to),
					em.Subject,
					em.HtmlContents,
					cc == null ? null : string.Join(";", cc),
					attachments);
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
					cc,
					null,
					attachments,
					em.HighImportance);
			}
			return sent ? "OK" : "L’envoi Exchange a échoué.";
		}
		catch (Exception ex)
		{
			return ex.ToString();
		}
	}

	internal static string ApplyStoredMailboxPassword(Email em, int? conseillerId, IConseillerMailboxLookup mailboxes)
	{
		if (em == null || !string.IsNullOrWhiteSpace(em.SenderPassword) || !conseillerId.HasValue)
		{
			return null;
		}
		if (!NeedsExchangePassword(em.SenderEmail))
		{
			return null;
		}
		ConseillerMailbox mailbox = null;
		try
		{
			mailbox = mailboxes?.GetByConseillerId(conseillerId.Value);
		}
		catch (Exception)
		{
			mailbox = null;
		}
		if (mailbox == null || string.IsNullOrWhiteSpace(mailbox.Password))
		{
			return MissingMailboxPassword;
		}
		em.SenderPassword = mailbox.Password;
		if (string.IsNullOrWhiteSpace(em.SenderEmail))
		{
			em.SenderEmail = mailbox.Email;
		}
		if (string.IsNullOrWhiteSpace(em.SenderName))
		{
			em.SenderName = mailbox.DisplayName;
		}
		return null;
	}

	internal static bool NeedsExchangePassword(string senderEmail)
	{
		if (string.IsNullOrWhiteSpace(senderEmail))
		{
			return true;
		}
		return senderEmail.Trim().EndsWith("@perle-rare.com", StringComparison.OrdinalIgnoreCase);
	}

	internal static bool TryReadAttachments(EmailAttachment[] source, out MailAttachment[] attachments, out string error)
	{
		attachments = null;
		error = null;
		if (source == null || source.Length == 0)
		{
			return true;
		}
		List<MailAttachment> list = new List<MailAttachment>();
		foreach (EmailAttachment item in source)
		{
			if (item == null)
			{
				continue;
			}
			string raw = item.ContentBase64 ?? "";
			int dataPrefix = raw.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
			if (dataPrefix >= 0)
			{
				raw = raw.Substring(dataPrefix + "base64,".Length);
			}
			raw = raw.Trim();
			bool hasName = !string.IsNullOrWhiteSpace(item.FileName);
			if (raw.Length == 0 && !hasName)
			{
				continue;
			}
			if (raw.Length == 0)
			{
				error = UnreadableAttachment;
				return false;
			}
			byte[] content;
			try
			{
				content = Convert.FromBase64String(raw);
			}
			catch (FormatException)
			{
				error = UnreadableAttachment;
				return false;
			}
			if (content.Length == 0)
			{
				error = UnreadableAttachment;
				return false;
			}
			list.Add(new MailAttachment(hasName ? item.FileName.Trim() : "piece-jointe", content));
		}
		attachments = list.Count == 0 ? null : list.ToArray();
		return true;
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
