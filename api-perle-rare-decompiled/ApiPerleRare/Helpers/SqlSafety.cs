using System;
using System.Text.RegularExpressions;

namespace ApiPerleRare.Helpers;

/// <summary>Escape / validate fragments used in leftover dynamic SQL. Does not change filter syntax the front sends.</summary>
internal static class SqlSafety
{
	private static readonly Regex Ident = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

	private static readonly Regex Number = new(@"^-?\d+(\.\d+)?$", RegexOptions.Compiled);

	public static string Quote(string value)
	{
		return "'" + (value ?? string.Empty).Replace("\\", "\\\\").Replace("'", "''") + "'";
	}

	public static string Identifier(string name)
	{
		if (string.IsNullOrEmpty(name) || !Ident.IsMatch(name))
		{
			throw new ArgumentException("Identifiant SQL interdit");
		}
		return name;
	}

	public static bool IsNumber(string value)
	{
		return !string.IsNullOrEmpty(value) && Number.IsMatch(value);
	}

	public static string NumberLiteral(string value)
	{
		if (!IsNumber(value))
		{
			throw new ArgumentException("Valeur numérique attendue");
		}
		return value;
	}
}
