using ApiPerleRare.Application.PropertyContacts;
using Xunit;

namespace ApiPerleRare.Tests;

public class ContactSharedIndicatorsTests
{
	[Fact]
	public void Request_accepts_empty_property_ids()
	{
		var req = new ContactSharedIndicatorsRequest { PropertyIds = null };
		Assert.Null(req.PropertyIds);
	}
}
