using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using ApiPerleRare.Application.Abstractions;

namespace ApiPerleRare.Application.Contacts;

public sealed class CountContactsRechercheUseCase : ICountContactsRechercheUseCase
{
	private readonly IApplicationDbContext _context;

	public CountContactsRechercheUseCase(IApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<int> Execute(string where, bool applyFilter, string userLogin)
	{
		IQueryable<ContactsRecherche> query = _context.ContactsRecherche;
		query = ListContactsRechercheExUseCase.ApplyDefaultFilter(query, applyFilter, userLogin);
		query = EFHelper<ContactsRecherche>.Apply(query, where);
		return await query.CountAsync();
	}
}
