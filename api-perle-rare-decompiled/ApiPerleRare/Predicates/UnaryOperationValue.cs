using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ApiPerleRare.Predicates;

public class UnaryOperationValue : IPredicateValue, IValue
{
	public IValue Value { get; }

	public UnaryOperator Operator { get; }

	public UnaryOperationValue(IValue value, UnaryOperator op)
	{
		Value = value;
		Operator = op;
	}

	public bool Equals(IValue other)
	{
		if (!(other is UnaryOperationValue uov))
		{
			return false;
		}
		return Operator == uov.Operator && Value.Equals(uov.Value);
	}

	public Expression Eval(ParameterExpression row)
	{
		string fieldName = ((IdentifierValue)Value).Identifier;
		MemberExpression param = null;
		string[] array = fieldName.Split('.');
		foreach (string p in array)
		{
			param = ((param != null) ? Expression.Property(param, p) : Expression.Property(row, p));
		}
		if (Attribute.IsDefined(param.Member, typeof(ConsiderDefautValueAsNullAttribute)))
		{
			Type propertyType = ((PropertyInfo)param.Member).PropertyType;
			if (propertyType == typeof(int?))
			{
				object defaultValue = 0;
				return Operator switch
				{
					UnaryOperator.Null => Expression.OrElse(Expression.Equal(param, Expression.Constant(null)), Expression.Equal(param, Expression.Constant(defaultValue, propertyType))), 
					UnaryOperator.NotNull => Expression.AndAlso(Expression.NotEqual(param, Expression.Constant(null)), Expression.NotEqual(param, Expression.Constant(defaultValue, propertyType))), 
					_ => throw new NotImplementedException($"Opérateur '{Operator}' non géré"), 
				};
			}
			throw new NotImplementedException("Valeur par défaut de '" + propertyType.FullName + "' à définir");
		}
		return Operator switch
		{
			UnaryOperator.Null => Expression.Equal(param, Expression.Constant(null)), 
			UnaryOperator.NotNull => Expression.NotEqual(param, Expression.Constant(null)), 
			_ => throw new NotImplementedException($"Opérateur '{Operator}' non géré"), 
		};
	}

	public string GetSQL(Func<string[], string> getSqlNameFromPropName)
	{
		string v = Value.GetSQL(getSqlNameFromPropName);
		return Operator switch
		{
			UnaryOperator.Null => v + " IS NULL", 
			UnaryOperator.NotNull => v + " IS NOT NULL", 
			_ => throw new NotImplementedException($"Opérateur '{Operator}' non géré"), 
		};
	}
}
