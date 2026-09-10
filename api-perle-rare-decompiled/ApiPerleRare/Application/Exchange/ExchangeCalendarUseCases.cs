using System;
using System.Threading.Tasks;
using ApiPerleRare.Controllers;

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

public sealed class AddAppointmentUseCase : IAddAppointmentUseCase
{
	private readonly IConseillerMailboxLookup _mailboxes;

	private readonly IExchangeService _exchange;

	public AddAppointmentUseCase(IConseillerMailboxLookup mailboxes, IExchangeService exchange)
	{
		_mailboxes = mailboxes;
		_exchange = exchange;
	}

	public async Task<AddAppointmentResponse> Execute(int conseillerId, RendezVous rendezVous)
	{
		try
		{
			ConseillerMailbox cp = await _mailboxes.GetByConseillerIdAsync(conseillerId);
			AddAppointmentRes res = await _exchange.AddAppointment(cp.Email, cp.Password, rendezVous);
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
}

public sealed class FindAppointmentByEvenementUseCase : IFindAppointmentByEvenementUseCase
{
	private readonly IConseillerMailboxLookup _mailboxes;

	private readonly IExchangeService _exchange;

	public FindAppointmentByEvenementUseCase(IConseillerMailboxLookup mailboxes, IExchangeService exchange)
	{
		_mailboxes = mailboxes;
		_exchange = exchange;
	}

	public async Task<FindAppointmentFromERefEvenementResponse> Execute(int conseillerId, int eRefEvenement)
	{
		try
		{
			ConseillerMailbox cp = await _mailboxes.GetByConseillerIdAsync(conseillerId);
			RendezVous rdv = await _exchange.FindAppointmentFromERefEvenement(cp.Email, cp.Password, eRefEvenement);
			return new FindAppointmentFromERefEvenementResponse
			{
				Error = null,
				Appointment = rdv
			};
		}
		catch (Exception ex)
		{
			return new FindAppointmentFromERefEvenementResponse
			{
				Error = ex.ToString()
			};
		}
	}
}

public sealed class DeleteAppointmentByEvenementUseCase : IDeleteAppointmentByEvenementUseCase
{
	private readonly IConseillerMailboxLookup _mailboxes;

	private readonly IExchangeService _exchange;

	public DeleteAppointmentByEvenementUseCase(IConseillerMailboxLookup mailboxes, IExchangeService exchange)
	{
		_mailboxes = mailboxes;
		_exchange = exchange;
	}

	public async Task<DeleteAppointmentFromERefEvenementResponse> Execute(int conseillerId, int eRefEvenement)
	{
		try
		{
			ConseillerMailbox cp = await _mailboxes.GetByConseillerIdAsync(conseillerId);
			bool found = await _exchange.DeleteAppointmentFromERefEvenement(cp.Email, cp.Password, eRefEvenement);
			return new DeleteAppointmentFromERefEvenementResponse
			{
				Error = null,
				IsDeleted = found
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
}
