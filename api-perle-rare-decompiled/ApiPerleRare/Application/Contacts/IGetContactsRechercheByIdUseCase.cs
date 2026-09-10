using System.Threading.Tasks;
using ApiPerleRare.Models;

namespace ApiPerleRare.Application.Contacts;

public interface IGetContactsRechercheByIdUseCase
{
	Task<ContactsRecherche> Execute(uint id);
}
