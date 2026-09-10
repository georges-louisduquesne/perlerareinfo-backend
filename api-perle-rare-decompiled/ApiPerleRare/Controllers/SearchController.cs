using System.Collections.Generic;
using ApiPerleRare.Application.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[EnableCors]
[ApiController]
public class SearchController : ControllerBase
{
	private readonly ISearchUseCase _search;

	public SearchController(ISearchUseCase search)
	{
		_search = search;
	}

	[HttpGet("{filter}/{nb:int=15}")]
	[Authorize]
	public IEnumerable<SelectResult> Search(string filter, int nb = 15)
	{
		return _search.Execute(filter, nb);
	}
}
