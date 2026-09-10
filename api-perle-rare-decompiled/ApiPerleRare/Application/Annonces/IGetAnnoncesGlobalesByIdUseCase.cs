using System.Threading.Tasks;
using ApiPerleRare.Models;

namespace ApiPerleRare.Application.Annonces;

public interface IGetAnnoncesGlobalesByIdUseCase
{
	Task<AnnoncesGlobales> Execute(uint id, int photos);
}
