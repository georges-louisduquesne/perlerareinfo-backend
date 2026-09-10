using System.Collections.Generic;

namespace ApiPerleRare.Application.Search;

public interface ISearchUseCase
{
	IEnumerable<SelectResult> Execute(string filter, int max = 15);
}
