using ApiPerleRare;

namespace ApiPerleRare.Application.Authentication;

public sealed class SwitchFilterUseCase : ISwitchFilterUseCase
{
	private readonly IUserSessionService _session;

	public SwitchFilterUseCase(IUserSessionService session)
	{
		_session = session;
	}

	public bool Execute()
	{
		_session.Filter = !_session.Filter;
		return _session.Filter;
	}
}
