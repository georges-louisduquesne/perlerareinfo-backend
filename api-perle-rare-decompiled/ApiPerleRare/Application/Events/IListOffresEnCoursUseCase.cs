using System.Threading.Tasks;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;

namespace ApiPerleRare.Application.Events;

public interface IListOffresEnCoursUseCase
{
	Task<SelectResult<ClientEvenements>> Execute(EntityQuery query, uint eRefConseiller, string userLogin, bool applyNegociateurFilter);
}
