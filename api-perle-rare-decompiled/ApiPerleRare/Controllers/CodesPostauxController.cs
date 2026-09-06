using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Models;

namespace ApiPerleRare.Controllers;

public class CodesPostauxController : QueryEntitiesController<CodesPostaux>
{
	public CodesPostauxController(IQueryEntitiesUseCase<CodesPostaux> query)
		: base(query)
	{
	}
}
