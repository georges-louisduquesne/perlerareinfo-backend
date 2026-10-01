using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Cors;

namespace ApiPerleRare.Controllers;

[EnableCors]
public class TypesCatInterIndirectsController : QueryEntitiesController<TypesCatInterIndirect>
{
	public TypesCatInterIndirectsController(IQueryEntitiesUseCase<TypesCatInterIndirect> query)
		: base(query)
	{
	}
}
