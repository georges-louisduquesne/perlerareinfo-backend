using System.Threading.Tasks;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPerleRare.Controllers;

/// <summary>
/// Shared HTTP adapter for frozen list queries. Route stays <c>api/{controller}</c>.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public abstract class QueryEntitiesController<TEntity> : ControllerBase where TEntity : class
{
	private readonly IQueryEntitiesUseCase<TEntity> _query;

	protected QueryEntitiesController(IQueryEntitiesUseCase<TEntity> query)
	{
		_query = query;
	}

	[HttpGet]
	public async Task<ActionResult<SelectResult<TEntity>>> Get(
		[FromQuery] string select = null,
		[FromQuery] string where = null,
		[FromQuery] string orderby = null,
		[FromQuery] int skip = 0,
		[FromQuery] int take = 0)
	{
		return await _query.Execute(new EntityQuery
		{
			Select = select,
			Where = where,
			OrderBy = orderby,
			Skip = skip,
			Take = take
		});
	}
}
