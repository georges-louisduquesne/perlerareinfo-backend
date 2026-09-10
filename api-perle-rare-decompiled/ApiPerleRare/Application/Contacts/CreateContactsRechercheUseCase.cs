using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using ApiPerleRare.Application.Abstractions;

namespace ApiPerleRare.Application.Contacts;

public sealed class CreateContactsRechercheUseCase : ICreateContactsRechercheUseCase
{
	private readonly IApplicationDbContext _context;

	public CreateContactsRechercheUseCase(IApplicationDbContext context)
	{
		_context = context;
		ContactsRecherchePhpConfig.EnsureRegistered();
	}

	public async Task<ContactsRecherche> Execute(ContactsRecherche ncr)
	{
		ContactsRecherchePhpConfig.EnsureRegistered();
		if (await _context.ContactsRecherche.FirstOrDefaultAsync((ContactsRecherche cr) => cr.CNomFamille == ncr.CNomFamille && cr.CTelPersonnel1 == ncr.CTelPersonnel1 && cr.CMel1 == ncr.CMel1) != null)
		{
			return null;
		}
		ContactsRecherche contactsRecherche = ncr;
		if (contactsRecherche.CKeyWord1 == null)
		{
			contactsRecherche.CKeyWord1 = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.CKeyWord2 == null)
		{
			contactsRecherche.CKeyWord2 = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.CKeyWord3 == null)
		{
			contactsRecherche.CKeyWord3 = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.CMandat == null)
		{
			contactsRecherche.CMandat = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.C2emeApporteur == null)
		{
			contactsRecherche.C2emeApporteur = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.C2emeNegociateur == null)
		{
			contactsRecherche.C2emeNegociateur = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.C2emeConseiller == null)
		{
			contactsRecherche.C2emeConseiller = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.CQrecherche == null)
		{
			contactsRecherche.CQrecherche = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.COrigine == null)
		{
			contactsRecherche.COrigine = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.CConditionCom == null)
		{
			contactsRecherche.CConditionCom = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.CRechercheCom == null)
		{
			contactsRecherche.CRechercheCom = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.CLocalisation == null)
		{
			contactsRecherche.CLocalisation = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.CTags == null)
		{
			contactsRecherche.CTags = "";
		}
		EFHelper<ContactsRecherche>.ConvertPhpSerializedToPhp(ncr);
		_context.ContactsRecherche.Add(ncr);
		await _context.SaveChangesAsync();
		return ncr;
	}
}
