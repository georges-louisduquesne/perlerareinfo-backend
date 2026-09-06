using System;
using System.Linq.Expressions;

namespace ApiPerleRare.Predicates;

public class BooleanValue : IValue
{
	public bool Value { get; }

	public BooleanValue(bool value)
	{
		Value = value;
	}

	public bool Equals(IValue other)
	{
		if (!(other is BooleanValue bv))
		{
			return false;
		}
		return Value == bv.Value;
	}

	public Expression Eval(ParameterExpression row)
	{
		return Expression.Constant(Value);
	}

	public string GetSQL(Func<string[], string> getSqlNameFromPropName)
	{
		return "1";
	}
}
