using System.Threading;
using ApiPerleRare.RecupInfos;

namespace ApiPerleRare.Application.Annonces;

public interface IRecupInfosAnnonces2UseCase
{
	RecupInfoResponse Execute(Filter filter, CancellationToken cancellationToken = default);
}
