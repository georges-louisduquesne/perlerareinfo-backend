using ApiPerleRare.Application.Files;
using Xunit;

namespace ApiPerleRare.Tests;

public class NetworkLogoPublicPathTests
{
	[Fact]
	public void Serves_network_logo_under_logos_reseaux()
	{
		Assert.True(AgencyLogoAddress.TryPublicFileName("180.png", "logos/reseaux", out string relative, out string contentType));
		Assert.Equal("logos/reseaux/180.png", relative);
		Assert.Equal("image/png", contentType);
	}

	[Fact]
	public void Normalizes_jpeg_and_keeps_agency_logos_on_the_agency_folder()
	{
		Assert.True(AgencyLogoAddress.TryPublicFileName("12.jpeg", "logos/reseaux", out string network, out string jpeg));
		Assert.Equal("logos/reseaux/12.jpg", network);
		Assert.Equal("image/jpeg", jpeg);

		Assert.True(AgencyLogoAddress.TryPublicFileName("4570.svg", out string agency, out string svg));
		Assert.Equal("logos/agences/4570.svg", agency);
		Assert.Equal("image/svg+xml", svg);
	}

	[Fact]
	public void Rejects_another_folder_and_a_non_image_name()
	{
		Assert.False(AgencyLogoAddress.TryPublicFileName("180.png", "logos/autres", out _, out _));
		Assert.False(AgencyLogoAddress.TryPublicFileName("../180.png", "logos/reseaux", out _, out _));
		Assert.False(AgencyLogoAddress.TryPublicFileName("logo.svg", "logos/reseaux", out _, out _));
	}
}
