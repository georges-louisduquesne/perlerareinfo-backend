using System;
using System.Globalization;
using System.Linq.Expressions;

namespace ApiPerleRare.Predicates;

public class IntegerValue : IValue
{
	public int Value { get; }

	public IntegerValue(int value)
	{
		Value = value;
	}

	public bool Equals(IValue other)
	{
		if (!(other is IntegerValue iv))
		{
			return false;
		}
		return Value == iv.Value;
	}

	public Expression Eval(ParameterExpression row)
	{
		return Expression.Constant(Value);
	}

	public string GetSQL(Func<string[], string> getSqlNameFromPropName)
	{
		return Value.ToString(CultureInfo.InvariantCulture);
	}
}
