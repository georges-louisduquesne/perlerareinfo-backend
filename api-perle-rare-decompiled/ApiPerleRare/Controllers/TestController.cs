using System;
using ApiPerleRare.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[EnableCors]
[ApiController]
public class TestController : ControllerBase
{
	private readonly ITestService _testService;

	private readonly ISearchService _searchService;

	public TestController(ITestService testService, ISearchService searchService)
	{
		_testService = testService;
		_searchService = searchService;
	}

	[Authorize(Roles = "Admin")]
	[HttpGet("info")]
	public IActionResult Info()
	{
		try
		{
			return Ok(_testService.GetInfo());
		}
		catch (Exception)
		{
			return Ok(ClientError.Generic);
		}
	}

	[Authorize(Roles = "Admin")]
	[HttpGet("clearcache")]
	public IActionResult ClearCache()
	{
		try
		{
			_searchService.ClearCache();
			return Ok("Cache vidé");
		}
		catch (Exception)
		{
			return Ok(ClientError.Generic);
		}
	}
}
