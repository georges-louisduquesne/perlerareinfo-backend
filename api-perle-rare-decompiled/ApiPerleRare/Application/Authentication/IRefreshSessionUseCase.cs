namespace ApiPerleRare.Application.Authentication;

public interface IRefreshSessionUseCase
{
	AuthenticateResult Execute(int userId);
}
