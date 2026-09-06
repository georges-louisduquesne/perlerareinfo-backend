using System;
using System.Linq;
using System.Linq.Expressions;

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

	public Expression Eval(ParameterExpression row)
	{
		return Expression.Property(row, Property);
	}

	public string GetSQL(Func<string[], string> getSqlNameFromPropName)
	{
		return getSqlNameFromPropName((from p in Property.Split('.')
			select p.Trim()).ToArray());
	}
}
