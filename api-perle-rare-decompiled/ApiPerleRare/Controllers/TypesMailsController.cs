using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Models;

namespace ApiPerleRare.Controllers;

public class TypesMailsController : QueryEntitiesController<TypesMails>
{
	public TypesMailsController(IQueryEntitiesUseCase<TypesMails> query)
		: base(query)
	{
	}
}
