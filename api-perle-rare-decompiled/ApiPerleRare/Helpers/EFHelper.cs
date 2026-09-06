using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using ApiPerleRare.Orderbys;
using ApiPerleRare.Predicates;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Helpers;

public class EFHelper<TEFModel> where TEFModel : class
{
	private static readonly PropertyInfo[] _props;

	private static PropertyInfo[] _phpSerializedProps;

	static EFHelper()
	{
		_props = typeof(TEFModel).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty);
	}

	public static void SetPhpSerialized(params Expression<Func<TEFModel, string>>[] phpSerialized)
	{
		_phpSerializedProps = phpSerialized.Select((Expression<Func<TEFModel, string>> p) => GetPropertyInfo(p)).ToArray();
	}

	private static PropertyInfo GetPropertyInfo(Expression<Func<TEFModel, string>> f)
	{
		MemberExpression pe = (MemberExpression)f.Body;
		return (PropertyInfo)pe.Member;
	}

	public static IQueryable<TEFModel> Apply(IQueryable<TEFModel> query, string where, string orderby = null, int take = 0, int skip = 0, string select = null, Func<IQueryable<TEFModel>, IQueryable<TEFModel>> finalize = null)
	{
		if (!string.IsNullOrWhiteSpace(where))
		{
			IValue val = ApiPerleRare.Predicates.Parser.Parse(where);
			if (!(val is IPredicateValue))
			{
				throw new Exception("'" + where + "' doit être un filtre/prédicat");
			}
			ParameterExpression param = Expression.Parameter(typeof(TEFModel));
			Expression exp = val.Eval(param);
			query = query.Where(Expression.Lambda<Func<TEFModel, bool>>(exp, new ParameterExpression[1] { param }));
		}
		if (!string.IsNullOrWhiteSpace(select))
		{
			NewExpression ne = Expression.New(typeof(TEFModel));
			ParameterExpression param2 = Expression.Parameter(typeof(TEFModel));
			List<MemberBinding> mbs = new List<MemberBinding>();
			string[] array = select.Split(',');
			foreach (string field in array)
			{
				string f = field.Trim();
				PropertyInfo mi = _props.Single((PropertyInfo p) => string.Compare(p.Name, field, ignoreCase: true) == 0);
				mbs.Add(Expression.Bind(mi, Expression.Property(param2, mi.Name)));
			}
			MemberInitExpression exp2 = Expression.MemberInit(ne, mbs.ToArray());
			query = query.Select(Expression.Lambda<Func<TEFModel, TEFModel>>(exp2, new ParameterExpression[1] { param2 }));
			query = query.Distinct();
		}
		if (!string.IsNullOrWhiteSpace(orderby))
		{
			List<OrderByDef> orderbys = ApiPerleRare.Orderbys.Parser.Parse(orderby);
			IOrderedQueryable<TEFModel> oq = null;
			foreach (OrderByDef ob in orderbys)
			{
				ParameterExpression param3 = Expression.Parameter(typeof(TEFModel));
				MemberExpression exp3 = null;
				string[] fieldNames = ob.FieldNames;
				foreach (string fn in fieldNames)
				{
					exp3 = ((exp3 != null) ? Expression.Property(exp3, fn) : Expression.Property(param3, fn));
				}
				if (exp3.Type == typeof(string))
				{
					oq = CompleteOrderBy<string>(query, oq, ob, param3, exp3);
					continue;
				}
				if (exp3.Type == typeof(DateTime))
				{
					oq = CompleteOrderBy<DateTime>(query, oq, ob, param3, exp3);
					continue;
				}
				if (exp3.Type == typeof(DateTime?))
				{
					oq = CompleteOrderBy<DateTime?>(query, oq, ob, param3, exp3);
					continue;
				}
				if (exp3.Type == typeof(DateOnly))
				{
					oq = CompleteOrderBy<DateOnly>(query, oq, ob, param3, exp3);
					continue;
				}
				if (exp3.Type == typeof(DateOnly?))
				{
					oq = CompleteOrderBy<DateOnly?>(query, oq, ob, param3, exp3);
					continue;
				}
				if (exp3.Type == typeof(int?))
				{
					oq = CompleteOrderBy<int?>(query, oq, ob, param3, exp3);
					continue;
				}
				if (exp3.Type == typeof(uint))
				{
					oq = CompleteOrderBy<uint>(query, oq, ob, param3, exp3);
					continue;
				}
				throw new NotImplementedException("Le tri sur les champs de type '" + exp3.Type.FullName + "' non implémenté");
			}
			query = oq;
		}
		if (finalize != null)
		{
			query = finalize(query);
		}
		if (skip > 0)
		{
			query = query.Skip(skip);
		}
		if (take > 0)
		{
			query = query.Take(take);
		}
		return query;
	}

	private static IOrderedQueryable<TEFModel> CompleteOrderBy<T>(IQueryable<TEFModel> query, IOrderedQueryable<TEFModel> oq, OrderByDef ob, ParameterExpression param, MemberExpression exp)
	{
		Expression<Func<TEFModel, T>> lambda = Expression.Lambda<Func<TEFModel, T>>(exp, new ParameterExpression[1] { param });
		oq = ((oq == null) ? ((!ob.Asc) ? query.OrderByDescending(lambda) : query.OrderBy(lambda)) : ((!ob.Asc) ? oq.ThenByDescending(lambda) : oq.ThenBy(lambda)));
		return oq;
	}

	public static Task<SelectResult<TEFModel>> Select(DbSet<TEFModel> query, string where, string orderby = null, int take = 0, int skip = 0, string select = null, Func<IQueryable<TEFModel>, IQueryable<TEFModel>> finalize = null)
	{
		return Select(query.AsNoTracking(), where, orderby, take, skip, select, finalize);
	}

	public static async Task<SelectResult<TEFModel>> Select(IQueryable<TEFModel> query, string where, string orderby = null, int take = 0, int skip = 0, string select = null, Func<IQueryable<TEFModel>, IQueryable<TEFModel>> finalize = null)
	{
		SelectResult<TEFModel> selectResult = new SelectResult<TEFModel>();
		SelectResult<TEFModel> selectResult2 = selectResult;
		selectResult2.Items = await Apply(query, where, orderby, take, skip, select, finalize).ToArrayAsync();
		SelectResult<TEFModel> res = selectResult;
		res.Items.FixEncoding();
		if (take <= 0 || (res.Items.Length < take && skip <= 0))
		{
			res.Total = res.Items.Length;
		}
		else
		{
			SelectResult<TEFModel> selectResult3 = res;
			selectResult3.Total = await Apply(query, where).CountAsync();
		}
		ConvertPhpSerializedToJson(res.Items);
		return res;
	}

	public static void Copy<TEFModelTarget>(TEFModel source, TEFModelTarget target) where TEFModelTarget : TEFModel
	{
		PropertyInfo[] props = _props;
		foreach (PropertyInfo prop in props)
		{
			prop.SetValue(target, prop.GetValue(source));
		}
	}

	public static TEFModelTarget Copy<TEFModelTarget>(TEFModel source) where TEFModelTarget : TEFModel, new()
	{
		TEFModelTarget target = new TEFModelTarget();
		PropertyInfo[] props = _props;
		foreach (PropertyInfo prop in props)
		{
			prop.SetValue(target, prop.GetValue(source));
		}
		return target;
	}

	public static void ConvertPhpSerializedToJson(IEnumerable<TEFModel> res)
	{
		if (_phpSerializedProps == null)
		{
			return;
		}
		foreach (TEFModel r in res)
		{
			ConvertPhpSerializedToJson(r);
		}
	}

	public static void ConvertPhpSerializedToJson(TEFModel m)
	{
		if (_phpSerializedProps == null)
		{
			return;
		}
		PropertyInfo[] phpSerializedProps = _phpSerializedProps;
		foreach (PropertyInfo p in phpSerializedProps)
		{
			string val = p.GetValue(m) as string;
			if (!string.IsNullOrEmpty(val))
			{
				try
				{
					val = PhpSerializer.ToJson(val);
					p.SetValue(m, val);
				}
				catch (Exception value)
				{
					p.SetValue(m, $"Echec conversion '{val}' : {value}");
				}
			}
		}
	}

	internal static void ConvertPhpSerializedToPhp(TEFModel m)
	{
		if (_phpSerializedProps == null)
		{
			return;
		}
		PropertyInfo[] phpSerializedProps = _phpSerializedProps;
		foreach (PropertyInfo p in phpSerializedProps)
		{
			string val = p.GetValue(m) as string;
			if (!string.IsNullOrEmpty(val))
			{
				try
				{
					val = PhpSerializer.FromJson(val);
					p.SetValue(m, val);
				}
				catch (Exception value)
				{
					p.SetValue(m, $"Echec conversion '{val}' : {value}");
				}
			}
		}
	}
}
