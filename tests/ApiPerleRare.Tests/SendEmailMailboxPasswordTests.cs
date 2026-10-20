using System;
using System.Threading.Tasks;
using ApiPerleRare.Application.Exchange;
using ApiPerleRare.Controllers;
using Xunit;

namespace ApiPerleRare.Tests;

public class SendEmailMailboxPasswordTests
{
	[Fact]
	public void Missing_client_password_is_filled_from_the_signed_in_conseiller()
	{
		Email email = new Email
		{
			SenderEmail = "ada@perle-rare.com",
			SenderName = "Ada",
			To = "client@example.com",
			Subject = "Bonjour"
		};
		string blocked = SendEmailUseCase.ApplyStoredMailboxPassword(email, 42, new FakeMailboxLookup("secret-mail"));
		Assert.Null(blocked);
		Assert.Equal("secret-mail", email.SenderPassword);
		Assert.Equal("ada@perle-rare.com", email.SenderEmail);
	}

	[Fact]
	public void Empty_stored_password_blocks_exchange_send()
	{
		Email email = new Email
		{
			SenderEmail = "ada@perle-rare.com",
			To = "client@example.com",
			Subject = "Bonjour"
		};
		string blocked = SendEmailUseCase.ApplyStoredMailboxPassword(email, 42, new FakeMailboxLookup("  "));
		Assert.Equal(SendEmailUseCase.MissingMailboxPassword, blocked);
		Assert.True(string.IsNullOrWhiteSpace(email.SenderPassword));
	}

	[Fact]
	public void Client_password_is_kept_when_already_provided()
	{
		Email email = new Email
		{
			SenderEmail = "ada@perle-rare.com",
			SenderPassword = "from-client"
		};
		string blocked = SendEmailUseCase.ApplyStoredMailboxPassword(email, 42, new FakeMailboxLookup("from-db"));
		Assert.Null(blocked);
		Assert.Equal("from-client", email.SenderPassword);
	}

	[Fact]
	public void Non_exchange_sender_does_not_need_a_mailbox_password()
	{
		Email email = new Email
		{
			SenderEmail = "ada@gmail.com"
		};
		string blocked = SendEmailUseCase.ApplyStoredMailboxPassword(email, 42, new FakeMailboxLookup(null));
		Assert.Null(blocked);
		Assert.True(string.IsNullOrWhiteSpace(email.SenderPassword));
	}

	[Fact]
	public void Non_admin_cannot_send_from_the_support_mailbox()
	{
		Email email = new Email
		{
			SenderEmail = "support@perle-rare.com",
			SenderName = "L'équipe Support",
			To = "marta.clery@perle-rare.com",
			Subject = "Facture d'honoraires"
		};
		string blocked = SendEmailUseCase.ApplyStoredMailboxPassword(
			email,
			42,
			new FakeMailboxLookup("counselor-secret", "support@perle-rare.com", "support-secret"),
			callerIsAdmin: false,
			out string loginEmail);
		Assert.Equal(ExchangeSendFailure.Denied("support@perle-rare.com"), blocked);
		Assert.Null(loginEmail);
		Assert.True(string.IsNullOrWhiteSpace(email.SenderPassword));
		Assert.Equal("support@perle-rare.com", email.SenderEmail);
	}

	[Fact]
	public void Admin_does_not_send_on_behalf_of_support_without_that_mailbox_password()
	{
		Email email = new Email
		{
			SenderEmail = "support@perle-rare.com",
			SenderName = "L'équipe Support"
		};
		string blocked = SendEmailUseCase.ApplyStoredMailboxPassword(
			email,
			42,
			new FakeMailboxLookup("secret-mail"),
			callerIsAdmin: true,
			out string loginEmail);
		Assert.Equal(ExchangeSendFailure.Denied("support@perle-rare.com"), blocked);
		Assert.Null(loginEmail);
		Assert.True(string.IsNullOrWhiteSpace(email.SenderPassword));
	}

	[Fact]
	public void Admin_sends_from_the_support_mailbox_itself()
	{
		Email email = new Email
		{
			SenderEmail = "support@perle-rare.com",
			SenderName = "L'équipe Support"
		};
		string blocked = SendEmailUseCase.ApplyStoredMailboxPassword(
			email,
			42,
			new FakeMailboxLookup("counselor-secret", "support@perle-rare.com", "support-secret"),
			callerIsAdmin: true,
			out string loginEmail);
		Assert.Null(blocked);
		Assert.Null(loginEmail);
		Assert.Equal("support-secret", email.SenderPassword);
		Assert.Equal("support@perle-rare.com", email.SenderEmail);
		Assert.Equal("L'équipe Support", email.SenderName);
	}

	[Fact]
	public void Exchange_failure_text_hides_the_raw_error()
	{
		Assert.Equal(
			"L’authentification Exchange a échoué.",
			ExchangeSendFailure.Message(new AggregateException(new Exception("401 Unauthorized")), "support@perle-rare.com"));
		Assert.Equal(
			"Le compte Exchange n’a pas le droit d’envoyer au nom de support@perle-rare.com.",
			ExchangeSendFailure.Message(new Exception("The user account does not have the right to send mail on behalf of the specified sending account."), "support@perle-rare.com"));
		Assert.Equal(
			"La pièce jointe est trop volumineuse pour Exchange.",
			ExchangeSendFailure.Message(new Exception("The message exceeds the maximum supported size."), null));
		Assert.Equal("L’envoi Exchange a échoué.", ExchangeSendFailure.Message(new Exception("password=secret"), "support@perle-rare.com"));
	}

	private sealed class FakeMailboxLookup : IConseillerMailboxLookup
	{
		private readonly string _password;

		private readonly string _sharedEmail;

		private readonly string _sharedPassword;

		public FakeMailboxLookup(string password, string sharedEmail = null, string sharedPassword = null)
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

		public Task<ConseillerMailbox> GetByConseillerIdAsync(int conseillerId)
		{
			return Task.FromResult(GetByConseillerId(conseillerId));
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
}
