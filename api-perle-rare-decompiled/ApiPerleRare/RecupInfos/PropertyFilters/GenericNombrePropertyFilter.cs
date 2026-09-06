using System;
using System.Linq;
using System.Text.Json;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;

namespace ApiPerleRare.RecupInfos.PropertyFilters;

public class GenericNombrePropertyFilter : AbstractPropertyFilter
{
	public enum Field
	{
		NbPieces,
		NbChambres,
		Etage
	}

	private readonly Field _field;

	private readonly string[] _values;

	private readonly bool _nonConnue;

	private readonly int _max;

	private readonly int[] _intValues;

	public GenericNombrePropertyFilter(Field field, string value)
	{
		_field = field;
		if (value != null)
		{
			string json = PhpSerializer.ToJson(value);
			if (json == "{}")
			{
				_values = Array.Empty<string>();
			}
			else
			{
				_values = JsonSerializer.Deserialize<string[]>(json);
			}
			_nonConnue = _values.Any((string v) => v == "NC");
			switch (field)
			{
			case Field.NbPieces:
				_max = 6;
				break;
			case Field.NbChambres:
				_max = 6;
				break;
			case Field.Etage:
				_max = 4;
				break;
			default:
				throw new NotImplementedException();
			}
			_intValues = _values.Where((string v) => v != "NC").Select(int.Parse).ToArray();
			if (!_intValues.Contains(_max))
			{
				_max = -1;
			}
		}
	}

	public override void BuildSearch(PropertyFilterSearch search)
	{
		if (_values != null)
		{
			string val;
			if (_values.Length == 0)
			{
				val = "a:0:{}";
			}
			else
			{
				string json = JsonSerializer.Serialize(_values);
				val = PhpSerializer.FromJson(json);
			}
			switch (_field)
			{
			case Field.NbPieces:
				search.NbPieces = val;
				break;
			case Field.NbChambres:
				search.NbChambres = val;
				break;
			case Field.Etage:
				search.Etage = val;
				break;
			default:
				throw new NotImplementedException();
			}
		}
	}

	public override string GetNotMatchReason(Property property)
	{
		if (_values == null)
		{
			return null;
		}
		if (_values.Length == 0 && !_nonConnue)
		{
			return null;
		}
		byte? v = _field switch
		{
			Field.NbPieces => property.PNbPieces, 
			Field.NbChambres => property.PNbChambres, 
			Field.Etage => property.PEtage, 
			_ => throw new NotImplementedException(), 
		};
		if (!v.HasValue)
		{
			return _nonConnue ? null : $"{_field} doit être renseigné";
		}
		return (_intValues.Contains(v.Value) || (_max > 0 && v.Value >= _max)) ? null : $"{_field} : {v.Value} pas parmi les valeurs {string.Join(", ", _intValues)}";
	}
}
