using ApiPerleRare;
using ApiPerleRare.Application.Authentication;
using ApiPerleRare.Models;
using Xunit;

namespace ApiPerleRare.Tests;

public class SwitchSessionUseCaseTests
{
	[Fact]
	public void SwitchDispo_delegates_to_directory_with_ip()
	{
		var users = new RecordingUsers();
		var useCase = new SwitchDispoUseCase(users);

		bool result = useCase.Execute(7, "10.0.0.8");

		Assert.True(result);
		Assert.Equal(7, users.LastRef);
		Assert.Equal("10.0.0.8", users.LastIp);
	}

	[Fact]
	public void SwitchFilter_toggles_session_flag()
	{
		var session = new StubSession { Filter = false };
		var useCase = new SwitchFilterUseCase(session);

		Assert.True(useCase.Execute());
		Assert.True(session.Filter);
		Assert.False(useCase.Execute());
		Assert.False(session.Filter);
	}

	private sealed class RecordingUsers : IUserService
	{
		public int LastRef;

		public string LastIp;

		public ConseillersPersonnels Authenticate(string username, string password, out string token)
		{
			token = null;
			return null;
		}

		public bool SwitchDispo(int refConseiller, string remoteIpAddress)
		{
			LastRef = refConseiller;
			LastIp = remoteIpAddress;
			return true;
		}

		public ConseillersPersonnels GetConseiller(string login) => null;

		public void WarmConseillers(System.Collections.Generic.IEnumerable<string> logins)
		{
		}

		public string GetInfo() => "";
	}

	private sealed class StubSession : IUserSessionService
	{
		public bool IsAdmin { get; set; }

		public bool Filter { get; set; }

		public string Login => "demo";

		public string Statut => "";

		public void ClearCache()
		{
		}
	}
}
