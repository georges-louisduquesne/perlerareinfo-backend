using System;
using System.Globalization;
using System.Linq.Expressions;

namespace ApiPerleRare.Predicates;

public class DateTimeValue : IValue
{
	private readonly string _value;

	public DateTimeValue(string value)
	{
		_value = value;
	}

	public bool Equals(IValue other)
	{
		return other is DateTimeValue dv && dv._value == _value;
	}

	public Expression Eval(ParameterExpression row)
	{
		return Expression.Constant(DateTime.Parse(_value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
	}

	public string GetSQL(Func<string[], string> getSqlNameFromPropName)
	{
		if (_value == "0001-01-01T00:00:00")
		{
			return "0000-00-00";
		}
		return "'" + _value + "'";
	}
}
