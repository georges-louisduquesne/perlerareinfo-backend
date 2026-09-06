using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Cors;

namespace ApiPerleRare.Controllers;

[EnableCors]
public class TypesEvenementsController : QueryEntitiesController<TypesEvenements>
{
	public TypesEvenementsController(IQueryEntitiesUseCase<TypesEvenements> query)
		: base(query)
	{
	}
}
