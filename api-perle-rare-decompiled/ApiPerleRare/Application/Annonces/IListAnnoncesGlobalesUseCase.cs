using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;

namespace ApiPerleRare.Application.Annonces;

public interface IListAnnoncesGlobalesUseCase
{
	Task<SelectResult<AnnoncesGlobales>> Execute(string select, string where, string orderby, int skip, int take, int photos);
}
