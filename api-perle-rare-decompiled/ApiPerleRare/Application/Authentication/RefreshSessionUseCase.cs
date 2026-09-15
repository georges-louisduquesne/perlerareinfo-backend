namespace ApiPerleRare.Application.Authentication;

public sealed class RefreshSessionUseCase : IRefreshSessionUseCase
{
	private readonly IUserService _users;

	public RefreshSessionUseCase(IUserService users)
	{
		_users = users;
	}

	public AuthenticateResult Execute(int userId)
	{
		var user = _users.RefreshSession(userId, out string token);
		if (user == null)
		{
			return AuthenticateResult.Fail();
		}
		return AuthenticateResult.Ok(UserSessionMapper.ToSession(user, token));
	}
}
