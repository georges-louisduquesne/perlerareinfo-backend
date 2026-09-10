using System.Collections.Generic;
using System.Threading.Tasks;
using ApiPerleRare.Controllers;

namespace ApiPerleRare.Application.Contacts;

public interface IListContactsRechercheAccueilUseCase
{
	Task<List<ContactsRechercheAccueil>> Execute(string where, string orderby, int skip, int take, int withAnnonces, int withTaches, bool applyFilter, string userLogin);
}
