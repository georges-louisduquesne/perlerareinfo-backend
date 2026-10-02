using System;
using System.Threading.Tasks;
using ApiPerleRare.Controllers;

namespace ApiPerleRare.Application.Exchange;

public interface IGetUnreadEmailCountUseCase
{
	UnreadEmailCountResponse Execute(int conseillerId);
}

public interface IGetTodayAppointmentsUseCase
{
	TodayAppointmentsResponse Execute(int conseillerId);
}

public interface IIsUserAvailableUseCase
{
	bool Execute(int conseillerId, string userEmail, DateTime start, DateTime end);
}

public interface IGetSalonCalendarUseCase
{
	System.Collections.Generic.List<SalonCalendarEvent> Execute(int conseillerId, DateTime start, DateTime end);
}

public interface IAddAppointmentUseCase
{
	Task<AddAppointmentResponse> Execute(int conseillerId, RendezVous rendezVous);

	Task<AddAppointmentResponse> ExecuteForOrganizer(int conseillerId, RendezVous rendezVous, int callerId, bool callerIsAdmin);
}

public interface IFindAppointmentByEvenementUseCase
{
	Task<FindAppointmentFromERefEvenementResponse> Execute(int conseillerId, int eRefEvenement);
}

public interface IDeleteAppointmentByEvenementUseCase
{
	Task<DeleteAppointmentFromERefEvenementResponse> Execute(int conseillerId, int eRefEvenement);

	Task<DeleteAppointmentFromERefEvenementResponse> ExecuteIfAllowed(int conseillerId, int eRefEvenement, int callerId, bool callerIsAdmin);

	Task<DeleteAppointmentFromERefEvenementResponse> ExecuteByAppointmentId(int conseillerId, string appointmentId, int callerId, bool callerIsAdmin);
}
