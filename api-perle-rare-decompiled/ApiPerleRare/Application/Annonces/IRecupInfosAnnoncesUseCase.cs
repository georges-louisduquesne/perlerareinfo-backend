using System.Threading.Tasks;
using ApiPerleRare.Controllers;

namespace ApiPerleRare.Application.Annonces;

public interface IRecupInfosAnnoncesUseCase
{
	Task<AnnoncesGlobalesController.InfoAnnonces> Execute(AnnoncesGlobalesController.FilterDef filterDef);
}
