using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ApiPerleRare.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiPerleRare.ScheduledTasks;

public class TypeformPartialLeadsScheduledTask : AbstractScheduledTask
{
	private static readonly (string Variant, string FormId)[] Forms = new (string, string)[2]
	{
		("01", "jnbqHWAh"),
		("02", "GTvB1tv3")
	};

	private readonly IServiceScopeFactory _serviceScopeFactory;

	private readonly IConfiguration _configuration;

	private readonly ILogger<TypeformPartialLeadsScheduledTask> _logger;

	public TypeformPartialLeadsScheduledTask(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration, ILogger<TypeformPartialLeadsScheduledTask> logger)
	{
		_serviceScopeFactory = serviceScopeFactory;
		_configuration = configuration;
		_logger = logger;
	}

	public override string Execute(DateTime lastExecution)
	{
		DateTime now = DateTime.Now;
		if (lastExecution.Date == now.Date && lastExecution.Hour == now.Hour)
		{
			return "-";
		}
		string token = _configuration["AppSettings:TypeformToken"];
		if (string.IsNullOrWhiteSpace(token))
		{
			token = Environment.GetEnvironmentVariable("TYPEFORM_TOKEN");
		}
		if (string.IsNullOrWhiteSpace(token))
		{
			return "TypeformToken absent";
		}
		int lookbackHours = 2;
		if (int.TryParse(_configuration["AppSettings:TypeformPartialLookbackHours"], out int parsed) && parsed > 0 && parsed <= 48)
		{
			lookbackHours = parsed;
		}
		DateTime sinceUtc = DateTime.UtcNow.AddHours(-lookbackHours);
		StringBuilder log = new StringBuilder();
		using HttpClient http = new HttpClient();
		http.Timeout = TimeSpan.FromSeconds(30.0);
		http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
		using IServiceScope scope = _serviceScopeFactory.CreateScope();
		using ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		foreach ((string variant, string formId) in Forms)
		{
			try
			{
				int inserted = ImportForm(http, db, formId, variant, sinceUtc);
				log.Append(variant).Append(" +").Append(inserted).Append(' ');
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Import Typeform {Variant} échoué", variant);
				log.Append(variant).Append(" erreur: ").Append(ex.Message).Append(' ');
			}
		}
		return log.ToString().Trim();
	}

	private static int ImportForm(HttpClient http, ApplicationDbContext db, string formId, string variant, DateTime sinceUtc)
	{
		string since = Uri.EscapeDataString(sinceUtc.ToString("yyyy-MM-ddTHH:mm:ss"));
		string url = "https://api.typeform.com/forms/" + formId + "/responses?page_size=200&response_type=partial&since=" + since;
		using HttpResponseMessage response = http.GetAsync(url).GetAwaiter().GetResult();
		string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException("HTTP " + (int)response.StatusCode);
		}
		using JsonDocument document = JsonDocument.Parse(body);
		if (!document.RootElement.TryGetProperty("items", out JsonElement items) || items.ValueKind != JsonValueKind.Array)
		{
			return 0;
		}
		int inserted = 0;
		foreach (JsonElement item in items.EnumerateArray())
		{
			Formulaires form = TypeformPartialLeadMapper.ToFormulaire(item, variant);
			if (form == null)
			{
				continue;
			}
			bool exists = db.Formulaires.Any((Formulaires f) => f.FVariant == form.FVariant && f.FTel == form.FTel && f.FDate == form.FDate);
			if (exists)
			{
				continue;
			}
			db.Formulaires.Add(form);
			inserted++;
		}
		if (inserted > 0)
		{
			db.SaveChanges();
		}
		return inserted;
	}
}
