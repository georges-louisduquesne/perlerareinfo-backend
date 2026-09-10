using System.Threading.Tasks;
using ApiPerleRare.Models;

namespace ApiPerleRare.Application.Contacts;

public interface ICreateContactsRechercheUseCase
{
	/// <summary>
	/// Returns null when a duplicate exists (caller maps to BadRequest "Doublon").
	/// </summary>
	Task<ContactsRecherche> Execute(ContactsRecherche ncr);
}
