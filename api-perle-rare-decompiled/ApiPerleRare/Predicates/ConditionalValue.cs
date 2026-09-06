using System;
using System.Linq.Expressions;

namespace ApiPerleRare.Predicates;

public class ConditionalValue : IValue
{
	public IValue Condition { get; }

	public IValue Then { get; }

	public IValue Else { get; }

	public ConditionalValue(IValue condition, IValue then, IValue @else)
	{
		Condition = condition;
		Then = then;
		Else = @else;
	}

	public bool Equals(IValue other)
	{
		if (!(other is ConditionalValue cv))
		{
			return false;
		}
		return Condition.Equals(cv.Condition) && Then.Equals(cv.Then) && Else.Equals(cv.Else);
	}

	public Expression Eval(ParameterExpression row)
	{
		throw new NotImplementedException();
	}

	public string GetSQL(Func<string[], string> getSqlNameFromPropName)
	{
		throw new NotImplementedException();
	}
}
