using System.Threading.Tasks;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;

namespace ApiPerleRare.Application.Events;

public interface IListProspectEvenementsUseCase
{
	Task<SelectResult<ProspectEvenements>> Execute(EntityQuery query, string userLogin, bool applyNegociateurFilter);
}
