using ApiPerleRare.Application.Authentication;
using ApiPerleRare.Models;
using Xunit;

namespace ApiPerleRare.Tests;

public class RefreshSessionUseCaseTests
{
	[Fact]
	public void Maps_active_user_to_front_session()
	{
		var users = new StubUsers(new ConseillersPersonnels
		{
			CpRefConseiller = 7,
			CpPrenom = "Anna",
			CpNomFamille = "Martin",
			CpLogin = "anna",
			CpAdmin = false,
			CpNegociateur = true,
			CpAutoLogin = "abc",
			CpDispo = 0,
			CpMel = "anna@test.local"
		}, "jwt-refresh");
		var useCase = new RefreshSessionUseCase(users);

		AuthenticateResult result = useCase.Execute(7);

		Assert.True(result.Success);
		Assert.Equal("Anna", result.Session.FirstName);
		Assert.Equal("jwt-refresh", result.Session.Token);
		Assert.Equal("anna", result.Session.Login);
		Assert.True(result.Session.IsNegociateur);
	}

	[Fact]
	public void Returns_fail_when_directory_rejects()
	{
		var useCase = new RefreshSessionUseCase(new StubUsers(null, null));
		AuthenticateResult result = useCase.Execute(99);
		Assert.False(result.Success);
		Assert.Null(result.Session);
	}

	private sealed class StubUsers : IUserService
	{
		private readonly ConseillersPersonnels _user;

		private readonly string _token;

		public StubUsers(ConseillersPersonnels user, string token)
		{
			_user = user;
			_token = token;
		}

		public ConseillersPersonnels Authenticate(string username, string password, out string token)
		{
			token = null;
			return null;
		}

		public ConseillersPersonnels RefreshSession(int userId, out string token)
		{
			token = _token;
			return _user;
		}

		public bool SwitchDispo(int refConseiller, string remoteIpAddress) => false;

		public ConseillersPersonnels GetConseiller(string login) => null;

		public void WarmConseillers(System.Collections.Generic.IEnumerable<string> logins)
		{
		}

		public string GetInfo() => "";
	}
}
