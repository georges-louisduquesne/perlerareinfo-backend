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
		re = CoerceRight(le, re);
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

	private static Expression CoerceRight(Expression le, Expression re)
	{
		if (le.Type == re.Type || re is not ConstantExpression ce)
		{
			return re;
		}
		if (ce.Value == null)
		{
			return Expression.Constant(null, le.Type);
		}
		Type target = Nullable.GetUnderlyingType(le.Type) ?? le.Type;
		object value = ce.Value;
		if (target == typeof(DateTime))
		{
			if (value is DateTime dt)
			{
				return Expression.Constant(dt, le.Type);
			}
			if (value is string s && TryParseDateTime(s, out DateTime parsed))
			{
				return Expression.Constant(parsed, le.Type);
			}
			throw new NotImplementedException("A convertir en datetime (" + ce.Type.FullName + ")");
		}
		try
		{
			object converted = Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
			return Expression.Constant(converted, le.Type);
		}
		catch (Exception ex)
		{
			throw new InvalidCastException("Impossible de convertir " + value.GetType().FullName + " vers " + le.Type.FullName, ex);
		}
	}

	private static bool TryParseDateTime(string s, out DateTime parsed)
	{
		if (DateTime.TryParseExact(s, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
		{
			return true;
		}
		return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed);
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
