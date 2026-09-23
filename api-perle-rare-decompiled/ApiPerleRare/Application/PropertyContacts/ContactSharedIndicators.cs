using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Application.PropertyContacts;

public sealed class ContactSharedIndicatorsRequest
{
	public string[] PropertyIds { get; set; }
}

public sealed class SharedIndicatorItemDto
{
	public string PropertyId { get; set; }

	public uint RefContact { get; set; }

	public string Name { get; set; }

	public string Color { get; set; }

	public string RoleKind { get; set; }

	public string RoleName { get; set; }

	public string Since { get; set; }

	public string EventType { get; set; }

	public string EventWhen { get; set; }

	public int? EventRef { get; set; }
}

public sealed class ContactSharedIndicatorsResponse
{
	public Dictionary<string, List<SharedIndicatorItemDto>> Own { get; set; } = new();

	public Dictionary<string, List<SharedIndicatorItemDto>> OtherEvents { get; set; } = new();

	public Dictionary<string, List<SharedIndicatorItemDto>> SharedPc { get; set; } = new();
}

public static class ContactSharedIndicatorsQuery
{
	private const int MaxPropertyIds = 2500;

	public static async Task<ContactSharedIndicatorsResponse> LoadAsync(
		ApplicationDbContext context,
		uint contactRef,
		IReadOnlyList<string> propertyIds)
	{
		var response = new ContactSharedIndicatorsResponse();
		if (contactRef == 0)
		{
			return response;
		}

		var ids = NormalizePropertyIds(propertyIds);
		if (ids.Count == 0)
		{
			ids = await context.PropertyContact.AsNoTracking()
				.Where((PropertyContact pc) => pc.PcRefContact == contactRef && pc.PcActif)
				.Select((PropertyContact pc) => pc.PcPropertyId)
				.Distinct()
				.Take(MaxPropertyIds)
				.ToListAsync();
		}

		if (ids.Count == 0)
		{
			return response;
		}

		var idSet = ids.ToHashSet(StringComparer.Ordinal);

		var ownPcs = await context.PropertyContact.AsNoTracking()
			.Where((PropertyContact pc) => pc.PcRefContact == contactRef && pc.PcActif && idSet.Contains(pc.PcPropertyId))
			.Select((PropertyContact pc) => new { pc.PcId, pc.PcPropertyId })
			.ToListAsync();
		var ownPcIds = ownPcs.Select((p) => p.PcId).ToHashSet();

		var otherPcs = await context.PropertyContact.AsNoTracking()
			.Where((PropertyContact pc) =>
				pc.PcActif
				&& pc.PcRefContact != contactRef
				&& idSet.Contains(pc.PcPropertyId))
			.Include((PropertyContact pc) => pc.PcRefContactNavigation)
			.ToListAsync();

		var events = await context.Evenements.AsNoTracking()
			.Where((Evenements e) => e.EStatut == 1 && idSet.Contains(e.EPropertyId))
			.ToListAsync();

		var ownContactEvents = await context.Evenements.AsNoTracking()
			.Where((Evenements e) => e.EStatut == 1 && e.ERefContact == contactRef)
			.ToListAsync();

		foreach (var ev in ownContactEvents)
		{
			foreach (var pc in ownPcs)
			{
				if (!EventMatchesOwnPc(ev, pc.PcPropertyId, contactRef))
				{
					continue;
				}
				AddGrouped(response.Own, pc.PcPropertyId, new SharedIndicatorItemDto
				{
					PropertyId = pc.PcPropertyId,
					RefContact = contactRef,
					Name = ev.ETypeEvenement ?? "Évènement",
					Color = "#FFFFFF",
					EventType = ev.ETypeEvenement,
					EventWhen = FormatEventWhen(ev.EDate),
					EventRef = ev.ERefEvenement,
				});
			}
		}

		foreach (var ev in events)
		{
			if (string.IsNullOrWhiteSpace(ev.EPropertyId) || !idSet.Contains(ev.EPropertyId))
			{
				continue;
			}
			var evContact = ev.ERefContact ?? 0;
			if (evContact == contactRef)
			{
				continue;
			}
			AddGrouped(response.OtherEvents, ev.EPropertyId, new SharedIndicatorItemDto
			{
				PropertyId = ev.EPropertyId,
				RefContact = evContact,
				Name = ev.ETypeEvenement ?? "Évènement",
				Color = "#FFFFFF",
				EventType = ev.ETypeEvenement,
				EventWhen = FormatEventWhen(ev.EDate),
				EventRef = ev.ERefEvenement,
			});
		}

		var seenPcKeys = new HashSet<string>(StringComparer.Ordinal);
		foreach (var row in otherPcs)
		{
			if (string.IsNullOrWhiteSpace(row.PcPropertyId))
			{
				continue;
			}
			var key = row.PcPropertyId + ":" + row.PcRefContact;
			if (!seenPcKeys.Add(key))
			{
				continue;
			}
			var cr = row.PcRefContactNavigation;
			var (roleKind, roleName) = RoleFromContact(cr);
			AddGrouped(response.SharedPc, row.PcPropertyId, new SharedIndicatorItemDto
			{
				PropertyId = row.PcPropertyId,
				RefContact = row.PcRefContact,
				Name = FormatContactName(cr, row.PcRefContact),
				Color = StatusColor(cr?.CStatut),
				RoleKind = roleKind,
				RoleName = roleName,
				Since = FormatAffDate(row.PcDateAff),
			});
		}

		return response;
	}

	private static List<string> NormalizePropertyIds(IReadOnlyList<string> propertyIds)
	{
		if (propertyIds == null || propertyIds.Count == 0)
		{
			return new List<string>();
		}
		var seen = new HashSet<string>(StringComparer.Ordinal);
		var outList = new List<string>();
		foreach (var raw in propertyIds)
		{
			var id = (raw ?? "").Trim();
			if (id.Length == 0 || id.Length > 40 || !seen.Add(id))
			{
				continue;
			}
			outList.Add(id);
			if (outList.Count >= MaxPropertyIds)
			{
				break;
			}
		}
		return outList;
	}

	private static bool EventMatchesOwnPc(Evenements ev, string propertyId, uint contactRef)
	{
		if (string.IsNullOrWhiteSpace(propertyId))
		{
			return false;
		}
		var evProp = ev.EPropertyId ?? "";
		if (evProp.Length > 0 && !string.Equals(evProp, propertyId, StringComparison.Ordinal))
		{
			return false;
		}
		var evRef = ev.ERefContact ?? 0;
		return evRef == 0 || evRef == contactRef;
	}

	private static void AddGrouped(
		Dictionary<string, List<SharedIndicatorItemDto>> grouped,
		string propertyId,
		SharedIndicatorItemDto item)
	{
		if (string.IsNullOrWhiteSpace(propertyId))
		{
			return;
		}
		if (!grouped.TryGetValue(propertyId, out var list))
		{
			list = new List<SharedIndicatorItemDto>();
			grouped[propertyId] = list;
		}
		list.Add(item);
	}

	private static (string roleKind, string roleName) RoleFromContact(ContactsRecherche cr)
	{
		if (cr == null)
		{
			return ("", "");
		}
		var conseiller = (cr.CNomFamilleConseiller ?? "").Trim();
		if (conseiller.Length > 0)
		{
			return ("conseiller", conseiller);
		}
		var neg = (cr.CNegociateur ?? "").Trim();
		if (neg.Length > 0)
		{
			return ("negociateur", neg);
		}
		return ("", "");
	}

	private static string FormatContactName(ContactsRecherche cr, uint fallbackRef)
	{
		if (cr == null)
		{
			return "#" + fallbackRef;
		}
		var nom = (cr.CNomFamille ?? "").Trim().ToUpperInvariant();
		var prenomRaw = (cr.CPrenom ?? "").Trim();
		var prenom = "";
		if (prenomRaw.Length > 0)
		{
			prenom = char.ToUpper(prenomRaw[0], CultureInfo.GetCultureInfo("fr-FR")) + prenomRaw.Substring(1).ToLowerInvariant();
		}
		var name = (nom + " " + prenom).Trim();
		return name.Length > 0 ? name : "#" + fallbackRef;
	}

	private static string StatusColor(string statut)
	{
		var key = (statut ?? "").Trim().ToUpperInvariant();
		return key switch
		{
			"CLIENT ACTIF" => "#72AC7A",
			"CLIENT MORT" => "#A3C9A9",
			"PROSPECT ACTIF" => "#F3AD4D",
			"PROSPECT MORT" => "#F3CB94",
			_ => "#FFFFFF",
		};
	}

	private static string FormatAffDate(DateTime value)
	{
		if (value.Year < 1900)
		{
			return "";
		}
		return value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
	}

	private static string FormatEventWhen(DateTime value)
	{
		if (value.Year < 1900)
		{
			return "";
		}
		return value.ToString("dd/MM HH:mm", CultureInfo.InvariantCulture);
	}
}
