using System;
using ApiPerleRare.Models;

namespace ApiPerleRare.RecupInfos.PropertyFilters;

public class GenericBooleanPropertyFilter : AbstractPropertyFilter
{
	public class PrixVal
	{
		public float Prix { get; set; }

		public DateTime Date { get; set; }
	}

	public enum Field
	{
		DernEtage,
		Exclusivite,
		EvoPrix
	}

	private readonly Field _field;

	private readonly bool? _value;

	public GenericBooleanPropertyFilter(Field field, string val)
	{
		_field = field;
		switch (val)
		{
		case "0":
			_value = false;
			break;
		case "1":
			_value = true;
			break;
		case "2":
			_value = null;
			break;
		default:
			throw new NotImplementedException();
		}
	}

	public override void BuildSearch(PropertyFilterSearch search)
	{
		string v = ((_value == true) ? "1" : ((_value == false) ? "0" : "2"));
		switch (_field)
		{
		case Field.DernEtage:
			search.DernEtage = v;
			break;
		case Field.Exclusivite:
			search.Exclusivite = v;
			break;
		case Field.EvoPrix:
			search.EvoPrix = v;
			break;
		default:
			throw new NotImplementedException();
		}
	}

	public override string GetNotMatchReason(Property property)
	{
		if (!_value.HasValue)
		{
			return null;
		}
		bool? actual = _field switch
		{
			Field.DernEtage => property.PEstDernierEtage, 
			Field.Exclusivite => property.PEstExclusif, 
			Field.EvoPrix => GetEvoPrix(property.PPrixEvol), 
			_ => throw new NotImplementedException(), 
		};
		if (!actual.HasValue)
		{
			return null;
		}
		return (_value == actual) ? null : $"{actual} attendu pour {_field}";
	}

	private bool? GetEvoPrix(sbyte? prixEvol)
	{
		if (!prixEvol.HasValue || prixEvol == 0)
		{
			return null;
		}
		return prixEvol == 1;
	}
}
