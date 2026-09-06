using System;
using System.Linq.Expressions;

namespace ApiPerleRare.Predicates;

public class MethodValue : IValue
{
	public string MethodName { get; }

	public IValue[] Parameters { get; }

	public MethodValue(string methodName, params IValue[] parameters)
	{
		MethodName = methodName;
		Parameters = parameters;
	}

	public bool Equals(IValue other)
	{
		if (!(other is MethodValue mv))
		{
			return false;
		}
		if (MethodName != mv.MethodName || Parameters.Length != mv.Parameters.Length)
		{
			return false;
		}
		for (int i = 0; i < Parameters.Length; i++)
		{
			if (!Parameters[i].Equals(mv.Parameters[i]))
			{
				return false;
			}
		}
		return true;
	}

	public Expression Eval(ParameterExpression row)
	{
		if (string.Compare(MethodName, "now", ignoreCase: true) == 0)
		{
			return Expression.Constant(DateTime.Now);
		}
		throw new NotImplementedException();
	}

	public string GetSQL(Func<string[], string> getSqlNameFromPropName)
	{
		if (string.Compare(MethodName, "now", ignoreCase: true) == 0)
		{
			return "NOW()";
		}
		throw new NotImplementedException();
	}
}
