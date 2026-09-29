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

	public string RolePrenom { get; set; }

	public string RoleNom { get; set; }

	public string RoleLogin { get; set; }

	public string RolePhoto { get; set; }

	public string RoleTel { get; set; }

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
			.Where((PropertyContact pc) => pc.PcRefContact == contactRef && idSet.Contains(pc.PcPropertyId))
			.Select((PropertyContact pc) => new { pc.PcId, pc.PcPropertyId })
			.ToListAsync();

		var otherPcs = await context.PropertyContact.AsNoTracking()
			.Where((PropertyContact pc) =>
				pc.PcActif
				&& pc.PcRefContact != contactRef
				&& idSet.Contains(pc.PcPropertyId))
			.Include((PropertyContact pc) => pc.PcRefContactNavigation)
			.ToListAsync();

		var visitTypes = await LoadVisitTypesAsync(context);

		var events = await context.Evenements.AsNoTracking()
			.Where((Evenements e) => e.EStatut == 1 && idSet.Contains(e.EPropertyId) && visitTypes.Contains(e.ETypeEvenement))
			.ToListAsync();

		var ownContactEvents = await context.Evenements.AsNoTracking()
			.Where((Evenements e) => e.EStatut == 1 && e.ERefContact == contactRef && visitTypes.Contains(e.ETypeEvenement))
			.ToListAsync();

		foreach (var ev in ownContactEvents)
		{
			foreach (var pc in ownPcs)
			{
				if (!OwnVisitMatchesPropertyContact(ev.EPropertyId, ev.EPcId, ev.ERefContact, pc.PcId, pc.PcPropertyId, contactRef))
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

		var otherEventRefs = events
			.Select((ev) => ev.ERefContact ?? 0)
			.Where((r) => r != 0 && r != contactRef)
			.Distinct()
			.ToList();
		var otherEventContacts = otherEventRefs.Count == 0
			? new List<ContactsRecherche>()
			: await context.ContactsRecherche.AsNoTracking()
				.Where((ContactsRecherche c) => otherEventRefs.Contains(c.CRefContact))
				.ToListAsync();
		var contactsByRef = otherEventContacts.ToDictionary((c) => c.CRefContact);
		var counselorRows = await context.ConseillersPersonnels.AsNoTracking()
			.Select((ConseillersPersonnels c) => new
			{
				c.CpLogin,
				c.CpPrenom,
				c.CpNomFamille,
				c.CpPhotoSignature,
				c.CpTelPersonnel,
			})
			.ToListAsync();
		var counselors = counselorRows.Select((c) => new CounselorMatch
		{
			Login = c.CpLogin,
			Prenom = c.CpPrenom,
			Nom = c.CpNomFamille,
			Photo = c.CpPhotoSignature,
			Tel = c.CpTelPersonnel,
		}).ToList();

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
			contactsByRef.TryGetValue(evContact, out var otherContact);
			var item = new SharedIndicatorItemDto
			{
				PropertyId = ev.EPropertyId,
				RefContact = evContact,
				Name = otherContact == null ? "" : FormatContactName(otherContact, evContact),
				Color = StatusColor(otherContact?.CStatut),
				EventType = ev.ETypeEvenement,
				EventWhen = FormatEventWhen(ev.EDate),
				EventRef = ev.ERefEvenement,
			};
			ApplyCounselor(item, otherContact, counselors);
			AddGrouped(response.OtherEvents, ev.EPropertyId, item);
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
			var item = new SharedIndicatorItemDto
			{
				PropertyId = row.PcPropertyId,
				RefContact = row.PcRefContact,
				Name = FormatContactName(cr, row.PcRefContact),
				Color = StatusColor(cr?.CStatut),
				Since = FormatAffDate(row.PcDateAff),
			};
			ApplyCounselor(item, cr, counselors);
			AddGrouped(response.SharedPc, row.PcPropertyId, item);
		}

		return response;
	}

	/// <summary>Visit event types: types_evenements 5 / 6, or a category starting with « vis ».</summary>
	public static readonly string[] DefaultVisitTypes = { "RV VISITE SEUL", "RV VISITE CLIENT" };

	private static async Task<List<string>> LoadVisitTypesAsync(ApplicationDbContext context)
	{
		var rows = await context.TypesEvenements.AsNoTracking()
			.Select((TypesEvenements te) => new { te.TeRefTypeEvenement, te.TeTypeEvenement, te.TeCategorieEvenement })
			.ToListAsync();
		return SelectVisitTypes(rows.Select((r) => (r.TeRefTypeEvenement, r.TeTypeEvenement, r.TeCategorieEvenement)));
	}

	public static List<string> SelectVisitTypes(IEnumerable<(int refType, string type, string category)> rows)
	{
		var set = new HashSet<string>(DefaultVisitTypes, StringComparer.OrdinalIgnoreCase);
		foreach (var (refType, type, category) in rows ?? Enumerable.Empty<(int, string, string)>())
		{
			var name = (type ?? "").Trim();
			if (name.Length == 0)
			{
				continue;
			}
			var cat = (category ?? "").Trim();
			if (refType == 5 || refType == 6 || cat.StartsWith("vis", StringComparison.OrdinalIgnoreCase))
			{
				set.Add(name);
			}
		}
		return set.ToList();
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

	/// <summary>
	/// Pastille verte : visite liée à cette fiche (E_PC_Id) ou au même bien (E_PropertyId).
	/// Une visite du contact sans ces deux liens ne s'affiche sur aucune carte.
	/// </summary>
	public static bool OwnVisitMatchesPropertyContact(
		string eventPropertyId,
		uint? eventPcId,
		uint? eventContactRef,
		uint pcId,
		string propertyId,
		uint contactRef)
	{
		if (eventPcId is uint evPc && evPc > 0 && pcId > 0 && evPc == pcId)
		{
			return true;
		}
		if (string.IsNullOrWhiteSpace(propertyId) || string.IsNullOrWhiteSpace(eventPropertyId))
		{
			return false;
		}
		if (!string.Equals(eventPropertyId.Trim(), propertyId.Trim(), StringComparison.Ordinal))
		{
			return false;
		}
		var evRef = eventContactRef ?? 0;
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

	public sealed class CounselorMatch
	{
		public string Login { get; set; }

		public string Prenom { get; set; }

		public string Nom { get; set; }

		public string Photo { get; set; }

		public string Tel { get; set; }
	}

	/// <summary>Login d'abord, sinon nom de famille.</summary>
	public static CounselorMatch MatchCounselor(IEnumerable<CounselorMatch> people, string key)
	{
		var needle = (key ?? "").Trim();
		if (needle.Length == 0 || people == null)
		{
			return null;
		}
		CounselorMatch byNom = null;
		foreach (var person in people)
		{
			if (person == null)
			{
				continue;
			}
			if (string.Equals((person.Login ?? "").Trim(), needle, StringComparison.OrdinalIgnoreCase))
			{
				return person;
			}
			if (byNom == null && string.Equals((person.Nom ?? "").Trim(), needle, StringComparison.OrdinalIgnoreCase))
			{
				byNom = person;
			}
		}
		return byNom;
	}

	private static void ApplyCounselor(SharedIndicatorItemDto dto, ContactsRecherche cr, IReadOnlyList<CounselorMatch> people)
	{
		var (kind, name) = RoleFromContact(cr);
		dto.RoleKind = kind;
		dto.RoleName = name;
		var match = MatchCounselor(people, name);
		if (match == null)
		{
			return;
		}
		dto.RolePrenom = (match.Prenom ?? "").Trim();
		dto.RoleNom = string.IsNullOrWhiteSpace(match.Nom) ? name : match.Nom.Trim();
		dto.RoleLogin = (match.Login ?? "").Trim();
		dto.RolePhoto = (match.Photo ?? "").Trim();
		dto.RoleTel = (match.Tel ?? "").Trim();
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
