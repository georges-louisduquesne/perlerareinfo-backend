using ApiPerleRare.Helpers;
using ApiPerleRare.Models;

namespace ApiPerleRare.Application.Contacts;

/// <summary>
/// Registers PHP-serialized properties for <see cref="ContactsRecherche"/>
/// (same as the former <c>ContactsRecherchesController</c> static ctor).
/// </summary>
internal static class ContactsRecherchePhpConfig
{
	private static bool _registered;

	private static readonly object Gate = new object();

	public static void EnsureRegistered()
	{
		if (_registered)
		{
			return;
		}
		lock (Gate)
		{
			if (_registered)
			{
				return;
			}
			EFHelper<ContactsRecherche>.SetPhpSerialized(
				(ContactsRecherche c) => c.CAnciennete,
				(ContactsRecherche c) => c.CBudget,
				(ContactsRecherche c) => c.CBudgetC,
				(ContactsRecherche c) => c.CSurface,
				(ContactsRecherche c) => c.CTags,
				(ContactsRecherche c) => c.CLocalisation);
			_registered = true;
		}
	}
}
