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

	private readonly ISwitchDispoUseCase _switchDispo;

	private readonly ISwitchFilterUseCase _switchFilter;

	public UserController(
		IAuthenticateUseCase authenticate,
		ISwitchDispoUseCase switchDispo,
		ISwitchFilterUseCase switchFilter)
	{
		_authenticate = authenticate;
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
