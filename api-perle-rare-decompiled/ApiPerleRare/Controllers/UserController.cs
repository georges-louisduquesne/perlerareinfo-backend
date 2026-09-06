using System.Net;
using ApiPerleRare.Entities;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[EnableCors]
[ApiController]
public class UserController : ControllerBase
{
	private readonly IUserService _userService;

	private readonly IUserSessionService _userSessionService;

	public UserController(IUserService userService, IUserSessionService userSessionService)
	{
		_userService = userService;
		_userSessionService = userSessionService;
	}

	[AllowAnonymous]
	[HttpPost("authenticate")]
	public IActionResult Authenticate([FromBody] AuthenticateModel model)
	{
		string token;
		ConseillersPersonnels user = _userService.Authenticate(model.Login, model.Password, out token);
		if (user == null)
		{
			return BadRequest(new
			{
				message = "Login or password is incorrect"
			});
		}
		return Ok(new UserModel
		{
			FirstName = user.CpPrenom,
			LastName = user.CpNomFamille,
			Login = user.CpLogin,
			Id = user.CpRefConseiller,
			Token = token,
			IsAdmin = user.CpAdmin,
			IsNegociateur = user.CpNegociateur,
			AutoLogin = user.CpAutoLogin,
			Dispo = user.CpDispo,
			Filter = !user.CpAdmin,
			Email = user.CpMel
		});
	}

	[HttpGet("SwitchDispo")]
	[Authorize]
	public bool SwitchDispo()
	{
		int refConseiller = this.GetUserId();
		IPAddress? obj = base.Request?.HttpContext?.Connection?.RemoteIpAddress;
		return _userService.SwitchDispo(refConseiller, obj?.ToString());
	}

	[HttpGet("SwitchFilter")]
	[Authorize]
	public bool SwitchFilter()
	{
		_userSessionService.Filter = !_userSessionService.Filter;
		return _userSessionService.Filter;
	}
}
