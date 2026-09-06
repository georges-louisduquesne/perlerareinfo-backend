using System;
using System.Globalization;
using System.Linq.Expressions;

namespace ApiPerleRare.Predicates;

public class DoubleValue : IValue
{
	public double Value { get; }

	public DoubleValue(double value)
	{
		Value = value;
	}

	public bool Equals(IValue other)
	{
		if (!(other is DoubleValue iv))
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
