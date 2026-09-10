using System.Linq;
using ApiPerleRare.Application.Abstractions;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Application.Tasks;

public sealed class CountTachesUseCase : ICountTachesUseCase
{
	private readonly IApplicationDbContext _context;

	public CountTachesUseCase(IApplicationDbContext context)
	{
		_context = context;
	}

	public async System.Threading.Tasks.Task<int> Execute(string where, string option, string userLogin, bool applyQuiFilter)
	{
		IQueryable<Taches> root = TachesQuery.ApplyProspectOption(_context.Taches.AsNoTracking(), option, userLogin, applyQuiFilter);
		IQueryable<TachesEx> taches = TachesQuery.ProjectEx(root);
		taches = EFHelper<TachesEx>.Apply(taches, where);
		return await taches.CountAsync();
	}
}
