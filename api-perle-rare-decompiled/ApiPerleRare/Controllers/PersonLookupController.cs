using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[EnableCors]
[ApiController]
[Authorize]
public class PersonLookupController : ControllerBase
{
	private readonly IConfiguration _configuration;

	public PersonLookupController(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public class PersonLookupRequest
	{
		public string Prenom { get; set; }

		public string Nom { get; set; }
	}

	[HttpPost("identify")]
	public async Task<IActionResult> Identify([FromBody] PersonLookupRequest body, CancellationToken cancellationToken)
	{
		var prenom = Clean(body?.Prenom);
		var nom = Clean(StripTrailingNumber(body?.Nom));
		if (PersonLookupReader.NameTokens(prenom + " " + nom).Count < 2)
		{
			return Ok(PersonLookupReader.Unavailable());
		}
		var token = ReadApifyToken(_configuration);
		if (string.IsNullOrWhiteSpace(token))
		{
			Console.WriteLine("PersonLookup: jeton Apify absent.");
			return Ok(PersonLookupReader.Unavailable());
		}
		using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(55) };
		try
		{
			var lightTask = RunActor(http, token, "automly~linkedin-people-search-scraper", new
			{
				firstName = prenom,
				lastName = nom,
				maxResults = 12,
				fullProfiles = false,
			}, 22, 512, cancellationToken);
			var googleTask = RunActor(http, token, "apify~google-search-scraper", new
			{
				queries = (prenom + " " + nom).Trim(),
				maxPagesPerQuery = 1,
				countryCode = "fr",
				languageCode = "fr",
				searchLanguage = "fr",
				saveHtml = false,
				saveHtmlToKeyValueStore = false,
			}, 20, 1024, cancellationToken);

			var people = PersonLookupReader.ReadLinkedIn(await lightTask);
			var exact = people.Where(person => PersonLookupReader.IsExactPersonName(prenom, nom, person.Name)).ToList();
			if (exact.Count == 1)
			{
				try
				{
					var fullJson = await RunActor(http, token, "automly~linkedin-people-search-scraper", new
					{
						firstName = prenom,
						lastName = nom,
						maxResults = 2,
						fullProfiles = true,
					}, 20, 512, cancellationToken);
					people = PersonLookupReader.Merge(people, PersonLookupReader.ReadLinkedIn(fullJson));
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch
				{
					// Le décompte reste valable sans la photo.
				}
			}

			string googleJson = null;
			try
			{
				googleJson = await googleTask;
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch
			{
				googleJson = null;
			}

			return Ok(new
			{
				available = true,
				aiSummary = PersonLookupReader.ReadAiSummary(googleJson),
				linkedin = people,
				web = googleJson == null ? new List<PublicWebHit>() : PersonLookupReader.ReadGoogle(googleJson),
			});
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			Console.WriteLine("PersonLookup: " + ex.Message);
			return Ok(PersonLookupReader.Unavailable());
		}
	}

	private static async Task<string> RunActor(HttpClient http, string token, string actorId, object input, int timeoutSecs, int memory, CancellationToken cancellationToken)
	{
		var url = "https://api.apify.com/v2/acts/" + actorId + "/run-sync-get-dataset-items?timeout=" + timeoutSecs + "&memory=" + memory;
		using var request = new HttpRequestMessage(HttpMethod.Post, url);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
		request.Content = JsonContent.Create(input);
		using var response = await http.SendAsync(request, cancellationToken);
		var body = await response.Content.ReadAsStringAsync(cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException("Recherche publique indisponible (" + (int)response.StatusCode + ").");
		}
		return body;
	}

	internal static string ReadApifyToken(IConfiguration configuration)
	{
		var configured = configuration?["AppSettings:ApifyToken"];
		if (!string.IsNullOrWhiteSpace(configured))
		{
			return configured.Trim();
		}
		var env = Environment.GetEnvironmentVariable("APIFY_TOKEN");
		if (!string.IsNullOrWhiteSpace(env))
		{
			return env.Trim();
		}
		try
		{
			var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".apify", "auth.json");
			if (System.IO.File.Exists(path))
			{
				var json = System.IO.File.ReadAllText(path);
				var fromFile = TokenFromAuthJson(json);
				if (!string.IsNullOrWhiteSpace(fromFile))
				{
					return fromFile;
				}
			}
		}
		catch
		{
			// Fichier illisible : le trousseau Windows peut encore avoir le jeton.
		}
		return ReadKeyringToken();
	}

	private static string DecodeCredential(byte[] bytes)
	{
		if (bytes.Length >= 4 && bytes.Length % 2 == 0 && bytes[1] == 0)
		{
			var utf16 = Encoding.Unicode.GetString(bytes).Trim('\0', ' ', '\r', '\n');
			if (!string.IsNullOrWhiteSpace(utf16))
			{
				return utf16;
			}
		}
		return Encoding.UTF8.GetString(bytes).Trim('\0', ' ', '\r', '\n');
	}

	internal static string TokenFromAuthJson(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}
		try
		{
			using var doc = JsonDocument.Parse(json);
			return TokenFromElement(doc.RootElement);
		}
		catch
		{
			return null;
		}
	}

	private static string TokenFromElement(JsonElement element)
	{
		if (element.ValueKind != JsonValueKind.Object)
		{
			return null;
		}
		if (element.TryGetProperty("token", out var token) && token.ValueKind == JsonValueKind.String)
		{
			var value = token.GetString();
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value.Trim();
			}
		}
		if (element.TryGetProperty("profiles", out var profiles) && profiles.ValueKind == JsonValueKind.Object)
		{
			foreach (var profile in profiles.EnumerateObject())
			{
				var found = TokenFromElement(profile.Value);
				if (!string.IsNullOrWhiteSpace(found))
				{
					return found;
				}
			}
		}
		return null;
	}

	private static string ReadKeyringToken()
	{
		if (!OperatingSystem.IsWindows())
		{
			return null;
		}
		string[] targets =
		{
			"token.com.apify.cli",
			"com.apify.cli.token",
			"com.apify.cli/token",
			"com.apify.cli:token",
		};
		foreach (var target in targets)
		{
			var value = ReadGenericCredential(target);
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value.Trim();
			}
		}
		return null;
	}

	private static string ReadGenericCredential(string target)
	{
		if (!CredRead(target, 1, 0, out var credential))
		{
			return null;
		}
		try
		{
			var cred = Marshal.PtrToStructure<Credential>(credential);
			if (cred.CredentialBlob == IntPtr.Zero || cred.CredentialBlobSize <= 0)
			{
				return null;
			}
			var bytes = new byte[cred.CredentialBlobSize];
			Marshal.Copy(cred.CredentialBlob, bytes, 0, bytes.Length);
			var text = DecodeCredential(bytes);
			return string.IsNullOrWhiteSpace(text) ? null : text;
		}
		finally
		{
			CredFree(credential);
		}
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct Credential
	{
		public int Flags;
		public int Type;
		public IntPtr TargetName;
		public IntPtr Comment;
		public long LastWritten;
		public int CredentialBlobSize;
		public IntPtr CredentialBlob;
		public int Persist;
		public int AttributeCount;
		public IntPtr Attributes;
		public IntPtr TargetAlias;
		public IntPtr UserName;
	}

	[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credential);

	[DllImport("advapi32.dll")]
	private static extern void CredFree(IntPtr credential);

	private static string Clean(string value)
	{
		var text = Regex.Replace((value ?? "").Trim(), "\\s+", " ");
		return text.Length <= 80 ? text : text.Substring(0, 80).Trim();
	}

	private static string StripTrailingNumber(string value)
	{
		var text = Clean(value);
		var match = Regex.Match(text, "^(.*\\D)\\s*(\\d+)$");
		if (!match.Success)
		{
			return text;
		}
		var head = match.Groups[1].Value.Trim();
		return head.Length == 0 ? text : head;
	}
}
