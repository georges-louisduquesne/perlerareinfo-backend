using System;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Application.Exchange;

public sealed class ConseillerMailboxLookup : IConseillerMailboxLookup
{
	private readonly IApplicationDbContext _db;

	public ConseillerMailboxLookup(IApplicationDbContext db)
	{
		_db = db;
	}

	public ConseillerMailbox GetByConseillerId(int conseillerId)
	{
		var cp = (from c in _db.ConseillersPersonnels
			where (long)c.CpRefConseiller == (long)conseillerId
			select new { c.CpMel, c.CpMelMotDePasse }).SingleOrDefault();
		if (cp == null)
		{
			throw new Exception($"Aucun conseiller '{conseillerId}'");
		}
		return new ConseillerMailbox
		{
			Email = cp.CpMel,
			Password = cp.CpMelMotDePasse
		};
	}

	public async Task<ConseillerMailbox> GetByConseillerIdAsync(int conseillerId)
	{
		var cp = await (from c in _db.ConseillersPersonnels
			where (long)c.CpRefConseiller == (long)conseillerId
			select new { c.CpMel, c.CpMelMotDePasse }).SingleOrDefaultAsync();
		if (cp == null)
		{
			throw new Exception($"Aucun conseiller '{conseillerId}'");
		}
		return new ConseillerMailbox
		{
			Email = cp.CpMel,
			Password = cp.CpMelMotDePasse
		};
	}
}
