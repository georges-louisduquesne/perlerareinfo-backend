using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using ApiPerleRare.Application.Abstractions;

namespace ApiPerleRare.Application.Events;

public sealed class ListContactEvenementsUseCase : IListContactEvenementsUseCase
{
	private readonly IApplicationDbContext _context;

	public ListContactEvenementsUseCase(IApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<List<ContactEvenements>> Execute(EntityQuery query)
	{
		query ??= new EntityQuery();
		IQueryable<ContactEvenements> contactEvenements = _context.ContactEvenements.AsNoTracking();
		contactEvenements = EFHelper<ContactEvenements>.Apply(contactEvenements, query.Where, query.OrderBy, query.Take, query.Skip, query.Select);
		return await contactEvenements.ToListAsync();
	}
}
