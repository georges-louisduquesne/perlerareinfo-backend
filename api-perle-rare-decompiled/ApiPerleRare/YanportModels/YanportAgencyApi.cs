namespace ApiPerleRare.YanportModels;

public static class YanportAgencyApi
{
	public static string AgencyUrl(long yanportId) => "https://api.yanport.com/agencies/" + yanportId;

	public static bool TryParseId(string raw, out long id)
	{
		id = 0;
		if (string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}
		return long.TryParse(raw.Trim(), out id) && id > 0;
	}
}
