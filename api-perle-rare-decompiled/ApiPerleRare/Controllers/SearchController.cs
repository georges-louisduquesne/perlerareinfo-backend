using System;
using System.Collections.Generic;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[EnableCors]
[ApiController]
public class SearchController : ControllerBase
{
	private readonly ISearchService _searchService;

	public SearchController(ISearchService searchService)
	{
		_searchService = searchService;
	}

	[HttpGet("{filter}/{nb:int=15}")]
	[Authorize]
	public IEnumerable<SelectResult> Search(string filter, int nb = 15)
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
				Max = nb
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
