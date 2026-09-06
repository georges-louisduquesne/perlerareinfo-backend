using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Models;

namespace ApiPerleRare.Controllers;

public class QualiteRelationIntermediairesController : QueryEntitiesController<QualiteRelationIntermediaire>
{
	public QualiteRelationIntermediairesController(IQueryEntitiesUseCase<QualiteRelationIntermediaire> query)
		: base(query)
	{
	}
}
