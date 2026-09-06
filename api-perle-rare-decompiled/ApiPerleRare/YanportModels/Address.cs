namespace ApiPerleRare.YanportModels;

public class Address
{
	public long CityId { get; set; }

	public long QuarterId { get; set; }

	public string Street { get; set; }

	public string StreetNumber { get; set; }

	public string ZipCode { get; set; }

	public Location Location { get; set; }
}
