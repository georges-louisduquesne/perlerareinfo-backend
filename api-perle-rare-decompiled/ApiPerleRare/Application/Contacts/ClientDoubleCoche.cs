using System;
using System.Collections.Generic;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;

namespace ApiPerleRare.Application.Contacts;

/// <summary>
/// Double coche de la colonne État (Clients actifs) : un RV COMPROMIS ou RV ACTE AUTHENT. actif
/// est lié au contact (<c>E_Statut = 1</c>, <c>E_RefContact</c>).
/// </summary>
internal static class ClientDoubleCoche
{
	internal static string BuildSql(string ids)
	{
		return $@"SELECT DISTINCT E_RefContact
FROM evenements
WHERE E_Statut = 1
  AND E_RefContact IN ({ids})
  AND E_TypeEvenement IN ('{BienNotificationGate.TypeCompromis}', '{BienNotificationGate.TypeActe}', '{BienNotificationGate.TypeActeBare}')";
	}

	internal static void Apply(IEnumerable<ContactsRechercheAccueil> rows, IReadOnlySet<uint> contactIds)
	{
		if (rows == null)
		{
			return;
		}
		foreach (ContactsRechercheAccueil row in rows)
		{
			row._DoubleCoche = contactIds != null && contactIds.Contains(row.CRefContact);
		}
	}

	internal static bool TryReadContactId(object value, out uint id)
	{
		switch (value)
		{
			case uint ui:
				id = ui;
				return true;
			case int i when i > 0:
				id = (uint)i;
				return true;
			case long l when l > 0 && l <= uint.MaxValue:
				id = (uint)l;
				return true;
			case ulong ul when ul > 0 && ul <= uint.MaxValue:
				id = (uint)ul;
				return true;
			default:
				id = 0;
				return false;
		}
	}
}
