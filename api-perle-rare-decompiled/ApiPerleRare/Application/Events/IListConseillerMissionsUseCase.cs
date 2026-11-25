using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ApiPerleRare.Application.Events;

public interface IListConseillerMissionsUseCase
{
	Task<List<ConseillerMissionRow>> Execute(uint conseillerId, CancellationToken cancellationToken = default);
}
