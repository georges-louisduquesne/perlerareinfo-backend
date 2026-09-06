using System;
using System.Linq.Expressions;

namespace ApiPerleRare.Predicates;

public class BinaryOperationValue : IValue
{
	public IValue Left { get; }

	public IValue Right { get; }

	public BinaryOperator Operator { get; }

	public BinaryOperationValue(IValue left, IValue right, BinaryOperator op)
	{
		Left = left;
		Right = right;
		Operator = op;
	}

	public bool Equals(IValue other)
	{
		if (!(other is BinaryOperationValue bov))
		{
			return false;
		}
		return Left.Equals(bov.Left) && Right.Equals(bov.Right) && Operator == bov.Operator;
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
