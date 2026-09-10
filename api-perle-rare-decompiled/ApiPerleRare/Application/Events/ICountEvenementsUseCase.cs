using System.Threading.Tasks;

namespace ApiPerleRare.Application.Events;

public interface ICountEvenementsUseCase
{
	Task<int> Execute(string where, string option, string userLogin, bool applyNegociateurFilter);
}
