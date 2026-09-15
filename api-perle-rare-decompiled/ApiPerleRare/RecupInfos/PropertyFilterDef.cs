using System.Collections.Generic;
using System.Linq;
using ApiPerleRare.Models;
using ApiPerleRare.RecupInfos.PropertyFilters;

namespace ApiPerleRare.RecupInfos;

public class PropertyFilterDef
{
	private List<AbstractPropertyFilter> _filters = new List<AbstractPropertyFilter>();

	public PropertyFilterDef(PropertyFilterSearch search)
	{
		_filters.Add(new TypeTransactionPropertyFilter((!string.IsNullOrEmpty(search.TypeTransaction)) ? new bool?(search.TypeTransaction == "A") : ((bool?)null)));
		_filters.Add(new TypeBienPropertyFilter(search.TypeBien));
		_filters.Add(new LocalisationPropertyFilter(search.Localisation));
		_filters.Add(new GenericNombrePropertyFilter(GenericNombrePropertyFilter.Field.NbPieces, search.NbPieces));
		_filters.Add(new GenericNombrePropertyFilter(GenericNombrePropertyFilter.Field.NbChambres, search.NbChambres));
		_filters.Add(new GenericNombrePropertyFilter(GenericNombrePropertyFilter.Field.Etage, search.Etage));
		_filters.Add(new GenericRangePropertyFilter(GenericRangePropertyFilter.Field.Surface, search.Surface));
		_filters.Add(new GenericRangePropertyFilter(GenericRangePropertyFilter.Field.Budget, search.Budget));
		_filters.Add(new GenericRangePropertyFilter(GenericRangePropertyFilter.Field.BudgetC, search.BudgetC));
		_filters.Add(new GenericBooleanPropertyFilter(GenericBooleanPropertyFilter.Field.DernEtage, search.DernEtage));
		_filters.Add(new GenericBooleanPropertyFilter(GenericBooleanPropertyFilter.Field.Exclusivite, search.Exclusivite));
		_filters.Add(new GenericBooleanPropertyFilter(GenericBooleanPropertyFilter.Field.EvoPrix, search.EvoPrix));
		_filters.Add(new AnciennetePropertyFilter(search.Anciennete));
		_filters.Add(new TagsPropertyFilter(search.Tags));
	}

	public PropertyFilterSearch GetSearch()
	{
		PropertyFilterSearch search = new PropertyFilterSearch();
		foreach (AbstractPropertyFilter filter in _filters)
		{
			filter.BuildSearch(search);
		}
		return search;
	}

	public bool IsMatch(Property property)
	{
		if (property.PState != 1)
		{
			return false;
		}
		if (property.PDateFin.HasValue && property.PDateFin.Value.Year >= 1000)
		{
			return false;
		}
		string[] notMatchingReasons = (from f in _filters
			select f.GetNotMatchReason(property) into r
			where r != null
			select r).ToArray();
		return notMatchingReasons.Length == 0;
	}
}
