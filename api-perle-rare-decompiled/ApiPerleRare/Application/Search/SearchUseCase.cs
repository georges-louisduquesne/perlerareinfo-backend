using System;
using System.Collections.Generic;
using System.Threading;
using System.Web;

namespace ApiPerleRare.Application.Search;

public sealed class SearchUseCase : ISearchUseCase
{
	private readonly ISearchService _searchService;

	public SearchUseCase(ISearchService searchService)
	{
		_searchService = searchService;
	}

	public IEnumerable<SelectResult> Execute(string filter, int max = 15, CancellationToken cancellationToken = default)
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
				Max = max,
				Cancellation = cancellationToken
			});
		}
		catch (Exception) when (cancellationToken.IsCancellationRequested)
		{
			return new SelectResult[0];
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
