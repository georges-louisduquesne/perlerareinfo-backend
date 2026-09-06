using System;
using System.Text.Json;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;

namespace ApiPerleRare.RecupInfos.PropertyFilters;

public class GenericRangePropertyFilter : AbstractPropertyFilter
{
	public enum Field
	{
		Surface,
		Budget,
		BudgetC
	}

	private readonly Field _field;

	private readonly int _from;

	private readonly int _to;

	private readonly bool? _nonconnu;

	public GenericRangePropertyFilter(Field field, string value)
	{
		_field = field;
		if (string.IsNullOrEmpty(value))
		{
			_from = int.MinValue;
			return;
		}
		string json = PhpSerializer.ToJson(value);
		string[] vals = JsonSerializer.Deserialize<string[]>(json);
		if (vals.Length != 3)
		{
			throw new Exception("3 valeurs attendues");
		}
		_from = int.Parse(vals[0]);
		_to = int.Parse(vals[1]);
		switch (vals[2])
		{
		case "2":
			_nonconnu = null;
			return;
		case "1":
			_nonconnu = true;
			return;
		case "0":
		case "3":
			_nonconnu = false;
			return;
		}
		throw new NotImplementedException($"{field} : valeur '{vals[2]}' inattendue");
	}

	public override void BuildSearch(PropertyFilterSearch search)
	{
		if (_from != int.MinValue)
		{
			string v = PhpSerializer.FromJson(JsonSerializer.Serialize(new string[3]
			{
				_from.ToString(),
				_to.ToString(),
				(_nonconnu == true) ? "1" : ((_nonconnu == false) ? "0" : "2")
			}));
			switch (_field)
			{
			case Field.Surface:
				search.Surface = v;
				break;
			case Field.Budget:
				search.Budget = v;
				break;
			case Field.BudgetC:
				search.BudgetC = v;
				break;
			default:
				throw new NotImplementedException();
			}
		}
	}

	public override string GetNotMatchReason(Property property)
	{
		if (_from == int.MinValue)
		{
			return null;
		}
		if (_from == 0 && _to == 0 && !_nonconnu.HasValue)
		{
			return null;
		}
		double? v = _field switch
		{
			Field.Surface => property.PSurface, 
			Field.Budget => property.PPrix, 
			Field.BudgetC => (property.PSurface.HasValue && property.PPrix.HasValue) ? new double?(property.PPrix.Value / property.PSurface.Value) : ((double?)null), 
			_ => throw new NotImplementedException(), 
		};
		if (!v.HasValue)
		{
			return (_nonconnu == true) ? null : $"{_field} : info connue";
		}
		if (_from == 0 && _to == 0)
		{
			return (_nonconnu == false) ? null : $"{_field} : info non connue";
		}
		return (v.Value >= (double)_from && (_to == 0 || v.Value < (double)(_to + 1))) ? null : $"{_field} : {v.Value} pas dans la fourchette {_from} .. {_to}";
	}
}
