using Xunit;
using ApiPerleRare.Application.Events;

namespace ApiPerleRare.Tests;

public class EncaissementsEnCoursFilterTests
{
	[Theory]
	[InlineData(null, null, true)]
	[InlineData(0, 0, true)]
	[InlineData(null, 0, true)]
	[InlineData(0, null, true)]
	[InlineData(12, null, false)]
	[InlineData(null, 4, false)]
	[InlineData(12, 4, false)]
	[InlineData(1, 0, false)]
	public void Waiting_for_payment_treats_zero_as_unissued(int? hon, int? ps, bool expected)
	{
		Assert.Equal(expected, EncaissementsEnCoursFilter.IsWaitingForPayment(hon, ps));
	}
}
