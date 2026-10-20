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

	/// <summary>Boîte partagée : les admins s’y connectent, ils n’envoient pas « au nom de ».</summary>
	internal const string SupportFromAddress = "support@perle-rare.com";

	private readonly IExchangeService _exchange;

	private readonly ILocalhostMailService _localhostMail;

	private readonly IConseillerMailboxLookup _mailboxes;

	public SendEmailUseCase(IExchangeService exchange, ILocalhostMailService localhostMail, IConseillerMailboxLookup mailboxes)
	{
		_exchange = exchange;
		_localhostMail = localhostMail;
		_mailboxes = mailboxes;
	}

	public string Execute(Email em, int? conseillerId, bool callerIsAdmin = false)
	{
		try
		{
			string blocked = ApplyStoredMailboxPassword(em, conseillerId, _mailboxes, callerIsAdmin, out string loginEmail);
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
					em.HighImportance,
					loginEmail: loginEmail);
			}
			return sent ? "OK" : "L’envoi Exchange a échoué.";
		}
		catch (ExchangeSendException ex)
		{
			return ex.Message;
		}
		catch (Exception ex)
		{
			return ex.ToString();
		}
	}

	internal static string ApplyStoredMailboxPassword(Email em, int? conseillerId, IConseillerMailboxLookup mailboxes)
	{
		return ApplyStoredMailboxPassword(em, conseillerId, mailboxes, callerIsAdmin: false, out _);
	}

	internal static string ApplyStoredMailboxPassword(Email em, int? conseillerId, IConseillerMailboxLookup mailboxes, out string loginEmail)
	{
		return ApplyStoredMailboxPassword(em, conseillerId, mailboxes, callerIsAdmin: false, out loginEmail);
	}

	/// <summary>
	/// Fills the Exchange password. <c>support@</c> is sent by opening that
	/// mailbox (admins only). Another From address keeps the signed-in mailbox
	/// as <paramref name="loginEmail"/> and the requested address as the visible From.
	/// </summary>
	internal static string ApplyStoredMailboxPassword(Email em, int? conseillerId, IConseillerMailboxLookup mailboxes, bool callerIsAdmin, out string loginEmail)
	{
		loginEmail = null;
		if (em == null || !conseillerId.HasValue)
		{
			return null;
		}
		if (!string.IsNullOrWhiteSpace(em.SenderPassword) && !IsSupportFrom(em.SenderEmail))
		{
			return null;
		}
		if (!NeedsExchangePassword(em.SenderEmail))
		{
			return null;
		}
		if (IsSupportFrom(em.SenderEmail))
		{
			// Le mot de passe du conseiller ne doit pas ouvrir support@ ni servir à un envoi « au nom de ».
			em.SenderPassword = null;
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
		string requested = (em.SenderEmail ?? string.Empty).Trim();
		string ownEmail = (mailbox.Email ?? string.Empty).Trim();
		if (requested.Length > 0 && !string.Equals(requested, ownEmail, StringComparison.OrdinalIgnoreCase))
		{
			if (IsSupportFrom(requested) && !callerIsAdmin)
			{
				return ExchangeSendFailure.Denied(requested);
			}
			ConseillerMailbox shared = TryMailboxByEmail(mailboxes, requested);
			if (shared != null && !string.IsNullOrWhiteSpace(shared.Password)
				&& string.Equals((shared.Email ?? string.Empty).Trim(), requested, StringComparison.OrdinalIgnoreCase))
			{
				em.SenderPassword = shared.Password;
				if (string.IsNullOrWhiteSpace(em.SenderName))
				{
					em.SenderName = shared.DisplayName;
				}
				return null;
			}
			if (IsSupportFrom(requested))
			{
				// Pas de mot de passe pour support@ : ne pas ouvrir la boîte du conseiller
				// et poser un From étranger. Exchange répond SendAsDenied.
				return ExchangeSendFailure.Denied(requested);
			}
			loginEmail = ownEmail;
			em.SenderPassword = mailbox.Password;
			return null;
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

	private static ConseillerMailbox TryMailboxByEmail(IConseillerMailboxLookup mailboxes, string email)
	{
		if (mailboxes == null || string.IsNullOrWhiteSpace(email))
		{
			return null;
		}
		try
		{
			return mailboxes.GetByEmail(email.Trim());
		}
		catch (Exception)
		{
			return null;
		}
	}

	internal static bool IsSupportFrom(string email)
	{
		return string.Equals((email ?? string.Empty).Trim(), SupportFromAddress, StringComparison.OrdinalIgnoreCase);
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
