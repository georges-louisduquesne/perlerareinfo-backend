using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;

namespace ApiPerleRare.Controllers;

[EnableCors]
[Authorize(Roles = "Admin")]
public class ConseillersPersonnelsTypesEvenementsController : QueryEntitiesController<ConseillersPersonnelsTypesEvenements>
{
	public ConseillersPersonnelsTypesEvenementsController(IQueryEntitiesUseCase<ConseillersPersonnelsTypesEvenements> query)
		: base(query)
	{
	}
}
