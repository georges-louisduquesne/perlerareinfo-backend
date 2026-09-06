namespace ApiPerleRare.Application.Authentication;

public interface ISwitchDispoUseCase
{
	bool Execute(int refConseiller, string remoteIpAddress);
}
