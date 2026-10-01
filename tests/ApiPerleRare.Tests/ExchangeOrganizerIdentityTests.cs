using ApiPerleRare.Application.Exchange;
using Xunit;

namespace ApiPerleRare.Tests;

public class ExchangeOrganizerIdentityTests
{
	[Fact]
	public void Legacy_dn_is_not_an_smtp_address()
	{
		const string dn = "/o=MyOranization/ou=Exchange Administrative Group (FYDIBOHF23SPDLT)/cn=Recipients/cn=7b4ccc8f10964589b3e62a2535933613-tsY-a60e1f";
		Assert.True(ExchangeOrganizerIdentity.IsLegacyDn(dn));
		Assert.Equal(string.Empty, ExchangeOrganizerIdentity.SmtpOrEmpty(dn));
	}

	[Fact]
	public void Smtp_address_is_kept()
	{
		Assert.False(ExchangeOrganizerIdentity.IsLegacyDn("ada@perle-rare.com"));
		Assert.Equal("ada@perle-rare.com", ExchangeOrganizerIdentity.SmtpOrEmpty("  ada@perle-rare.com  "));
	}
}
