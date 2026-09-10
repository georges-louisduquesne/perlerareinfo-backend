using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;

namespace ApiPerleRare.Application.Tasks;

public interface IListClientTachesUseCase
{
	System.Threading.Tasks.Task<SelectResult<ProspectTaches>> Execute(EntityQuery query, string userLogin, bool applyQuiFilter);
}
