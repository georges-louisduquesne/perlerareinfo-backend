using System.Threading.Tasks;
using ApiPerleRare.Models;

namespace ApiPerleRare.Application.Contacts;

public enum UpdateContactsRechercheStatus
{
	Ok,
	IdMismatch,
	NotFound
}

public interface IUpdateContactsRechercheUseCase
{
	Task<UpdateContactsRechercheStatus> Execute(uint id, ContactsRecherche contactsRecherche);
}
