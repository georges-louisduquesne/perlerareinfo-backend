using System.Threading.Tasks;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;

namespace ApiPerleRare.Application.Events;

public interface IListHomeClientEvenementsUseCase
{
	Task<SelectResult<ClientEvenements>> Execute(HomeEventKind kind, EntityQuery query, string userLogin, bool applyNegociateurFilter);
}
