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

	private readonly IRefreshSessionUseCase _refresh;

	private readonly ISwitchDispoUseCase _switchDispo;

	private readonly ISwitchFilterUseCase _switchFilter;

	public UserController(
		IAuthenticateUseCase authenticate,
		IRefreshSessionUseCase refresh,
		ISwitchDispoUseCase switchDispo,
		ISwitchFilterUseCase switchFilter)
	{
		_authenticate = authenticate;
		_refresh = refresh;
		_switchDispo = switchDispo;
		_switchFilter = switchFilter;
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

	/// <summary>
	/// Sliding 48 h: same session JSON as authenticate, new token. Additive route (front opt-in).
	/// </summary>
	[Authorize]
	[HttpGet("refresh")]
	public IActionResult Refresh()
	{
		AuthenticateResult result = _refresh.Execute(this.GetUserId());
		if (!result.Success)
		{
			return Unauthorized();
		}
		return Ok(result.Session);
	}

	[HttpGet("SwitchDispo")]
	[Authorize]
	public bool SwitchDispo()
	{
		int refConseiller = this.GetUserId();
		IPAddress obj = base.Request?.HttpContext?.Connection?.RemoteIpAddress;
		return _switchDispo.Execute(refConseiller, obj?.ToString());
	}

	[HttpGet("SwitchFilter")]
	[Authorize]
	public bool SwitchFilter()
	{
		return _switchFilter.Execute();
	}
}
