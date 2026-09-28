using System;
using System.Text.Json;
using ApiPerleRare.Models;

namespace ApiPerleRare.ScheduledTasks;

internal static class TypeformPartialLeadMapper
{
	internal const string BudgetRef = "1b79097e-2487-4141-bca5-6fe90e6674ad";

	internal const string RechercheRef = "49801432-858b-4e38-9d47-e442c2f0a9c8";

	internal const string NomRef = "cecdd17b-bdc5-4281-9271-bc8537f51bb2";

	internal const string TelRef = "201b290b-0b38-4898-8372-2252cd3db179";

	internal static string NormalizePhone(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return null;
		}
		string compact = raw.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
		if (compact.StartsWith("+33", StringComparison.Ordinal))
		{
			string rest = compact.Substring(3);
			if (rest.StartsWith("0", StringComparison.Ordinal))
			{
				return rest;
			}
			return "0" + rest;
		}
		return compact;
	}

	internal static DateTime ToParisWallClock(DateTime utc)
	{
		DateTime specified = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
		DateTime paris = TimeZoneInfo.ConvertTimeFromUtc(specified, ParisTimeZone());
		return DateTime.SpecifyKind(paris, DateTimeKind.Unspecified);
	}

	internal static bool IsIgnoredTest(string nom, string utmSource)
	{
		if (StartsWithTest(nom))
		{
			return true;
		}
		if (string.Equals(utmSource, "xxxxx", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return StartsWithTest(utmSource);
	}

	internal static Formulaires ToFormulaire(JsonElement item, string variant)
	{
		string budget = null;
		string recherche = null;
		string nom = null;
		string tel = null;
		if (item.TryGetProperty("answers", out JsonElement answers) && answers.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement answer in answers.EnumerateArray())
			{
				string fieldRef = FieldRef(answer);
				string value = AnswerValue(answer);
				if (fieldRef == BudgetRef)
				{
					budget = value;
				}
				else if (fieldRef == RechercheRef)
				{
					recherche = value;
				}
				else if (fieldRef == NomRef)
				{
					nom = value;
				}
				else if (fieldRef == TelRef)
				{
					tel = value;
				}
			}
		}
		string phone = NormalizePhone(tel);
		if (string.IsNullOrEmpty(phone))
		{
			return null;
		}
		string source = Hidden(item, "utm_source");
		if (IsIgnoredTest(nom, source))
		{
			return null;
		}
		if (!TryReadUtc(item, "staged_at", out DateTime utc) && !TryReadUtc(item, "landed_at", out utc))
		{
			return null;
		}
		string texte = JoinTexte(budget, recherche);
		return new Formulaires
		{
			FDate = ToParisWallClock(utc),
			FType = 1,
			FStatut = 1,
			FNom = Cut(nom, 100),
			FTel = Cut(phone, 100),
			FSouhaits = "a",
			FTexte = texte,
			FUtmSource = Cut(source, 50),
			FUtmMedium = Cut(Hidden(item, "utm_medium"), 50),
			FUtmCampaign = Cut(Hidden(item, "utm_campaign"), 50),
			FUtmTerm = Cut(Hidden(item, "utm_term"), 100),
			FUtmContent = Cut(Hidden(item, "utm_content"), 100),
			FVariant = Cut(variant, 5)
		};
	}

	private static string JoinTexte(string budget, string recherche)
	{
		budget = string.IsNullOrWhiteSpace(budget) ? null : budget.Trim();
		recherche = string.IsNullOrWhiteSpace(recherche) ? null : recherche.Replace("\r", " ").Replace("\n", " ").Trim();
		if (budget == null)
		{
			return recherche;
		}
		if (recherche == null)
		{
			return budget;
		}
		return budget + " " + recherche;
	}

	private static bool StartsWithTest(string value)
	{
		return !string.IsNullOrWhiteSpace(value) && value.Trim().StartsWith("test", StringComparison.OrdinalIgnoreCase);
	}

	private static string FieldRef(JsonElement answer)
	{
		if (!answer.TryGetProperty("field", out JsonElement field) || !field.TryGetProperty("ref", out JsonElement fieldRef))
		{
			return null;
		}
		return fieldRef.GetString();
	}

	private static string AnswerValue(JsonElement answer)
	{
		if (answer.TryGetProperty("choice", out JsonElement choice) && choice.ValueKind == JsonValueKind.Object && choice.TryGetProperty("label", out JsonElement label))
		{
			return label.GetString();
		}
		if (answer.TryGetProperty("phone_number", out JsonElement phone))
		{
			return phone.GetString();
		}
		if (answer.TryGetProperty("text", out JsonElement text))
		{
			return text.GetString();
		}
		return null;
	}

	private static string Hidden(JsonElement item, string name)
	{
		if (!item.TryGetProperty("hidden", out JsonElement hidden) || hidden.ValueKind != JsonValueKind.Object || !hidden.TryGetProperty(name, out JsonElement value))
		{
			return null;
		}
		string text = value.GetString();
		return string.IsNullOrWhiteSpace(text) ? null : text;
	}

	private static bool TryReadUtc(JsonElement item, string name, out DateTime utc)
	{
		utc = default(DateTime);
		if (!item.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.String)
		{
			return false;
		}
		if (!DateTime.TryParse(value.GetString(), null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out utc))
		{
			return false;
		}
		utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
		return true;
	}

	private static string Cut(string value, int max)
	{
		if (string.IsNullOrEmpty(value))
		{
			return value;
		}
		return value.Length <= max ? value : value.Substring(0, max);
	}

	private static TimeZoneInfo ParisTimeZone()
	{
		if (TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Paris", out TimeZoneInfo paris))
		{
			return paris;
		}
		return TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
	}
}
