namespace ApiPerleRare;

public class SearchQuery
{
	public string Filter { get; set; }

	public string SqlFilter { get; set; }

	public string HtmlFilter { get; set; }

	public int Max { get; set; } = 15;
}
