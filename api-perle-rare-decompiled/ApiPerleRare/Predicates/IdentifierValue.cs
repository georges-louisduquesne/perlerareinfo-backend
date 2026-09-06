using System;
using System.Linq;
using System.Linq.Expressions;

namespace ApiPerleRare.Predicates;

public class IdentifierValue : IValue
{
	public string Identifier { get; }

	public IdentifierValue(string identifier)
	{
		Identifier = identifier;
	}

	public bool Equals(IValue other)
	{
		if (!(other is IdentifierValue iv))
		{
			return false;
		}
		return Identifier == iv.Identifier;
	}

	public Expression Eval(ParameterExpression row)
	{
		throw new NotImplementedException();
	}

	public string GetSQL(Func<string[], string> getSqlNameFromPropName)
	{
		return getSqlNameFromPropName((from p in Identifier.Split('.')
			select p.Trim()).ToArray());
	}
}
