using System.Net;
using ApiPerleRare.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[EnableCors]
[ApiController]
public class UserController : ControllerBase
{
	private readonly IAuthenticateUseCase _authenticate;

	private readonly IUserService _userService;

	private readonly IUserSessionService _userSessionService;

	public UserController(
		IAuthenticateUseCase authenticate,
		IUserService userService,
		IUserSessionService userSessionService)
	{
		_authenticate = authenticate;
		_userService = userService;
		_userSessionService = userSessionService;
	}

	[AllowAnonymous]
	[HttpPost("authenticate")]
	public IActionResult Authenticate([FromBody] AuthenticateModel model)
	{
		AuthenticateResult result = _authenticate.Execute(model.Login, model.Password);
		if (!result.Success)
		{
			return BadRequest(new
			{
				message = "Login or password is incorrect"
			});
		}
		return Ok(result.Session);
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
