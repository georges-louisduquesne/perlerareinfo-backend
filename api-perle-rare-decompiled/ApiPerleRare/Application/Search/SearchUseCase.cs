using System;
using System.Collections.Generic;
using System.Web;

namespace ApiPerleRare.Application.Search;

public sealed class SearchUseCase : ISearchUseCase
{
	private readonly ISearchService _searchService;

	public SearchUseCase(ISearchService searchService)
	{
		_searchService = searchService;
	}

	public IEnumerable<SelectResult> Execute(string filter, int max = 15)
	{
		if (string.IsNullOrWhiteSpace(filter))
		{
			return new SelectResult[0];
		}
		try
		{
			string sqlFilter = filter.Replace("%", "\\%");
			return _searchService.Search(new SearchQuery
			{
				Filter = filter,
				SqlFilter = sqlFilter,
				HtmlFilter = HttpUtility.HtmlEncode(filter),
				Max = max
			});
		}
		catch (Exception ex)
		{
			return new SelectResult[1]
			{
				new SelectResult
				{
					Category = "Erreur",
					Description = ex.ToString()
				}
			};
		}
	}
}
