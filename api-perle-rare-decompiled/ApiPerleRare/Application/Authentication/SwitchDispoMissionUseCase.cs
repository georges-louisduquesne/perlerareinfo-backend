using ApiPerleRare;

namespace ApiPerleRare.Application.Authentication;

public sealed class SwitchDispoMissionUseCase : ISwitchDispoMissionUseCase
{
	private readonly IUserService _users;

	public SwitchDispoMissionUseCase(IUserService users)
	{
		_users = users;
	}

	public bool Execute(int refConseiller, string remoteIpAddress)
	{
		return _users.SwitchDispoMission(refConseiller, remoteIpAddress);
	}
}
