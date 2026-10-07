using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Application.Disponibilites;

public sealed class ListActiveMissionLoginsUseCase : IListActiveMissionLoginsUseCase
{
	private readonly IApplicationDbContext _context;

	public ListActiveMissionLoginsUseCase(IApplicationDbContext context)
	{
		_context = context;
	}

	public Task<List<string>> Execute()
	{
		return ActiveMissionLogins.Query(_context.ContactsRecherche.AsNoTracking())
			.Select((c) => c.CNomFamilleConseiller)
			.ToListAsync();
	}
}
