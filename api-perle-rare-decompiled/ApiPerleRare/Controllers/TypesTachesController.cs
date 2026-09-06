using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Models;

namespace ApiPerleRare.Controllers;

public class TypesTachesController : QueryEntitiesController<TypesTaches>
{
	public TypesTachesController(IQueryEntitiesUseCase<TypesTaches> query)
		: base(query)
	{
	}
}
