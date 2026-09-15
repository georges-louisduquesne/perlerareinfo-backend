namespace ApiPerleRare.Helpers;

/// <summary>
/// Shared skip/take normalization. <c>take = 0</c> still means "no LIMIT" (home Accueil).
/// </summary>
public static class QueryPaging
{
	public static void Normalize(ref int skip, ref int take)
	{
		if (skip < 0)
		{
			skip = 0;
		}
		if (take < 0)
		{
			take = 0;
		}
	}

	/// <summary>
	/// OFFSET without ORDER BY is unstable on MariaDB. Keep the existing orderby when present.
	/// </summary>
	public static string EnsureOrderBy(string orderby, string fallbackField)
	{
		if (!string.IsNullOrWhiteSpace(orderby))
		{
			return orderby;
		}
		return string.IsNullOrWhiteSpace(fallbackField) ? orderby : fallbackField;
	}
}
