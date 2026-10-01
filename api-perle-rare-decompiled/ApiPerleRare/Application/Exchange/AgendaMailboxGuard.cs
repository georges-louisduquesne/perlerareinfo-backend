using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using ApiPerleRare;

namespace ApiPerleRare.Application.Exchange;

internal static class AgendaMailboxGuard
{
	public static async Task<int?> EventOrganizerId(IApplicationDbContext db, int eRefEvenement)
	{
		if (eRefEvenement <= 0)
		{
			return null;
		}
		uint? organizer = await db.Evenements.AsNoTracking()
			.Where((e) => e.ERefEvenement == eRefEvenement)
			.Select((e) => e.ERefConseiller)
			.FirstOrDefaultAsync();
		if (!organizer.HasValue)
		{
			return null;
		}
		return (int)organizer.Value;
	}

	public static async Task<bool> CanWrite(
		IApplicationDbContext db,
		IConseillerMailboxLookup mailboxes,
		IExchangeService exchange,
		int targetId,
		int eRefEvenement,
		int callerId,
		bool callerIsAdmin)
	{
		if (callerIsAdmin || targetId == callerId)
		{
			return true;
		}
		if (await TargetIsContactRole(db, targetId, eRefEvenement))
		{
			return true;
		}
		return await ItemExists(mailboxes, exchange, targetId, eRefEvenement);
	}

	public static async Task<bool> CanDelete(
		IApplicationDbContext db,
		IConseillerMailboxLookup mailboxes,
		IExchangeService exchange,
		int targetId,
		int eRefEvenement,
		int callerId,
		bool callerIsAdmin)
	{
		if (callerIsAdmin || targetId == callerId)
		{
			return true;
		}
		if (await TargetIsContactRole(db, targetId, eRefEvenement))
		{
			return true;
		}
		int? organizerId = await EventOrganizerId(db, eRefEvenement);
		if (organizerId.HasValue && organizerId.Value == targetId)
		{
			return true;
		}
		return await ItemExists(mailboxes, exchange, targetId, eRefEvenement);
	}

	private static async Task<bool> TargetIsContactRole(IApplicationDbContext db, int targetId, int eRefEvenement)
	{
		if (eRefEvenement <= 0)
		{
			return false;
		}
		uint? contactId = await db.Evenements.AsNoTracking()
			.Where((e) => e.ERefEvenement == eRefEvenement)
			.Select((e) => e.ERefContact)
			.FirstOrDefaultAsync();
		if (!contactId.HasValue)
		{
			return false;
		}
		var contact = await db.ContactsRecherche.AsNoTracking()
			.Where((c) => c.CRefContact == contactId.Value)
			.Select((c) => new
			{
				c.CApporteur,
				c.C2emeApporteur,
				c.CNegociateur,
				c.C2emeNegociateur,
				c.CNomFamilleConseiller,
				c.C2emeConseiller
			})
			.FirstOrDefaultAsync();
		if (contact == null)
		{
			return false;
		}
		string login = await db.ConseillersPersonnels.AsNoTracking()
			.Where((c) => (long)c.CpRefConseiller == targetId)
			.Select((c) => c.CpLogin)
			.FirstOrDefaultAsync();
		return AgendaOrganizerAccess.LoginIsContactRole(
			login,
			contact.CApporteur,
			contact.C2emeApporteur,
			contact.CNegociateur,
			contact.C2emeNegociateur,
			contact.CNomFamilleConseiller,
			contact.C2emeConseiller);
	}

	private static async Task<bool> ItemExists(
		IConseillerMailboxLookup mailboxes,
		IExchangeService exchange,
		int targetId,
		int eRefEvenement)
	{
		if (eRefEvenement <= 0)
		{
			return false;
		}
		try
		{
			ConseillerMailbox cp = await mailboxes.GetByConseillerIdAsync(targetId);
			RendezVous existing = await exchange.FindAppointmentFromERefEvenement(cp.Email, cp.Password, eRefEvenement);
			return existing != null;
		}
		catch
		{
			return false;
		}
	}
}
