using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using ApiPerleRare.Orderbys;
using ApiPerleRare.Predicates;
using MySqlConnector;

namespace ApiPerleRare.Helpers;

public class DataReaderHelper<TModel> where TModel : class, new()
{
	private readonly IDataReader _dataReader;

	private Dictionary<int, PropertyInfo> _columns = new Dictionary<int, PropertyInfo>();

	public DataReaderHelper(IDataReader dataReader)
	{
		_dataReader = dataReader;
	}

	public TModel Read()
	{
		if (!_dataReader.Read())
		{
			return null;
		}
		if (_columns.Count == 0)
		{
			PropertyInfo[] props = typeof(TModel).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty);
			for (int i = 0; i < _dataReader.FieldCount; i++)
			{
				string name = _dataReader.GetName(i);
				name = name.Replace("_", "");
				PropertyInfo prop = props.SingleOrDefault((PropertyInfo p) => string.Compare(p.Name, name, ignoreCase: true) == 0);
				if (prop == null)
				{
					throw new Exception("Aucun colonne ne correspond à '" + name + "'");
				}
				_columns.Add(i, prop);
			}
		}
		TModel m = new TModel();
		foreach (KeyValuePair<int, PropertyInfo> c in _columns)
		{
			if (!_dataReader.IsDBNull(c.Key))
			{
				object val = _dataReader.GetValue(c.Key);
				if (val.GetType() != c.Value.PropertyType && val.GetType() != Nullable.GetUnderlyingType(c.Value.PropertyType))
				{
					val = Convert.ChangeType(val, c.Value.PropertyType);
				}
				c.Value.SetValue(m, val);
			}
		}
		return m;
	}

	public TModel[] ReadAll()
	{
		List<TModel> list = new List<TModel>();
		while (true)
		{
			TModel m = Read();
			if (m == null)
			{
				break;
			}
			list.Add(m);
		}
		return list.ToArray();
	}

	public static SelectResult<TModel> Select(MySqlConnection connection, string tableName, Func<string[], string> getSqlNameFromPropName, string where, string orderby = null, int take = 0, int skip = 0, string select = null)
	{
		string sql = "SELECT ";
		string sqlCount = "SELECT COUNT(*) ";
		List<string> fields = new List<string>();
		if (!string.IsNullOrWhiteSpace(select))
		{
			string[] array = select.Split(',');
			foreach (string field in array)
			{
				string f = getSqlNameFromPropName((from text in field.Split('.')
					select text.Trim()).ToArray());
				fields.Add(f);
			}
		}
		sql = ((fields.Count != 0) ? (sql + string.Join(", ", fields)) : (sql + "*"));
		sql = sql + " FROM " + tableName;
		sqlCount = sqlCount + " FROM " + tableName;
		if (!string.IsNullOrWhiteSpace(where))
		{
			IValue val = ApiPerleRare.Predicates.Parser.Parse(where);
			if (!(val is IPredicateValue))
			{
				throw new Exception("'" + where + "' doit être un filtre/prédicat");
			}
			sql = sql + " WHERE " + val.GetSQL(getSqlNameFromPropName);
			sqlCount = sqlCount + " WHERE " + val.GetSQL(getSqlNameFromPropName);
		}
		if (!string.IsNullOrWhiteSpace(orderby))
		{
			List<OrderByDef> orderbys = ApiPerleRare.Orderbys.Parser.Parse(orderby);
			List<string> os = new List<string>();
			foreach (OrderByDef ob in orderbys)
			{
				os.Add(getSqlNameFromPropName(ob.FieldNames) + " " + (ob.Asc ? "ASC" : "DESC"));
			}
			if (os.Count > 0)
			{
				sql = sql + " ORDER BY " + string.Join(", ", os);
			}
		}
		if (take > 0)
		{
			sql += $" LIMIT {take}";
		}
		if (skip > 0)
		{
			sql += $" OFFSET {skip}";
		}
		SelectResult<TModel> res = new SelectResult<TModel>();
		using (MySqlCommand cmd = connection.CreateCommand())
		{
			cmd.CommandText = sql;
			using MySqlDataReader reader = cmd.ExecuteReader();
			DataReaderHelper<TModel> drh = new DataReaderHelper<TModel>(reader);
			res.Items = drh.ReadAll();
		}
		if (take == 0 && skip == 0)
		{
			res.Total = res.Items.Length;
		}
		else
		{
			using MySqlCommand cmd2 = connection.CreateCommand();
			cmd2.CommandText = sqlCount;
			res.Total = Convert.ToInt32(cmd2.ExecuteScalar());
		}
		return res;
	}
}
