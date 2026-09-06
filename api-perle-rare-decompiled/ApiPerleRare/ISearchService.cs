using System.Collections.Generic;

namespace ApiPerleRare;

public interface ISearchService
{
	List<SelectResult> Search(SearchQuery query);

	void ClearCache();
}
