using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using ApiPerleRare.Application.Abstractions;

namespace ApiPerleRare.Application.Contacts;

public sealed class UpdateContactsRechercheUseCase : IUpdateContactsRechercheUseCase
{
	private readonly IApplicationDbContext _context;

	public UpdateContactsRechercheUseCase(IApplicationDbContext context)
	{
		_context = context;
		ContactsRecherchePhpConfig.EnsureRegistered();
	}

	public async Task<UpdateContactsRechercheStatus> Execute(uint id, ContactsRecherche contactsRecherche)
	{
		ContactsRecherchePhpConfig.EnsureRegistered();
		if (id != contactsRecherche.CRefContact)
		{
			return UpdateContactsRechercheStatus.IdMismatch;
		}
		EFHelper<ContactsRecherche>.ConvertPhpSerializedToPhp(contactsRecherche);
		_context.Entry(contactsRecherche).State = EntityState.Modified;
		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException)
		{
			if (!ContactsRechercheExists(id))
			{
				return UpdateContactsRechercheStatus.NotFound;
			}
			throw;
		}
		return UpdateContactsRechercheStatus.Ok;
	}

	private bool ContactsRechercheExists(uint id)
	{
		return _context.ContactsRecherche.Any((ContactsRecherche e) => e.CRefContact == id);
	}
}
