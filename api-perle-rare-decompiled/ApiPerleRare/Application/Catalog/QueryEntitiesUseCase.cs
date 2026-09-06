using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;

namespace ApiPerleRare.Application.Catalog;

public sealed class QueryEntitiesUseCase<TEntity> : IQueryEntitiesUseCase<TEntity> where TEntity : class
{
	private readonly ApplicationDbContext _context;

	public QueryEntitiesUseCase(ApplicationDbContext context)
	{
		_context = context;
	}

	public Task<SelectResult<TEntity>> Execute(EntityQuery query)
	{
		query ??= new EntityQuery();
		return EFHelper<TEntity>.Select(_context.Set<TEntity>(), query.Where, query.OrderBy, query.Take, query.Skip, query.Select);
	}
}
