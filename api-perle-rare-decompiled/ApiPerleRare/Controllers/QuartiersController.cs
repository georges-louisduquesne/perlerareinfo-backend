using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Models;

namespace ApiPerleRare.Controllers;

public class QuartiersController : QueryEntitiesController<Quartiers>
{
	public QuartiersController(IQueryEntitiesUseCase<Quartiers> query)
		: base(query)
	{
	}
}
