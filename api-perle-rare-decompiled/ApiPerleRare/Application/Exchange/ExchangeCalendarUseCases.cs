using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Application.Abstractions;
using ApiPerleRare.Controllers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Application.Exchange;

public sealed class GetUnreadEmailCountUseCase : IGetUnreadEmailCountUseCase
{
	private readonly IConseillerMailboxLookup _mailboxes;

	private readonly IExchangeService _exchange;

	public GetUnreadEmailCountUseCase(IConseillerMailboxLookup mailboxes, IExchangeService exchange)
	{
		_mailboxes = mailboxes;
		_exchange = exchange;
	}

	public UnreadEmailCountResponse Execute(int conseillerId)
	{
		try
		{
			ConseillerMailbox cp = _mailboxes.GetByConseillerId(conseillerId);
			return new UnreadEmailCountResponse
			{
				Unread = _exchange.GetUnreadEmails(cp.Email, cp.Password)
			};
		}
		catch (Exception ex)
		{
			return new UnreadEmailCountResponse
			{
				Unread = null,
				Error = ex.ToString()
			};
		}
	}
}

public sealed class GetTodayAppointmentsUseCase : IGetTodayAppointmentsUseCase
{
	private readonly IConseillerMailboxLookup _mailboxes;

	private readonly IExchangeService _exchange;

	public GetTodayAppointmentsUseCase(IConseillerMailboxLookup mailboxes, IExchangeService exchange)
	{
		_mailboxes = mailboxes;
		_exchange = exchange;
	}

	public TodayAppointmentsResponse Execute(int conseillerId)
	{
		try
		{
			ConseillerMailbox cp = _mailboxes.GetByConseillerId(conseillerId);
			return _exchange.GetTodayAppointments(cp.Email, cp.Password);
		}
		catch (Exception ex)
		{
			return new TodayAppointmentsResponse
			{
				Error = ex.ToString()
			};
		}
	}
}

public sealed class IsUserAvailableUseCase : IIsUserAvailableUseCase
{
	private readonly IConseillerMailboxLookup _mailboxes;

	private readonly IExchangeService _exchange;

	public IsUserAvailableUseCase(IConseillerMailboxLookup mailboxes, IExchangeService exchange)
	{
		_mailboxes = mailboxes;
		_exchange = exchange;
	}

	public bool Execute(int conseillerId, string userEmail, DateTime start, DateTime end)
	{
		ConseillerMailbox cp = _mailboxes.GetByConseillerId(conseillerId);
		return _exchange.IsUserAvailable(cp.Email, cp.Password, userEmail, start, end);
	}
}

public sealed class GetSalonCalendarUseCase : IGetSalonCalendarUseCase
{
	private readonly IConseillerMailboxLookup _mailboxes;

	private readonly IExchangeService _exchange;

	public GetSalonCalendarUseCase(IConseillerMailboxLookup mailboxes, IExchangeService exchange)
	{
		_mailboxes = mailboxes;
		_exchange = exchange;
	}

	public System.Collections.Generic.List<SalonCalendarEvent> Execute(int conseillerId, DateTime start, DateTime end)
	{
		ConseillerMailbox cp = _mailboxes.GetByConseillerId(conseillerId);
		return _exchange.GetSalonCalendar(cp.Email, cp.Password, start, end);
	}
}

public sealed class AddAppointmentUseCase : IAddAppointmentUseCase
{
	private readonly IConseillerMailboxLookup _mailboxes;

	private readonly IExchangeService _exchange;

	private readonly IApplicationDbContext _db;

	public AddAppointmentUseCase(IConseillerMailboxLookup mailboxes, IExchangeService exchange, IApplicationDbContext db)
	{
		_mailboxes = mailboxes;
		_exchange = exchange;
		_db = db;
	}

	public async Task<AddAppointmentResponse> ExecuteForOrganizer(int conseillerId, RendezVous rendezVous, int callerId, bool callerIsAdmin)
	{
		int eventId = rendezVous?.ERefEvenement ?? 0;
		if (!await AgendaMailboxGuard.CanWrite(_db, _mailboxes, _exchange, conseillerId, eventId, callerId, callerIsAdmin))
		{
			return new AddAppointmentResponse
			{
				Error = "Agenda non autorisé pour ce conseiller."
			};
		}
		AddAppointmentResponse result = await Execute(conseillerId, rendezVous);
		if (!string.IsNullOrEmpty(result.Error) || callerId == conseillerId || eventId <= 0)
		{
			return result;
		}
		try
		{
			ConseillerMailbox caller = await _mailboxes.GetByConseillerIdAsync(callerId);
			await _exchange.DeleteAppointmentFromERefEvenement(caller.Email, caller.Password, eventId);
		}
		catch
		{
			// Le rendez-vous est déjà dans l'agenda choisi.
		}
		return result;
	}

	public async Task<AddAppointmentResponse> Execute(int conseillerId, RendezVous rendezVous)
	{
		try
		{
			ConseillerMailbox cp = await _mailboxes.GetByConseillerIdAsync(conseillerId);
			AddAppointmentRes res = await _exchange.AddAppointment(cp.Email, cp.Password, rendezVous);
			await PersistAppointmentId(rendezVous?.ERefEvenement ?? 0, res?.Id, conseillerId);
			return new AddAppointmentResponse
			{
				Id = res.Id,
				Updated = res.Updated,
				Error = null
			};
		}
		catch (Exception ex)
		{
			return new AddAppointmentResponse
			{
				Error = ex.ToString()
			};
		}
	}

	private async Task PersistAppointmentId(int eventId, string appointmentId, int conseillerId)
	{
		if (eventId <= 0 || string.IsNullOrWhiteSpace(appointmentId))
		{
			return;
		}
		Evenements row = await _db.Evenements.FirstOrDefaultAsync((e) => e.ERefEvenement == eventId);
		if (row == null)
		{
			return;
		}
		row.EAppointmentId = appointmentId;
		if (conseillerId > 0)
		{
			row.ERefConseiller = (uint)conseillerId;
		}
		await _db.SaveChangesAsync();
	}
}

public sealed class FindAppointmentByEvenementUseCase : IFindAppointmentByEvenementUseCase
{
	private readonly IConseillerMailboxLookup _mailboxes;

	private readonly IExchangeService _exchange;

	private readonly IApplicationDbContext _db;

	public FindAppointmentByEvenementUseCase(IConseillerMailboxLookup mailboxes, IExchangeService exchange, IApplicationDbContext db)
	{
		_mailboxes = mailboxes;
		_exchange = exchange;
		_db = db;
	}

	public async Task<FindAppointmentFromERefEvenementResponse> Execute(int conseillerId, int eRefEvenement)
	{
		List<int> mailboxIds = new List<int>();
		int? organizerId = await AgendaMailboxGuard.EventOrganizerId(_db, eRefEvenement);
		if (organizerId.HasValue)
		{
			mailboxIds.Add(organizerId.Value);
		}
		if (!mailboxIds.Contains(conseillerId))
		{
			mailboxIds.Add(conseillerId);
		}
		string lastError = null;
		foreach (int mailboxId in mailboxIds)
		{
			try
			{
				ConseillerMailbox cp = await _mailboxes.GetByConseillerIdAsync(mailboxId);
				string appointmentId = null;
				if (organizerId.HasValue && mailboxId == organizerId.Value)
				{
					Evenements row = await _db.Evenements.FirstOrDefaultAsync((e) => e.ERefEvenement == eRefEvenement);
					appointmentId = row?.EAppointmentId;
				}
				RendezVous rdv = await _exchange.FindAppointmentFromERefEvenement(cp.Email, cp.Password, eRefEvenement, appointmentId);
				if (rdv != null)
				{
					return new FindAppointmentFromERefEvenementResponse
					{
						Error = null,
						Appointment = rdv
					};
				}
			}
			catch (Exception ex)
			{
				lastError = ex.ToString();
			}
		}
		return new FindAppointmentFromERefEvenementResponse
		{
			Error = lastError,
			Appointment = null
		};
	}
}

public sealed class DeleteAppointmentByEvenementUseCase : IDeleteAppointmentByEvenementUseCase
{
	private readonly IConseillerMailboxLookup _mailboxes;

	private readonly IExchangeService _exchange;

	private readonly IApplicationDbContext _db;

	public DeleteAppointmentByEvenementUseCase(IConseillerMailboxLookup mailboxes, IExchangeService exchange, IApplicationDbContext db)
	{
		_mailboxes = mailboxes;
		_exchange = exchange;
		_db = db;
	}

	public async Task<DeleteAppointmentFromERefEvenementResponse> ExecuteIfAllowed(int conseillerId, int eRefEvenement, int callerId, bool callerIsAdmin)
	{
		if (!await AgendaMailboxGuard.CanDelete(_db, _mailboxes, _exchange, conseillerId, eRefEvenement, callerId, callerIsAdmin))
		{
			return new DeleteAppointmentFromERefEvenementResponse
			{
				Error = "Agenda non autorisé pour ce conseiller.",
				IsDeleted = false
			};
		}
		return await Execute(conseillerId, eRefEvenement);
	}

	public async Task<DeleteAppointmentFromERefEvenementResponse> Execute(int conseillerId, int eRefEvenement)
	{
		try
		{
			Evenements row = await _db.Evenements.FirstOrDefaultAsync((e) => e.ERefEvenement == eRefEvenement);
			int organizerId = (row != null && row.ERefConseiller.HasValue) ? (int)row.ERefConseiller.Value : conseillerId;
			string appointmentId = row?.EAppointmentId;
			bool deleted = false;
			string lastError = null;
			try
			{
				deleted = await DeleteOnMailbox(organizerId, eRefEvenement, appointmentId);
			}
			catch (Exception ex)
			{
				lastError = ex.ToString();
			}
			if (conseillerId != organizerId)
			{
				try
				{
					if (await DeleteOnMailbox(conseillerId, eRefEvenement, null))
					{
						deleted = true;
					}
				}
				catch (Exception ex)
				{
					lastError = ex.ToString();
				}
			}
			if (deleted && row != null && !string.IsNullOrEmpty(row.EAppointmentId))
			{
				row.EAppointmentId = null;
				await _db.SaveChangesAsync();
			}
			return new DeleteAppointmentFromERefEvenementResponse
			{
				Error = deleted ? null : lastError,
				IsDeleted = deleted
			};
		}
		catch (Exception ex)
		{
			return new DeleteAppointmentFromERefEvenementResponse
			{
				Error = ex.ToString()
			};
		}
	}

	public async Task<DeleteAppointmentFromERefEvenementResponse> ExecuteByAppointmentId(int conseillerId, string appointmentId, int callerId, bool callerIsAdmin)
	{
		if (string.IsNullOrWhiteSpace(appointmentId))
		{
			return new DeleteAppointmentFromERefEvenementResponse
			{
				Error = "Identifiant d'agenda manquant.",
				IsDeleted = false
			};
		}
		if (!callerIsAdmin && conseillerId != callerId)
		{
			return new DeleteAppointmentFromERefEvenementResponse
			{
				Error = "Agenda non autorisé pour ce conseiller.",
				IsDeleted = false
			};
		}
		try
		{
			ConseillerMailbox cp = await _mailboxes.GetByConseillerIdAsync(conseillerId);
			bool deleted = await _exchange.DeleteAppointmentById(cp.Email, cp.Password, appointmentId.Trim());
			return new DeleteAppointmentFromERefEvenementResponse
			{
				Error = deleted ? null : "Créneau introuvable.",
				IsDeleted = deleted
			};
		}
		catch (Exception ex)
		{
			return new DeleteAppointmentFromERefEvenementResponse
			{
				Error = ex.ToString(),
				IsDeleted = false
			};
		}
	}

	private async Task<bool> DeleteOnMailbox(int mailboxId, int eRefEvenement, string appointmentId)
	{
		ConseillerMailbox cp = await _mailboxes.GetByConseillerIdAsync(mailboxId);
		return await _exchange.DeleteAppointmentFromERefEvenement(cp.Email, cp.Password, eRefEvenement, appointmentId);
	}
}
