using System;
using System.Linq.Expressions;

namespace ApiPerleRare.Predicates;

public class StringValue : IValue
{
	public string Value { get; }

	public StringValue(string value)
	{
		Value = value;
	}

	public bool Equals(IValue other)
	{
		if (!(other is StringValue sv))
		{
			return false;
		}
		return Value == sv.Value;
	}

	public Expression Eval(ParameterExpression row)
	{
		return Expression.Constant(Value);
	}

	public string GetSQL(Func<string[], string> getSqlNameFromPropName)
	{
		return "'" + Value.Replace("'", "''") + "'";
	}
}
