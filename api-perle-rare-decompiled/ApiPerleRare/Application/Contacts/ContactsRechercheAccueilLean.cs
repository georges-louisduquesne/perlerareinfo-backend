using System;
using System.Linq.Expressions;
using ApiPerleRare.Models;

namespace ApiPerleRare.Application.Contacts;

/// <summary>
/// Accueil ne charge pas les TEXT (budget, localisation). La mission reste : icône A, B, L ou V.
/// </summary>
internal static class ContactsRechercheAccueilLean
{
	internal static readonly Expression<Func<ContactsRecherche, ContactsRecherche>> Projection = c => new ContactsRecherche
	{
		CRefContact = c.CRefContact,
		CPrenom = c.CPrenom,
		CNomFamille = c.CNomFamille,
		CDateCreation = c.CDateCreation,
		CDate = c.CDate,
		CDateFin = c.CDateFin,
		CApporteur = c.CApporteur,
		C2emeApporteur = c.C2emeApporteur,
		CNegociateur = c.CNegociateur,
		C2emeNegociateur = c.C2emeNegociateur,
		CNomFamilleConseiller = c.CNomFamilleConseiller,
		C2emeConseiller = c.C2emeConseiller,
		CTypeRecherche = c.CTypeRecherche,
		CStatut = c.CStatut,
		CIdTypeMission = c.CIdTypeMission,
		CMttHono = c.CMttHono
	};
}
