using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Models;

namespace ApiPerleRare.Controllers;

public class TemplatesController : QueryEntitiesController<Templates>
{
	public TemplatesController(IQueryEntitiesUseCase<Templates> query)
		: base(query)
	{
	}
}
