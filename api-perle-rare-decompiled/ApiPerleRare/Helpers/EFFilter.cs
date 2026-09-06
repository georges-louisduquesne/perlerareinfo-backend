namespace ApiPerleRare.Helpers;

public class EFFilter
{
	public string Where { get; set; }

	public string OrderBy { get; set; }

	public int? Take { get; set; }

	public int Skip { get; set; }
}
