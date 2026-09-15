using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ApiPerleRare.Tests;

public class ContractIntegrationTests : IClassFixture<ApiFactory>
{
	private readonly HttpClient _client;

	private readonly ApiFactory _factory;

	public ContractIntegrationTests(ApiFactory factory)
	{
		_factory = factory;
		_client = factory.CreateClient();
	}

	[Fact]
	public async Task Authenticate_keeps_front_json_contract()
	{
		var fail = await _client.PostAsJsonAsync("/api/User/authenticate", new { login = "x", password = "y" });
		Assert.Equal(HttpStatusCode.BadRequest, fail.StatusCode);
		using (var failDoc = JsonDocument.Parse(await fail.Content.ReadAsStringAsync()))
		{
			Assert.Equal("Login or password is incorrect", failDoc.RootElement.GetProperty("message").GetString());
		}

		var ok = await _client.PostAsJsonAsync("/api/User/authenticate", new { login = "demo", password = "demo" });
		Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
		using var doc = JsonDocument.Parse(await ok.Content.ReadAsStringAsync());
		JsonElement root = doc.RootElement;
		Assert.Equal("Jean", root.GetProperty("firstName").GetString());
		Assert.Equal("Test", root.GetProperty("lastName").GetString());
		Assert.Equal("demo", root.GetProperty("login").GetString());
		Assert.Equal(42, root.GetProperty("id").GetInt32());
		Assert.Equal("fake-front-token", root.GetProperty("token").GetString());
		Assert.True(root.GetProperty("isAdmin").GetBoolean());
		Assert.False(root.GetProperty("isNegociateur").GetBoolean());
		Assert.Equal("0601001", root.GetProperty("autoLogin").GetString());
		Assert.Equal(1, root.GetProperty("dispo").GetInt32());
		Assert.True(root.GetProperty("filter").ValueKind is JsonValueKind.True or JsonValueKind.False);
		Assert.Equal("jean@test.local", root.GetProperty("email").GetString());
	}

	[Fact]
	public async Task Refresh_rejects_anonymous_and_renews_session_json()
	{
		using HttpClient client = _factory.CreateClient();
		HttpResponseMessage anon = await client.GetAsync("/api/User/refresh");
		Assert.Equal(HttpStatusCode.Unauthorized, anon.StatusCode);

		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create());
		HttpResponseMessage ok = await client.GetAsync("/api/User/refresh");
		Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
		using var doc = JsonDocument.Parse(await ok.Content.ReadAsStringAsync());
		JsonElement root = doc.RootElement;
		Assert.Equal("Jean", root.GetProperty("firstName").GetString());
		Assert.Equal("Test", root.GetProperty("lastName").GetString());
		Assert.Equal("demo", root.GetProperty("login").GetString());
		Assert.Equal(42, root.GetProperty("id").GetInt32());
		Assert.Equal("fake-refresh-token", root.GetProperty("token").GetString());
		Assert.True(root.GetProperty("isAdmin").GetBoolean());
		Assert.Equal("jean@test.local", root.GetProperty("email").GetString());
	}

	[Theory]
	[InlineData("/api/Test/info")]
	[InlineData("/api/User/refresh")]
	[InlineData("/api/Health/Migrate")]
	[InlineData("/api/PropertyContacts")]
	[InlineData("/api/TypesTaches")]
	[InlineData("/api/Evenements/EncaissementsEnCours")]
	[InlineData("/api/ConseillersPersonnelsEvenements")]
	public async Task Front_unused_or_authed_routes_reject_anonymous(string path)
	{
		HttpResponseMessage res = await _client.GetAsync(path);
		Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
	}

	[Fact]
	public async Task Swagger_stays_available_in_local_safe_mode()
	{
		HttpResponseMessage res = await _client.GetAsync("/swagger/index.html");
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
	}

	[Fact]
	public async Task File_upload_keeps_IsSuccess_shape_and_blocks_traversal()
	{
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create());
		var body = new
		{
			fileName = "../secret.txt",
			fileContent = "Zg=="
		};
		HttpResponseMessage res = await _client.PostAsJsonAsync("/api/File/Upload", body);
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
		Assert.False(doc.RootElement.GetProperty("isSuccess").GetBoolean());
		Assert.True(doc.RootElement.TryGetProperty("errors", out _));
		Assert.False(System.IO.File.Exists(System.IO.Path.Combine(_factory.UploadRoot, "..", "secret.txt")));
	}
}
