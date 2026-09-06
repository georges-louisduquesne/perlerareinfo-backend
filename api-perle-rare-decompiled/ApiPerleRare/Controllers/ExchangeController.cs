using System;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Exchange.WebServices.Data;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ExchangeController : ControllerBase
{
	private readonly IExchangeService _exchangeService;

	private readonly ILocalhostMailService _localhostMailService;

	private readonly ApplicationDbContext _dbContext;

	public ExchangeController(IExchangeService exchangeService, ApplicationDbContext dbContext, ILocalhostMailService localhostMailService)
	{
		_exchangeService = exchangeService;
		_localhostMailService = localhostMailService;
		_dbContext = dbContext;
	}

	[Authorize(Roles = "Admin")]
	[HttpGet]
	[Route("GetUnreadEmailCount/{conseillerId}")]
	public UnreadEmailCountResponse GetUnreadEmailCount(int conseillerId)
	{
		try
		{
			var cp = (from conseillersPersonnels in _dbContext.ConseillersPersonnels
				where (long)conseillersPersonnels.CpRefConseiller == (long)conseillerId
				select new { conseillersPersonnels.CpMel, conseillersPersonnels.CpMelMotDePasse }).SingleOrDefault();
			if (cp == null)
			{
				throw new Exception($"Aucun conseiller '{conseillerId}'");
			}
			return new UnreadEmailCountResponse
			{
				Unread = _exchangeService.GetUnreadEmails(cp.CpMel, cp.CpMelMotDePasse)
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

	[Authorize]
	[HttpGet]
	[Route("GetUnreadEmailCount")]
	public UnreadEmailCountResponse GetUnreadEmailCount()
	{
		return GetUnreadEmailCount(this.GetUserId());
	}

	[Authorize]
	[HttpGet]
	[Route("GetTodayAppointments")]
	public TodayAppointmentsResponse GetTodayAppointments()
	{
		return GetTodayAppointments(this.GetUserId());
	}

	[Authorize(Roles = "Admin")]
	[HttpGet]
	[Route("GetTodayAppointments/{conseillerId}")]
	public TodayAppointmentsResponse GetTodayAppointments(int conseillerId)
	{
		try
		{
			var cp = (from conseillersPersonnels in _dbContext.ConseillersPersonnels
				where (long)conseillersPersonnels.CpRefConseiller == (long)conseillerId
				select new { conseillersPersonnels.CpMel, conseillersPersonnels.CpMelMotDePasse }).SingleOrDefault();
			if (cp == null)
			{
				throw new Exception($"Aucun conseiller '{conseillerId}'");
			}
			return _exchangeService.GetTodayAppointments(cp.CpMel, cp.CpMelMotDePasse);
		}
		catch (Exception ex)
		{
			return new TodayAppointmentsResponse
			{
				Error = ex.ToString()
			};
		}
	}

	[Authorize(Roles = "Admin")]
	[HttpPut]
	[Route("AddAppointment/{conseillerId}")]
	public async Task<AddAppointmentResponse> AddAppointment(int conseillerId, RendezVous rendezVous)
	{
		try
		{
			var cp = await (from conseillersPersonnels in _dbContext.ConseillersPersonnels
				where (long)conseillersPersonnels.CpRefConseiller == (long)conseillerId
				select new { conseillersPersonnels.CpMel, conseillersPersonnels.CpMelMotDePasse }).SingleOrDefaultAsync();
			if (cp == null)
			{
				throw new Exception($"Aucun conseiller '{conseillerId}'");
			}
			AddAppointmentRes res = await _exchangeService.AddAppointment(cp.CpMel, cp.CpMelMotDePasse, rendezVous);
			return new AddAppointmentResponse
			{
				Id = res.Id,
				Updated = res.Updated,
				Error = null
			};
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return new AddAppointmentResponse
			{
				Error = ex2.ToString()
			};
		}
	}

	[Authorize]
	[HttpPut]
	[Route("AddAppointment")]
	public Task<AddAppointmentResponse> AddAppointment(RendezVous rendezVous)
	{
		return AddAppointment(this.GetUserId(), rendezVous);
	}

	[Authorize]
	[HttpPost]
	[Route("SendEmail")]
	public string SendEmail(Email em)
	{
		try
		{
			if (em.SenderEmail != null && !em.SenderEmail.EndsWith("@perle-rare.com"))
			{
				_localhostMailService.SendMail(em.SenderName, em.SenderEmail, em.To, em.Subject, em.HtmlContents);
			}
			else
			{
				_exchangeService.SendMail(em.To, em.Subject, em.HtmlContents, delegate(EmailMessage e)
				{
					if (!string.IsNullOrWhiteSpace(em.SenderEmail))
					{
						e.Sender = new EmailAddress(em.SenderName, em.SenderEmail);
					}
				});
			}
			return "OK";
		}
		catch (Exception ex)
		{
			return ex.ToString();
		}
	}

	[Authorize]
	[HttpGet]
	[Route("FindAppointmentFromERefEvenement/{eRefEvenement}")]
	public Task<FindAppointmentFromERefEvenementResponse> FindAppointmentFromERefEvenement(int eRefEvenement)
	{
		return FindAppointmentFromERefEvenement(this.GetUserId(), eRefEvenement);
	}

	private async Task<FindAppointmentFromERefEvenementResponse> FindAppointmentFromERefEvenement(int conseillerId, int ERefEvenement)
	{
		try
		{
			var cp = await (from conseillersPersonnels in _dbContext.ConseillersPersonnels
				where (long)conseillersPersonnels.CpRefConseiller == (long)conseillerId
				select new { conseillersPersonnels.CpMel, conseillersPersonnels.CpMelMotDePasse }).SingleOrDefaultAsync();
			if (cp == null)
			{
				throw new Exception($"Aucun conseiller '{conseillerId}'");
			}
			RendezVous rdv = await _exchangeService.FindAppointmentFromERefEvenement(cp.CpMel, cp.CpMelMotDePasse, ERefEvenement);
			return new FindAppointmentFromERefEvenementResponse
			{
				Error = null,
				Appointment = rdv
			};
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return new FindAppointmentFromERefEvenementResponse
			{
				Error = ex2.ToString()
			};
		}
	}

	[Authorize]
	[HttpDelete]
	[Route("DeleteAppointmentFromERefEvenement/{eRefEvenement}")]
	public Task<DeleteAppointmentFromERefEvenementResponse> DeleteAppointmentFromERefEvenement(int eRefEvenement)
	{
		return DeleteAppointmentFromERefEvenement(this.GetUserId(), eRefEvenement);
	}

	private async Task<DeleteAppointmentFromERefEvenementResponse> DeleteAppointmentFromERefEvenement(int conseillerId, int ERefEvenement)
	{
		try
		{
			var cp = await (from conseillersPersonnels in _dbContext.ConseillersPersonnels
				where (long)conseillersPersonnels.CpRefConseiller == (long)conseillerId
				select new { conseillersPersonnels.CpMel, conseillersPersonnels.CpMelMotDePasse }).SingleOrDefaultAsync();
			if (cp == null)
			{
				throw new Exception($"Aucun conseiller '{conseillerId}'");
			}
			bool found = await _exchangeService.DeleteAppointmentFromERefEvenement(cp.CpMel, cp.CpMelMotDePasse, ERefEvenement);
			return new DeleteAppointmentFromERefEvenementResponse
			{
				Error = null,
				IsDeleted = found
			};
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return new DeleteAppointmentFromERefEvenementResponse
			{
				Error = ex2.ToString()
			};
		}
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
