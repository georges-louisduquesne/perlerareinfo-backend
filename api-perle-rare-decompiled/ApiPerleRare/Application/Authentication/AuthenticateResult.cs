using ApiPerleRare.Entities;

namespace ApiPerleRare.Application.Authentication;

public sealed class AuthenticateResult
{
	public bool Success { get; }

	public UserModel Session { get; }

	private AuthenticateResult(bool success, UserModel session)
	{
		Success = success;
		Session = session;
	}

	public static AuthenticateResult Fail() => new(false, null);

	public static AuthenticateResult Ok(UserModel session) => new(true, session);
}
