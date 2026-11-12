using System.Globalization;
using System.Text.RegularExpressions;

namespace ApiPerleRare.Helpers;

/// <summary>
/// Caps the MySqlConnector pool. Each API process otherwise keeps up to 100
/// server connections. The three live processes must stay under max_connections.
/// </summary>
internal static class DbConnectionPool
{
	internal static string Apply(string connectionString, string maxPool, string idleSeconds)
	{
		if (string.IsNullOrEmpty(connectionString))
		{
			return connectionString;
		}
		connectionString = Set(connectionString, "Maximum Pool Size", maxPool);
		return Set(connectionString, "Connection Idle Timeout", idleSeconds);
	}

	private static string Set(string connectionString, string name, string raw)
	{
		if (string.IsNullOrWhiteSpace(raw) || !int.TryParse(raw.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int value) || value <= 0)
		{
			return connectionString;
		}
		Regex pattern = new Regex("(^|;)\\s*" + Regex.Escape(name) + "\\s*=[^;]*", RegexOptions.IgnoreCase);
		string written = value.ToString(CultureInfo.InvariantCulture);
		if (pattern.IsMatch(connectionString))
		{
			return pattern.Replace(connectionString, "${1}" + name + "=" + written);
		}
		if (!connectionString.EndsWith(";", System.StringComparison.Ordinal))
		{
			connectionString += ";";
		}
		return connectionString + name + "=" + written;
	}
}
