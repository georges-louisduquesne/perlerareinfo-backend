using System;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using ApiPerleRare.Application.Abstractions;

namespace ApiPerleRare.Application.Tasks;

public sealed class CountActiveTachesUseCase : ICountActiveTachesUseCase
{
	private readonly IApplicationDbContext _context;

	public CountActiveTachesUseCase(IApplicationDbContext context)
	{
		_context = context;
	}

	public System.Threading.Tasks.Task<int> Execute(string userLogin)
	{
		return _context.Taches.CountAsync((Taches f) => f.TQui == userLogin && f.TEtat != "fait" && f.TDateRealisation <= DateTime.Now);
	}
}
