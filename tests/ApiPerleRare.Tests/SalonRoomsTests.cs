using ApiPerleRare.Services;
using Xunit;

namespace ApiPerleRare.Tests;

public class SalonRoomsTests
{
	[Fact]
	public void Calendar_lists_vendome_saint_honore_and_opera()
	{
		Assert.Equal(
			new[]
			{
				("panoramique", "grande_salle@perle-rare.com"),
				("bis", "salon@perle-rare.com"),
				("opera", "salon_opera@perle-rare.com")
			},
			PRExchangeService.SalonRooms);
	}
}
