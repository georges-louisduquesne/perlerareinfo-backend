using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Controllers;

namespace ApiPerleRare.Application.Events;

public interface IListEvenementsExUseCase
{
	Task<List<EvenementsEx>> Execute(EntityQuery query, string option, string userLogin, bool applyNegociateurFilter, CancellationToken cancellationToken = default);
}
