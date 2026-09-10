using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using ApiPerleRare.Application.Abstractions;

namespace ApiPerleRare.Application.Contacts;

public sealed class GetContactsRechercheByIdUseCase : IGetContactsRechercheByIdUseCase
{
	private readonly IApplicationDbContext _context;

	public GetContactsRechercheByIdUseCase(IApplicationDbContext context)
	{
		_context = context;
		ContactsRecherchePhpConfig.EnsureRegistered();
	}

	public async Task<ContactsRecherche> Execute(uint id)
	{
		ContactsRecherchePhpConfig.EnsureRegistered();
		ContactsRecherche contactsRecherche = await _context.ContactsRecherche.FindAsync(id);
		if (contactsRecherche == null)
		{
			return null;
		}
		EncodingHelper.FixEncodingInStringProperties(contactsRecherche);
		EFHelper<ContactsRecherche>.ConvertPhpSerializedToJson(contactsRecherche);
		return contactsRecherche;
	}
}
