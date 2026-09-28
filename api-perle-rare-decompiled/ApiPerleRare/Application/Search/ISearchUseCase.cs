using System.Collections.Generic;
using System.Threading;

namespace ApiPerleRare.Application.Search;

public interface ISearchUseCase
{
	IEnumerable<SelectResult> Execute(string filter, int max = 15, CancellationToken cancellationToken = default);
}
