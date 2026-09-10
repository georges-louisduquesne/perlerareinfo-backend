using System.Collections.Generic;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Controllers;

namespace ApiPerleRare.Application.Tasks;

public interface IListTachesExUseCase
{
	System.Threading.Tasks.Task<List<TachesEx>> Execute(EntityQuery query, string option, string userLogin, bool applyQuiFilter);
}
