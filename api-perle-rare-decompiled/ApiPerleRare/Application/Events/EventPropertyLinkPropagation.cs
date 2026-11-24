using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Application.Events;

/// <summary>
/// À l'enregistrement d'un évènement de visite, d'offre, de compromis, d'acte ou de CR VISITE
/// qui porte déjà un bien lié, recopie E_PropertyId sur les autres évènements actifs du même
/// contact et du même bien dont E_PC_Id et E_PropertyId sont vides. Le trigger
/// evenements_before_update pose ensuite E_PC_Id depuis property_contact.
/// </summary>
internal static class EventPropertyLinkPropagation
{
	internal static readonly string[] Types =
	{
		"RV VISITE SEUL",
		"RV VISITE CLIENT",
		"OFFRE",
		"OFFRE REFUSEE",
		"OFFRE ACCEPTEE",
		"RV COMPROMIS",
		"RV ACTE AUTHENT.",
		"RV ACTE AUTHENT",
		"CR VISITE"
	};

	internal static bool CarriesPropertyLink(Evenements source)
	{
		return source != null
			&& source.ERefContact.GetValueOrDefault() != 0
			&& source.ERefBien.GetValueOrDefault() != 0
			&& source.EPcId.GetValueOrDefault() != 0
			&& !string.IsNullOrWhiteSpace(source.EPropertyId)
			&& Types.Contains(source.ETypeEvenement);
	}

	internal static bool ShouldFill(Evenements source, Evenements target)
	{
		if (!CarriesPropertyLink(source) || target == null || target.ERefEvenement == source.ERefEvenement)
		{
			return false;
		}
		return target.EStatut == 1
			&& Types.Contains(target.ETypeEvenement)
			&& target.ERefContact == source.ERefContact
			&& target.ERefBien == source.ERefBien
			&& target.EPcId.GetValueOrDefault() == 0
			&& string.IsNullOrWhiteSpace(target.EPropertyId);
	}

	internal static async Task ApplyAsync(ApplicationDbContext db, int sourceId, CancellationToken cancellationToken = default)
	{
		if (sourceId <= 0)
		{
			return;
		}
		Evenements source = await db.Evenements.AsNoTracking()
			.FirstOrDefaultAsync((Evenements e) => e.ERefEvenement == sourceId, cancellationToken);
		if (!CarriesPropertyLink(source))
		{
			return;
		}
		List<Evenements> targets = await db.Evenements
			.Where((Evenements e) => e.ERefEvenement != source.ERefEvenement
				&& e.EStatut == 1
				&& e.ERefContact == source.ERefContact
				&& e.ERefBien == source.ERefBien
				&& (e.EPcId == null || e.EPcId == 0)
				&& (e.EPropertyId == null || e.EPropertyId == ""))
			.Where((Evenements e) => Types.Contains(e.ETypeEvenement))
			.ToListAsync(cancellationToken);
		string propertyId = source.EPropertyId.Trim();
		bool changed = false;
		foreach (Evenements target in targets)
		{
			if (!ShouldFill(source, target))
			{
				continue;
			}
			target.EPropertyId = propertyId;
			changed = true;
		}
		if (changed)
		{
			await db.SaveChangesAsync(cancellationToken);
		}
	}
}
