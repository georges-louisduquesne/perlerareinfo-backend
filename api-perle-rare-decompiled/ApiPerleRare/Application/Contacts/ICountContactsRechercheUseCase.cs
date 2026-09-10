using System.Threading.Tasks;

namespace ApiPerleRare.Application.Contacts;

public interface ICountContactsRechercheUseCase
{
	Task<int> Execute(string where, bool applyFilter, string userLogin);
}
