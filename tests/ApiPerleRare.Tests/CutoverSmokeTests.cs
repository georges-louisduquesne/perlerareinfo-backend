using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ApiPerleRare.Tests;

public class CutoverSmokeTests : IClassFixture<ApiFactory>
{
	private readonly ApiFactory _factory;

	public CutoverSmokeTests(ApiFactory factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task Login_then_refresh_then_front_get_routes()
	{
		using HttpClient client = _factory.CreateClient();
		string access = await ContractIntegrationTests.LoginToken(client);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);

		HttpResponseMessage refresh = await client.GetAsync("/api/User/refresh");
		Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
		using (var refreshDoc = JsonDocument.Parse(await refresh.Content.ReadAsStringAsync()))
		{
			string renewed = refreshDoc.RootElement.GetProperty("token").GetString();
			Assert.False(string.IsNullOrWhiteSpace(renewed));
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", renewed);
		}

		await AssertCatalog(client, "/api/TypesTaches?take=5");
		await AssertCatalog(client, "/api/TypesMails?take=5");

		HttpResponseMessage accueil = await client.GetAsync("/api/ContactsRecherches/Accueil?take=2&skip=0");
		Assert.Equal(HttpStatusCode.OK, accueil.StatusCode);
		using (var accueilDoc = JsonDocument.Parse(await accueil.Content.ReadAsStringAsync()))
		{
			JsonElement row = accueilDoc.RootElement[0];
			Assert.Equal(7, row.GetProperty("cRefContact").GetInt32());
			Assert.Equal("Martin", row.GetProperty("cNomFamille").GetString());
			Assert.Equal("Léa", row.GetProperty("cPrenom").GetString());
		}

		HttpResponseMessage list = await client.GetAsync("/api/ContactsRecherches?take=2");
		Assert.Equal(HttpStatusCode.OK, list.StatusCode);
		using (var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync()))
		{
			Assert.Equal(7, listDoc.RootElement[0].GetProperty("cRefContact").GetInt32());
		}

		HttpResponseMessage count = await client.GetAsync("/api/ContactsRecherches/Count");
		Assert.Equal(HttpStatusCode.OK, count.StatusCode);
		Assert.Equal("1", (await count.Content.ReadAsStringAsync()).Trim());

		HttpResponseMessage taches = await client.GetAsync("/api/Taches?take=2");
		Assert.Equal(HttpStatusCode.OK, taches.StatusCode);
		using (var tachesDoc = JsonDocument.Parse(await taches.Content.ReadAsStringAsync()))
		{
			Assert.Equal(3, tachesDoc.RootElement[0].GetProperty("tRef").GetInt32());
			Assert.Equal("Appel", tachesDoc.RootElement[0].GetProperty("tType").GetString());
		}

		string relative = "docs/cutover-smoke.txt";
		string payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("ok"));
		HttpResponseMessage upload = await client.PostAsJsonAsync("/api/File/Upload", new { fileName = relative, fileContent = payload });
		Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
		using (var uploadDoc = JsonDocument.Parse(await upload.Content.ReadAsStringAsync()))
		{
			Assert.True(uploadDoc.RootElement.GetProperty("isSuccess").GetBoolean());
		}

		HttpResponseMessage download = await client.PostAsJsonAsync("/api/File/Download", new { fileName = relative });
		Assert.Equal(HttpStatusCode.OK, download.StatusCode);
		using var downloadDoc = JsonDocument.Parse(await download.Content.ReadAsStringAsync());
		Assert.True(downloadDoc.RootElement.GetProperty("isSuccess").GetBoolean());
		Assert.Equal(payload, downloadDoc.RootElement.GetProperty("fileContent").GetString());
	}

	private static async Task AssertCatalog(HttpClient client, string path)
	{
		HttpResponseMessage res = await client.GetAsync(path);
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
		JsonElement root = doc.RootElement;
		Assert.Equal(1, root.GetProperty("total").GetInt32());
		Assert.Equal(JsonValueKind.Array, root.GetProperty("items").ValueKind);
		Assert.Equal(1, root.GetProperty("items").GetArrayLength());
	}
}
