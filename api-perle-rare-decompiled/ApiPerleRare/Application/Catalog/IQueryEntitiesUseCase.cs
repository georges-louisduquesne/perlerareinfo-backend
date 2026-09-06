using System.Threading.Tasks;
using ApiPerleRare.Helpers;

namespace ApiPerleRare.Application.Catalog;

public interface IQueryEntitiesUseCase<TEntity> where TEntity : class
{
	Task<SelectResult<TEntity>> Execute(EntityQuery query);
}
