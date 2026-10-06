using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Application.Exchange;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ExchangeController : ControllerBase
{
	private readonly IExchangeService _exchangeService;

	private readonly ISendEmailUseCase _sendEmail;

	private readonly IGetUnreadEmailCountUseCase _unreadEmailCount;

	private readonly IGetTodayAppointmentsUseCase _todayAppointments;

	private readonly IIsUserAvailableUseCase _isUserAvailable;

	private readonly IGetSalonCalendarUseCase _salonCalendar;

	private readonly IAddAppointmentUseCase _addAppointment;

	private readonly IFindAppointmentByEvenementUseCase _findAppointment;

	private readonly IDeleteAppointmentByEvenementUseCase _deleteAppointment;

	public ExchangeController(
		IExchangeService exchangeService,
		ISendEmailUseCase sendEmail,
		IGetUnreadEmailCountUseCase unreadEmailCount,
		IGetTodayAppointmentsUseCase todayAppointments,
		IIsUserAvailableUseCase isUserAvailable,
		IGetSalonCalendarUseCase salonCalendar,
		IAddAppointmentUseCase addAppointment,
		IFindAppointmentByEvenementUseCase findAppointment,
		IDeleteAppointmentByEvenementUseCase deleteAppointment)
	{
		_exchangeService = exchangeService;
		_sendEmail = sendEmail;
		_unreadEmailCount = unreadEmailCount;
		_todayAppointments = todayAppointments;
		_isUserAvailable = isUserAvailable;
		_salonCalendar = salonCalendar;
		_addAppointment = addAppointment;
		_findAppointment = findAppointment;
		_deleteAppointment = deleteAppointment;
	}

	[Authorize(Roles = "Admin")]
	[HttpGet]
	[Route("GetUnreadEmailCount/{conseillerId}")]
	public UnreadEmailCountResponse GetUnreadEmailCount(int conseillerId)
	{
		return _unreadEmailCount.Execute(conseillerId);
	}

	[Authorize]
	[HttpGet]
	[Route("GetUnreadEmailCount")]
	public UnreadEmailCountResponse GetUnreadEmailCount()
	{
		return _unreadEmailCount.Execute(this.GetUserId());
	}

	[Authorize]
	[HttpGet]
	[Route("GetTodayAppointments")]
	public TodayAppointmentsResponse GetTodayAppointments()
	{
		return _todayAppointments.Execute(this.GetUserId());
	}

	[Authorize(Roles = "Admin")]
	[HttpGet]
	[Route("GetTodayAppointments/{conseillerId}")]
	public TodayAppointmentsResponse GetTodayAppointments(int conseillerId)
	{
		return _todayAppointments.Execute(conseillerId);
	}

	[Authorize]
	[HttpPut]
	[Route("AddAppointment/{conseillerId}")]
	public Task<AddAppointmentResponse> AddAppointment(int conseillerId, RendezVous rendezVous)
	{
		return _addAppointment.ExecuteForOrganizer(conseillerId, rendezVous, this.GetUserId(), User.IsInRole("Admin"));
	}

	[Authorize]
	[HttpPost]
	[Route("AddAppointment/{conseillerId}")]
	public Task<AddAppointmentResponse> PostAppointment(int conseillerId, RendezVous rendezVous)
	{
		return _addAppointment.ExecuteForOrganizer(conseillerId, rendezVous, this.GetUserId(), User.IsInRole("Admin"));
	}

	[Authorize]
	[HttpPut]
	[Route("AddAppointment")]
	public Task<AddAppointmentResponse> AddAppointment(RendezVous rendezVous)
	{
		return _addAppointment.Execute(this.GetUserId(), rendezVous);
	}

	[Authorize]
	[HttpPost]
	[Route("AddAppointment")]
	public Task<AddAppointmentResponse> PostAppointment(RendezVous rendezVous)
	{
		return _addAppointment.Execute(this.GetUserId(), rendezVous);
	}

	[Authorize]
	[HttpGet]
	[Route("IsUserAvailable")]
	public bool IsUserAvailable(string userEmail, DateTime start, DateTime end)
	{
		return _isUserAvailable.Execute(this.GetUserId(), userEmail, start, end);
	}

	[Authorize]
	[HttpGet]
	[Route("GetSalonCalendar")]
	public List<SalonCalendarEvent> GetSalonCalendar(DateTime start, DateTime end)
	{
		return _salonCalendar.Execute(this.GetUserId(), start, end);
	}

	[Authorize]
	[HttpPost]
	[Route("SendEmail")]
	public string SendEmail(Email em)
	{
		int? conseillerId = User.TryGetId(out int id) ? id : null;
		return _sendEmail.Execute(em, conseillerId);
	}

	[Authorize]
	[HttpGet]
	[Route("FindAppointmentFromERefEvenement/{eRefEvenement}")]
	public Task<FindAppointmentFromERefEvenementResponse> FindAppointmentFromERefEvenement(int eRefEvenement)
	{
		return _findAppointment.Execute(this.GetUserId(), eRefEvenement);
	}

	[Authorize]
	[HttpDelete]
	[Route("DeleteAppointmentFromERefEvenement/{eRefEvenement}")]
	public Task<DeleteAppointmentFromERefEvenementResponse> DeleteAppointmentFromERefEvenement(int eRefEvenement)
	{
		return _deleteAppointment.Execute(this.GetUserId(), eRefEvenement);
	}

	[Authorize]
	[HttpDelete]
	[Route("DeleteAppointmentFromERefEvenement/{eRefEvenement}/{conseillerId}")]
	public Task<DeleteAppointmentFromERefEvenementResponse> DeleteAppointmentFromERefEvenement(int eRefEvenement, int conseillerId)
	{
		return _deleteAppointment.ExecuteIfAllowed(conseillerId, eRefEvenement, this.GetUserId(), User.IsInRole("Admin"));
	}

	[Authorize]
	[HttpDelete]
	[Route("DeleteAppointment/{conseillerId}")]
	public Task<DeleteAppointmentFromERefEvenementResponse> DeleteAppointment(int conseillerId, [FromQuery] string appointmentId)
	{
		return _deleteAppointment.ExecuteByAppointmentId(conseillerId, appointmentId, this.GetUserId(), User.IsInRole("Admin"));
	}

	[Authorize]
	[Route("GetTimeZones")]
	[HttpGet]
	public string[] GetTimeZoneIds()
	{
		return (from tz in TimeZoneInfo.GetSystemTimeZones()
			select tz.Id + ":" + tz.DisplayName).ToArray();
	}

	[Authorize]
	[Route("GetCurrentTimeZone")]
	[HttpGet]
	public string GetCurrentTimeZone()
	{
		TimeZoneInfo tz = _exchangeService.CurrentTimeZone;
		return tz.Id + ":" + tz.DisplayName;
	}
}
