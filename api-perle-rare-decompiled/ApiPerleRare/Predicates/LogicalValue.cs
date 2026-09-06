using System;
using System.Linq.Expressions;

namespace ApiPerleRare.Predicates;

public class LogicalValue : IPredicateValue, IValue
{
	public LogicalOperator Operator { get; }

	public IValue Left { get; }

	public IValue Right { get; }

	public LogicalValue(LogicalOperator op, IValue left, IValue right)
	{
		Operator = op;
		Left = left;
		Right = right;
	}

	public bool Equals(IValue other)
	{
		if (!(other is LogicalValue lv))
		{
			return false;
		}
		return Left.Equals(lv.Left) && Right.Equals(lv.Right);
	}

	public static IPredicateValue Apply(LogicalOperator op, IValue x, IValue y)
	{
		return new LogicalValue(op, x, y);
	}

	public Expression Eval(ParameterExpression row)
	{
		Expression le = Left.Eval(row);
		Expression re = Right.Eval(row);
		return Operator switch
		{
			LogicalOperator.And => Expression.AndAlso(le, re), 
			LogicalOperator.Or => Expression.OrElse(le, re), 
			_ => throw new NotImplementedException(), 
		};
	}

	public string GetSQL(Func<string[], string> getSqlNameFromPropName)
	{
		string le = Left.GetSQL(getSqlNameFromPropName);
		string re = Right.GetSQL(getSqlNameFromPropName);
		return Operator switch
		{
			LogicalOperator.And => $"({le}) AND ({re})", 
			LogicalOperator.Or => $"({le}) OR ({re})", 
			_ => throw new NotImplementedException(), 
		};
	}
}
