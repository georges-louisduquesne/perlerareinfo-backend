using System;
using System.Data;
using MySqlConnector;

namespace ApiPerleRare.Application.Monitoring;

/// <summary>
/// Biens encore ouverts, rangés par âge de <c>P_DateLastUpdate</c>.
/// </summary>
public static class RecrawlStockQuery
{
	public const string ActiveWhere = "P_State = 1 AND (P_DateFin IS NULL OR P_DateFin < '1000-01-01')";

	public const string Sql =
		"SELECT "
		+ "IFNULL(SUM(P_DateLastUpdate < DATE_SUB(NOW(), INTERVAL 90 DAY)), 0), "
		+ "IFNULL(SUM(P_DateLastUpdate >= DATE_SUB(NOW(), INTERVAL 90 DAY) AND P_DateLastUpdate < DATE_SUB(NOW(), INTERVAL 30 DAY)), 0) "
		+ "FROM property WHERE " + ActiveWhere + " AND IFNULL(P_EstRattrapage, 0) <> 4";

	public readonly struct Counts
	{
		public Counts(int over90, int between30And90)
		{
			Over90 = over90;
			Between30And90 = between30And90;
		}

		public int Over90 { get; }

		public int Between30And90 { get; }
	}

	public static Counts Read(MySqlConnection connection)
	{
		if (connection == null)
		{
			throw new ArgumentNullException(nameof(connection));
		}
		if (connection.State != ConnectionState.Open)
		{
			connection.Open();
		}
		using MySqlCommand command = connection.CreateCommand();
		command.CommandText = Sql;
		using MySqlDataReader reader = command.ExecuteReader();
		if (!reader.Read())
		{
			return new Counts(0, 0);
		}
		return new Counts(ToInt(reader.GetValue(0)), ToInt(reader.GetValue(1)));
	}

	private static int ToInt(object value)
	{
		if (value == null || value is DBNull)
		{
			return 0;
		}
		return Convert.ToInt32(value);
	}
}
