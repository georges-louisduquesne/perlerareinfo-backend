using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace ApiPerleRare.Helpers;

public static class CustomQuerySerializeHelper
{
	public static IEnumerable<Dictionary<string, object>> Serialize(SqlDataReader reader)
	{
		List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();
		List<string> cols = new List<string>();
		for (int i = 0; i < reader.FieldCount; i++)
		{
			cols.Add(reader.GetName(i));
		}
		while (reader.Read())
		{
			results.Add(SerializeRow(cols, reader));
		}
		return results;
	}

	private static Dictionary<string, object> SerializeRow(IEnumerable<string> cols, SqlDataReader reader)
	{
		Dictionary<string, object> result = new Dictionary<string, object>();
		foreach (string col in cols)
		{
			result.Add(col, reader[col]);
		}
		return result;
	}
}
