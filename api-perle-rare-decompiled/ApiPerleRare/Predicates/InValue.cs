using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ApiPerleRare.Predicates;

public class InValue : IPredicateValue, IValue
{
	public List<IValue> Values { get; }

	public string FieldName { get; }

	public InValue(string fieldName, List<IValue> values)
	{
		for (int i = 0; i < values.Count; i++)
		{
			if (values[i] is PropertyValue pv)
			{
				values[i] = new StringValue(pv.Property);
			}
		}
		Values = values;
		FieldName = fieldName;
	}

	public bool Equals(IValue other)
	{
		if (!(other is InValue o))
		{
			return false;
		}
		if (FieldName != o.FieldName)
		{
			return false;
		}
		if (Values.Count != o.Values.Count)
		{
			return false;
		}
		for (int i = 0; i < Values.Count; i++)
		{
			if (!Values[i].Equals(o.Values[i]))
			{
				return false;
			}
		}
		return true;
	}

	public Expression Eval(ParameterExpression row)
	{
		MemberExpression field = Expression.Property(row, FieldName);
		Type itemType = ((PropertyInfo)field.Member).PropertyType;
		object array = null;
		if (Values.All((IValue v) => v is StringValue))
		{
			array = Values.Select((IValue v) => ((StringValue)v).Value).ToArray();
		}
		else
		{
			if (!Values.All((IValue v) => v is IntegerValue))
			{
				throw new Exception("Toutes les valeurs dans le 'In' doivent être soit des chaînes, soit des entiers");
			}
			array = Values.Select((IValue v) => ((IntegerValue)v).Value).ToArray();
		}
		MethodInfo containsMethod = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public).First((MethodInfo _) => _.Name == "Contains" && _.GetParameters().Count() == 2).MakeGenericMethod(itemType);
		return Expression.Call(containsMethod, Expression.Constant(array), field);
	}

	public string GetSQL(Func<string[], string> getSqlNameFromPropName)
	{
		return FieldName + " in (" + string.Join(", ", Values.Select((IValue v) => v.GetSQL(getSqlNameFromPropName))) + ")";
	}
}
