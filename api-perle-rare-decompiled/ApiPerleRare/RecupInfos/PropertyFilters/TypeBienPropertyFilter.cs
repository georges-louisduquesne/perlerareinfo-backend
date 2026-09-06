using System.Linq;
using ApiPerleRare.Models;

namespace ApiPerleRare.RecupInfos.PropertyFilters;

public class TypeBienPropertyFilter : AbstractPropertyFilter
{
	private readonly string[] _types;

	public TypeBienPropertyFilter(string vals)
	{
		if (vals == null)
		{
			_types = null;
		}
		else
		{
			_types = vals.Split(",");
		}
	}

	public override void BuildSearch(PropertyFilterSearch search)
	{
		if (_types != null)
		{
			search.TypeBien = string.Join(",", _types);
		}
	}

	public override string GetNotMatchReason(Property property)
	{
		if (_types == null)
		{
			return "Type de bien obligatoire !";
		}
		if (property.PType == null)
		{
			return "Bien sans type de bien...";
		}
		return _types.Contains(property.PType) ? null : (property.PType + " pas parmi " + string.Join(", ", _types));
	}
}
