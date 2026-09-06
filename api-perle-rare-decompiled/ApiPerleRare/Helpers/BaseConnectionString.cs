using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ApiPerleRare.Helpers;

public abstract class BaseConnectionString
{
	private static Regex rgx_connectionString = new Regex("^(?<key>[\\w\\s\\d]+)=(?<value>[^;\"']+|\"[^\"]+\"|'[^']+')(?:;(?<key>[\\w\\s\\d]+)=(?<value>[^;\"']+|\"[^\"]+\"|'[^']+'))*[\\s;]*$");

	public BaseConnectionString(string connectionString)
	{
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			return;
		}
		PropertyInfo[] props = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty);
		Match m = rgx_connectionString.Match(connectionString);
		if (!m.Success)
		{
			throw new Exception("Chaîne de connexion '" + connectionString + "' non valide");
		}
		int nb = m.Groups["key"].Captures.Count;
		for (int i = 0; i < nb; i++)
		{
			string key = m.Groups["key"].Captures[i].Value.Trim();
			string value = m.Groups["value"].Captures[i].Value.Trim();
			if (value.StartsWith("\"") || value.StartsWith("'"))
			{
				value = value.Substring(1, value.Length - 2);
			}
			PropertyInfo prop = props.FirstOrDefault((PropertyInfo p) => string.Compare(p.Name, key, ignoreCase: true) == 0 || p.GetCustomAttributes<AliasAttribute>().Any((AliasAttribute aa) => string.Compare(aa.Alias, key, ignoreCase: true) == 0));
			if (prop == null)
			{
				throw new Exception("Aucun propriété '" + key + "' dans '" + GetType().FullName + "'");
			}
			prop.SetValue(this, Convert.ChangeType(value, prop.PropertyType, CultureInfo.InvariantCulture));
		}
	}

	public override string ToString()
	{
		PropertyInfo[] props = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty);
		return string.Join("; ", from p in props
			select GetParam(p, this) into p
			where p != null
			select p);
	}

	private string GetParam(PropertyInfo p, BaseConnectionString connectionString)
	{
		if (!p.CanWrite)
		{
			return null;
		}
		object val = p.GetValue(connectionString);
		if (p.PropertyType == typeof(string) && string.IsNullOrWhiteSpace((string)val))
		{
			return null;
		}
		if (p.PropertyType == typeof(int) && (int)val == 0)
		{
			return null;
		}
		DefaultValueAttribute dv = p.GetCustomAttribute<DefaultValueAttribute>();
		if (dv != null && dv.Value is IComparable && ((IComparable)dv.Value).Equals(val))
		{
			return null;
		}
		return p.Name + "=" + val;
	}
}
