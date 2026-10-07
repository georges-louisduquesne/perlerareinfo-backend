namespace ApiPerleRare.Application.Authentication;

public interface ISwitchDispoMissionUseCase
{
	bool Execute(int refConseiller, string remoteIpAddress);
}
