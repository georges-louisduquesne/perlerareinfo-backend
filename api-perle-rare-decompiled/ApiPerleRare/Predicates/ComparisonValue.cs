using System;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Predicates;

public class ComparisonValue : IPredicateValue, IValue
{
	public IValue Left { get; }

	public IValue Right { get; }

	public ComparisonOperator Operator { get; }

	public ComparisonValue(IValue left, IValue right, ComparisonOperator op)
	{
		Left = left;
		Right = right;
		Operator = op;
	}

	public bool Equals(IValue other)
	{
		if (!(other is ComparisonValue bov))
		{
			return false;
		}
		return Left.Equals(bov.Left) && Right.Equals(bov.Right) && Operator == bov.Operator;
	}

	public static bool AreEqual(object lv, object rv)
	{
		if (lv is string || rv is string)
		{
			if (lv == null)
			{
				lv = "";
			}
			if (rv == null)
			{
				rv = "";
			}
		}
		if (lv == null && rv == null)
		{
			return true;
		}
		if (lv == null || rv == null)
		{
			return false;
		}
		return ((IComparable)lv).CompareTo(rv) == 0;
	}

	public Expression Eval(ParameterExpression row)
	{
		Expression le = Left.Eval(row);
		Expression re = Right.Eval(row);
		if (le.Type == typeof(string) && re.Type != typeof(string))
		{
			re = Expression.Call(null, typeof(Convert).GetMethod("ToString", new Type[1] { re.Type }), re);
		}
		if (le.Type == typeof(DateTime) && re is ConstantExpression ce)
		{
			if (ce.Value is string s)
			{
				re = Expression.Constant(DateTime.ParseExact(s, "dd/MM/yyyy", CultureInfo.InvariantCulture));
			}
			else if (!(ce.Value is DateTime))
			{
				throw new NotImplementedException("A convertir en datetime (" + ce.Type.FullName + ")");
			}
		}
		else if (le.Type != re.Type && re is ConstantExpression ce2)
		{
			Type ult = Nullable.GetUnderlyingType(le.Type);
			re = ((ult != null && ce2.Value.GetType() == ult) ? Expression.Constant(ce2.Value, le.Type) : ((ce2.Value is int i && le.Type == typeof(uint?)) ? Expression.Constant((uint)i, le.Type) : ((!(ce2.Value is int i2) || !(le.Type == typeof(sbyte?))) ? Expression.Constant(Convert.ChangeType(ce2.Value, le.Type)) : Expression.Constant((sbyte)i2, le.Type))));
		}
		return Operator switch
		{
			ComparisonOperator.Equal => Expression.Equal(le, re), 
			ComparisonOperator.NotEqual => Expression.NotEqual(le, re), 
			ComparisonOperator.Greater => Expression.GreaterThan(le, re), 
			ComparisonOperator.GreaterOrEqual => Expression.GreaterThanOrEqual(le, re), 
			ComparisonOperator.Less => Expression.LessThan(le, re), 
			ComparisonOperator.LessOrEqual => Expression.LessThanOrEqual(le, re), 
			ComparisonOperator.Like => Expression.Call(typeof(DbFunctionsExtensions), "Like", Type.EmptyTypes, Expression.Constant(EF.Functions), le, re), 
			_ => throw new NotImplementedException(), 
		};
	}

	public string GetSQL(Func<string[], string> getSqlNameFromPropName)
	{
		string le = Left.GetSQL(getSqlNameFromPropName);
		string re = Right.GetSQL(getSqlNameFromPropName);
		return Operator switch
		{
			ComparisonOperator.Equal => le + " = " + re, 
			ComparisonOperator.NotEqual => le + " <> " + re, 
			ComparisonOperator.Greater => le + " > " + re, 
			ComparisonOperator.GreaterOrEqual => le + " >= " + re, 
			ComparisonOperator.Less => le + " < " + re, 
			ComparisonOperator.LessOrEqual => le + " <= " + re, 
			ComparisonOperator.Like => le + " like " + re, 
			_ => throw new NotImplementedException(), 
		};
	}
}
