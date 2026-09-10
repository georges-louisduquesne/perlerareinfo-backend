using System.Collections.Generic;
using System.Threading.Tasks;
using ApiPerleRare.Controllers;

namespace ApiPerleRare.Application.Contacts;

public interface IListContactsRechercheExUseCase
{
	Task<List<ContactsRechercheEx>> Execute(string select, string where, string orderby, int skip, int take, bool applyFilter, string userLogin);
}
