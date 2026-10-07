using System.Linq;
using ApiPerleRare.Models;

namespace ApiPerleRare.Application.Disponibilites;

/// <summary>
/// Missions CLIENT ACTIF en recherche (C_TypeRecherche = 0), tous conseillers.
/// Pas de filtre « mes dossiers » : le volume est le même pour chaque utilisateur.
/// </summary>
public static class ActiveMissionLogins
{
	public static IQueryable<ContactsRecherche> Query(IQueryable<ContactsRecherche> source)
	{
		return source.Where((ContactsRecherche c) => c.CStatut == "CLIENT ACTIF" && c.CTypeRecherche == "0");
	}
}
