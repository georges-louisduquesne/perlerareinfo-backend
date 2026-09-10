using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using ApiPerleRare.Application.Abstractions;

namespace ApiPerleRare.Application.Annonces;

public sealed class GetAnnoncesGlobalesByIdUseCase : IGetAnnoncesGlobalesByIdUseCase
{
	private readonly IApplicationDbContext _context;

	public GetAnnoncesGlobalesByIdUseCase(IApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<AnnoncesGlobales> Execute(uint id, int photos)
	{
		IQueryable<AnnoncesGlobales> query = _context.AnnoncesGlobales;
		AnnoncesGlobales annoncesGlobales = await query.FirstOrDefaultAsync((AnnoncesGlobales ag) => ag.AgRef == id);
		if (annoncesGlobales == null)
		{
			return null;
		}
		if (photos != 0)
		{
			if (string.IsNullOrWhiteSpace(annoncesGlobales.AgIdPropertyYanport))
			{
				annoncesGlobales.Photos = new PhotosAnnonces[0];
			}
			else
			{
				AnnoncesGlobales annoncesGlobales2 = annoncesGlobales;
				annoncesGlobales2.Photos = await (from a in _context.PhotosAnnonces
					where a.PaIdPropertyYanport == annoncesGlobales.AgIdPropertyYanport
					orderby a.PaRef
					select a).ToArrayAsync();
			}
		}
		return annoncesGlobales;
	}
}
