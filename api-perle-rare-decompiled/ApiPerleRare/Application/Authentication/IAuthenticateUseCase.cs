namespace ApiPerleRare.Application.Authentication;

public interface IAuthenticateUseCase
{
	AuthenticateResult Execute(string login, string password);
}
