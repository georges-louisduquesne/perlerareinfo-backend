using System;
using System.Text.Json;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;

namespace ApiPerleRare.RecupInfos.PropertyFilters;

public class AnciennetePropertyFilter : AbstractPropertyFilter
{
	private int _from;

	private int _to;

	public AnciennetePropertyFilter(string value)
	{
		if (value == null)
		{
			_from = int.MinValue;
			return;
		}
		string json = PhpSerializer.ToJson(value);
		string[] vals = JsonSerializer.Deserialize<string[]>(json);
		_from = int.Parse(vals[0]);
		_to = int.Parse(vals[1]);
	}

	public override void BuildSearch(PropertyFilterSearch search)
	{
		if (_from != int.MinValue)
		{
			string json = JsonSerializer.Serialize(new string[2]
			{
				_from.ToString(),
				_to.ToString()
			});
			search.Anciennete = PhpSerializer.FromJson(json);
		}
	}

	public override string GetNotMatchReason(Property property)
	{
		if (_from == int.MinValue || !property.PDateDebut.HasValue)
		{
			return null;
		}
		if (_from == 0 && _to == 0)
		{
			return null;
		}
		double nbDays = (DateTime.Today - property.PDateDebut.Value.Date).TotalDays;
		return (nbDays >= (double)_from && nbDays <= (double)_to) ? null : $"L'ancienneté devrait être comprise entre {_from} et {_to}. {nbDays} actuellement.";
	}
}
