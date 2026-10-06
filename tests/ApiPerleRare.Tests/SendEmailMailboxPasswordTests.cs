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

	private sealed class FakeMailboxLookup : IConseillerMailboxLookup
	{
		private readonly string _password;

		public FakeMailboxLookup(string password)
		{
			_password = password;
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
	}
}
