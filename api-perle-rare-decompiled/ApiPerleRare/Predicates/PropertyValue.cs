using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ApiPerleRare.Predicates;

public class PropertyValue : IValue
{
	public string Property { get; }

	public PropertyValue(string property)
	{
		Property = property;
	}

	public bool Equals(IValue other)
	{
		if (!(other is PropertyValue pv))
		{
			return false;
		}
		return Property == pv.Property;
	}

	public static PropertyInfo Resolve(Type type, string name)
	{
		if (type == null || string.IsNullOrEmpty(name))
		{
			return null;
		}
		const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase;
		PropertyInfo exact = type.GetProperty(name, flags);
		if (exact != null)
		{
			return exact;
		}
		string compact = name.Replace("_", "", StringComparison.Ordinal);
		foreach (PropertyInfo p in type.GetProperties(flags))
		{
			if (string.Equals(p.Name.Replace("_", "", StringComparison.Ordinal), compact, StringComparison.OrdinalIgnoreCase))
			{
				return p;
			}
		}
		return null;
	}

	public Expression Eval(ParameterExpression row)
	{
		PropertyInfo property = Resolve(row.Type, Property)
			?? throw new ArgumentException("'" + Property + "' is not a member of type '" + row.Type + "'");
		return Expression.Property(row, property);
	}

	public string GetSQL(Func<string[], string> getSqlNameFromPropName)
	{
		return getSqlNameFromPropName((from p in Property.Split('.')
			select p.Trim()).ToArray());
	}
}
