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

public interface IAddAppointmentUseCase
{
	Task<AddAppointmentResponse> Execute(int conseillerId, RendezVous rendezVous);
}

public interface IFindAppointmentByEvenementUseCase
{
	Task<FindAppointmentFromERefEvenementResponse> Execute(int conseillerId, int eRefEvenement);
}

public interface IDeleteAppointmentByEvenementUseCase
{
	Task<DeleteAppointmentFromERefEvenementResponse> Execute(int conseillerId, int eRefEvenement);
}
