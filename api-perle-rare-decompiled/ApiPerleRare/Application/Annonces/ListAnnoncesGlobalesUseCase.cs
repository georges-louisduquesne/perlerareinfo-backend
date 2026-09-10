using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using ApiPerleRare.Application.Abstractions;

namespace ApiPerleRare.Application.Annonces;

public sealed class ListAnnoncesGlobalesUseCase : IListAnnoncesGlobalesUseCase
{
	private readonly IApplicationDbContext _context;

	public ListAnnoncesGlobalesUseCase(IApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<SelectResult<AnnoncesGlobales>> Execute(string select, string where, string orderby, int skip, int take, int photos)
	{
		IQueryable<AnnoncesGlobales> query = _context.AnnoncesGlobales;
		SelectResult<AnnoncesGlobales> annonces = await EFHelper<AnnoncesGlobales>.Select(query, where, orderby, take, skip, select);
		if (photos != 0)
		{
			string[] yanportProps = (from i in annonces.Items
				select i.AgIdPropertyYanport into p
				where !string.IsNullOrWhiteSpace(p)
				select p).Distinct().ToArray();
			PhotosAnnonces[] allPhotos = ((yanportProps.Length == 0) ? new PhotosAnnonces[0] : (await (from photosAnnonces in _context.PhotosAnnonces
				where yanportProps.Contains(photosAnnonces.PaIdPropertyYanport)
				orderby photosAnnonces.PaRef
				select photosAnnonces).ToArrayAsync()));
			AnnoncesGlobales[] items = annonces.Items;
			foreach (AnnoncesGlobales a in items)
			{
				a.Photos = allPhotos.Where((PhotosAnnonces p) => p.PaIdPropertyYanport == a.AgIdPropertyYanport).ToArray();
			}
		}
		return annonces;
	}
}
