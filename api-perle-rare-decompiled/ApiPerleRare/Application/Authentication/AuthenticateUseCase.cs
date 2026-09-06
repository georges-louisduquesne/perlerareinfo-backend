namespace ApiPerleRare.Application.Authentication;

public sealed class AuthenticateUseCase : IAuthenticateUseCase
{
	private readonly IUserService _users;

	public AuthenticateUseCase(IUserService users)
	{
		_users = users;
	}

	public AuthenticateResult Execute(string login, string password)
	{
		var user = _users.Authenticate(login, password, out string token);
		if (user == null)
		{
			return AuthenticateResult.Fail();
		}
		return AuthenticateResult.Ok(UserSessionMapper.ToSession(user, token));
	}
}
