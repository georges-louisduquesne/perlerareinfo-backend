using System;
using System.Linq.Expressions;

namespace ApiPerleRare.Predicates;

public interface IValue
{
	bool Equals(IValue other);

	Expression Eval(ParameterExpression row);

	string GetSQL(Func<string[], string> getSqlNameFromPropName);
}
