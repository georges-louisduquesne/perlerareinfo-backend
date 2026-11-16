using ApiPerleRare.Helpers;
using Xunit;

namespace ApiPerleRare.Tests;

public class BienNotificationGateTests
{
	[Theory]
	[InlineData(1, false, true)]
	[InlineData(0, false, false)]
	[InlineData(1, true, false)]
	[InlineData(0, true, false)]
	[InlineData(2, false, false)]
	public void Mail_goes_out_only_when_the_switch_is_on_and_no_closing_event_is_active(sbyte alerteMail, bool blockingEvent, bool allows)
	{
		Assert.Equal(allows, BienNotificationGate.Allows(alerteMail, blockingEvent));
	}

	[Theory]
	[InlineData("RV COMPROMIS", true)]
	[InlineData("rv compromis", true)]
	[InlineData("RV ACTE AUTHENT.", true)]
	[InlineData("RV ACTE AUTHENT", true)]
	[InlineData(" rv acte authent. ", true)]
	[InlineData("OFFRE", false)]
	[InlineData("RV VISITE CLIENT", false)]
	[InlineData("OFFRE ACCEPTEE", false)]
	[InlineData("", false)]
	[InlineData(null, false)]
	public void Only_compromis_and_acte_authentique_block_property_mails(string type, bool blocking)
	{
		Assert.Equal(blocking, BienNotificationGate.IsBlockingType(type));
	}
}
