using System;
using System.Globalization;
using System.Threading.Tasks;
using MySqlConnector;

namespace ApiPerleRare.YanportModels;

/// <summary>
/// L'import Yanport rattache les biens via intermediaires_directs_yanport,
/// pas via intermediaires_directs.I_IdYanport. À la saisie ou au changement
/// de cet id, on recopie la ligne (source « saisi ») et on rattache les biens
/// déjà publiés dont le dealer porte cet id.
/// </summary>
public static class AgencyYanportLink
{
	/// <summary>intermediaires_directs_yanport.I_Source est varchar(5).</summary>
	public const string SaisieSource = "saisi";

	public const string DeleteFieldRowSql =
		"DELETE FROM intermediaires_directs_yanport WHERE I_RefIntermediaire=@agency AND I_YanportId=@yanport AND I_Source IN ('saisi','init')";

	public const string InsertFieldRowSql =
		"INSERT IGNORE INTO intermediaires_directs_yanport (I_RefIntermediaire, I_YanportId, I_Source) VALUES (@agency, @yanport, 'saisi')";

	public const string InsertPublishedLinksSql =
		"INSERT INTO rel_property_inter_direct (P_PropertyId, I_RefIntermediaire) "
		+ "SELECT p.P_PropertyId, @agency FROM property p "
		+ "WHERE p.P_State=1 AND (p.P_Annonceurs LIKE @idComma OR p.P_Annonceurs LIKE @idBrace OR p.P_Annonceurs LIKE @idSpComma OR p.P_Annonceurs LIKE @idSpBrace) "
		+ "AND NOT EXISTS (SELECT 1 FROM rel_property_inter_direct r WHERE r.P_PropertyId=p.P_PropertyId AND r.I_RefIntermediaire=@agency)";

	public const string DeletePublishedLinksSql =
		"DELETE r FROM rel_property_inter_direct r "
		+ "INNER JOIN property p ON p.P_PropertyId=r.P_PropertyId "
		+ "WHERE r.I_RefIntermediaire=@agency AND p.P_State=1 "
		+ "AND (p.P_Annonceurs LIKE @idComma OR p.P_Annonceurs LIKE @idBrace OR p.P_Annonceurs LIKE @idSpComma OR p.P_Annonceurs LIKE @idSpBrace)";

	public static bool ShouldRun(long previousYanportId, long nextYanportId)
	{
		return previousYanportId != nextYanportId && (previousYanportId > 0 || nextYanportId > 0);
	}

	public static bool AnnouncersContainDealerId(string annonceurs, long yanportId)
	{
		if (string.IsNullOrEmpty(annonceurs) || yanportId <= 0)
		{
			return false;
		}
		string id = yanportId.ToString(CultureInfo.InvariantCulture);
		return ContainsBounded(annonceurs, "\"Id\":" + id) || ContainsBounded(annonceurs, "\"Id\": " + id);
	}

	public static async Task<AgencyYanportLinkResult> ExecuteAsync(MySqlConnection connection, uint agencyId, long previousYanportId, long nextYanportId)
	{
		AgencyYanportLinkResult result = new AgencyYanportLinkResult();
		if (connection == null || agencyId == 0 || !ShouldRun(previousYanportId, nextYanportId))
		{
			return result;
		}
		if (previousYanportId > 0 && previousYanportId != nextYanportId)
		{
			result.FieldRowRemoved = await ExecuteAsync(connection, DeleteFieldRowSql, agencyId, previousYanportId);
			if (result.FieldRowRemoved > 0)
			{
				result.PropertiesUnlinked = await ExecuteAsync(connection, DeletePublishedLinksSql, agencyId, previousYanportId);
			}
		}
		if (nextYanportId > 0)
		{
			result.FieldRowEnsured = await ExecuteAsync(connection, InsertFieldRowSql, agencyId, nextYanportId);
			result.PropertiesLinked = await ExecuteAsync(connection, InsertPublishedLinksSql, agencyId, nextYanportId);
		}
		return result;
	}

	private static bool ContainsBounded(string text, string token)
	{
		int start = 0;
		while (start < text.Length)
		{
			int found = text.IndexOf(token, start, StringComparison.Ordinal);
			if (found < 0)
			{
				return false;
			}
			int after = found + token.Length;
			if (after >= text.Length || text[after] == ',' || text[after] == '}')
			{
				return true;
			}
			start = after;
		}
		return false;
	}

	private static async Task<int> ExecuteAsync(MySqlConnection connection, string sql, uint agencyId, long yanportId)
	{
		using MySqlCommand cmd = connection.CreateCommand();
		cmd.CommandText = sql;
		cmd.Parameters.Add("@agency", MySqlDbType.UInt32).Value = agencyId;
		cmd.Parameters.Add("@yanport", MySqlDbType.UInt64).Value = (ulong)yanportId;
		AddDealerLikes(cmd, yanportId);
		return await cmd.ExecuteNonQueryAsync();
	}

	private static void AddDealerLikes(MySqlCommand cmd, long yanportId)
	{
		string id = yanportId.ToString(CultureInfo.InvariantCulture);
		cmd.Parameters.Add("@idComma", MySqlDbType.VarChar).Value = "%\"Id\":" + id + ",%";
		cmd.Parameters.Add("@idBrace", MySqlDbType.VarChar).Value = "%\"Id\":" + id + "}%";
		cmd.Parameters.Add("@idSpComma", MySqlDbType.VarChar).Value = "%\"Id\": " + id + ",%";
		cmd.Parameters.Add("@idSpBrace", MySqlDbType.VarChar).Value = "%\"Id\": " + id + "}%";
	}
}

public sealed class AgencyYanportLinkResult
{
	public int FieldRowRemoved { get; set; }

	public int FieldRowEnsured { get; set; }

	public int PropertiesLinked { get; set; }

	public int PropertiesUnlinked { get; set; }
}
