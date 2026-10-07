using System.Collections.Generic;
using System.Threading.Tasks;
using ApiPerleRare.Application.Disponibilites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[EnableCors]
[ApiController]
[Authorize]
public class DisponibilitesController : ControllerBase
{
	private readonly IListActiveMissionLoginsUseCase _missions;

	public DisponibilitesController(IListActiveMissionLoginsUseCase missions)
	{
		_missions = missions;
	}

	/// <summary>
	/// Un login par mission active (CLIENT ACTIF, recherche en cours), sans filtre conseiller.
	/// </summary>
	[HttpGet("MissionsActives")]
	public Task<List<string>> MissionsActives()
	{
		return _missions.Execute();
	}
}
