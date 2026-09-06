using ApiPerleRare.Models;

namespace ApiPerleRare.RecupInfos;

public abstract class AbstractPropertyFilter
{
	public abstract void BuildSearch(PropertyFilterSearch search);

	public abstract string GetNotMatchReason(Property property);
}
