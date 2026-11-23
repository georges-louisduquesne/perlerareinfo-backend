using ApiPerleRare.Models;
using Xunit;

namespace ApiPerleRare.Tests;

public class ContactAmountDecimalTests
{
	[Fact]
	public void Frais_percus_and_honoraires_keep_cents()
	{
		ContactsRecherche row = new()
		{
			CFraisPercus = 1200.50m,
			CMttHono = 13312.50m
		};

		Assert.Equal(1200.50m, row.CFraisPercus);
		Assert.Equal(13312.50m, row.CMttHono);
	}
}
