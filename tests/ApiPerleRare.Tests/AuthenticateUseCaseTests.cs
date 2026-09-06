using ApiPerleRare.Application.Authentication;
using ApiPerleRare.Models;
using Xunit;

namespace ApiPerleRare.Tests;

public class AuthenticateUseCaseTests
{
	[Fact]
	public void Maps_persistence_user_to_front_session_without_password()
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
			CpMel = "anna@test.local",
			CpMotDePasse = "secret-must-not-leak"
		}, "jwt-1");
		var useCase = new AuthenticateUseCase(users);

		AuthenticateResult result = useCase.Execute("anna", "x");

		Assert.True(result.Success);
		Assert.Equal("Anna", result.Session.FirstName);
		Assert.Equal("Martin", result.Session.LastName);
		Assert.Equal("anna", result.Session.Login);
		Assert.Equal(7u, result.Session.Id);
		Assert.Equal("jwt-1", result.Session.Token);
		Assert.False(result.Session.IsAdmin);
		Assert.True(result.Session.IsNegociateur);
		Assert.True(result.Session.Filter);
		Assert.Equal("anna@test.local", result.Session.Email);
		string json = System.Text.Json.JsonSerializer.Serialize(result.Session);
		Assert.DoesNotContain("secret-must-not-leak", json);
		Assert.DoesNotContain("cpMotDePasse", json);
	}

	[Fact]
	public void Returns_fail_when_directory_rejects_credentials()
	{
		var useCase = new AuthenticateUseCase(new StubUsers(null, null));
		AuthenticateResult result = useCase.Execute("x", "y");
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
			token = _token;
			return _user;
		}

		public bool SwitchDispo(int refConseiller, string remoteIpAddress) => false;

		public ConseillersPersonnels GetConseiller(string login) => null;

		public string GetInfo() => "";
	}
}
