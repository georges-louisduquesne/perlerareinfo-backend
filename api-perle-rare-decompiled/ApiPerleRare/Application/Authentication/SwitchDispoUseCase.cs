using ApiPerleRare;

namespace ApiPerleRare.Application.Authentication;

public sealed class SwitchDispoUseCase : ISwitchDispoUseCase
{
	private readonly IUserService _users;

	public SwitchDispoUseCase(IUserService users)
	{
		_users = users;
	}

	public bool Execute(int refConseiller, string remoteIpAddress)
	{
		return _users.SwitchDispo(refConseiller, remoteIpAddress);
	}
}
