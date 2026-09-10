using System.Threading.Tasks;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;

namespace ApiPerleRare.Application.Events;

/// <summary>
/// Home tab « transactions réalisées en attente d'encaissement ».
/// Same JSON shape as other ClientEvenements lists (<c>items</c>/<c>total</c>).
/// </summary>
public interface IListEncaissementsEnCoursUseCase
{
	Task<SelectResult<ClientEvenements>> Execute(EntityQuery query, string userLogin, bool applyNegociateurFilter);
}
