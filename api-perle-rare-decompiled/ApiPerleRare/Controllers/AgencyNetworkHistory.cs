using System;
using System.Text.Json;

namespace ApiPerleRare.Controllers;

public class AgencyActorHistoryItem
{
	public string Action { get; set; }

	public DateTime Date { get; set; }

	public string Actor { get; set; }

	public string Champs { get; set; }
}

public static class AgencyNetworkHistory
{
	public const int NameMax = 50;

	public static uint? NormalizeId(uint? id)
	{
		return id.HasValue && id.Value > 0 ? id : null;
	}

	public static string NormalizeName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return null;
		}
		string text = name.Trim();
		return text.Length <= NameMax ? text : text.Substring(0, NameMax);
	}

	public static bool Changed(uint? beforeId, string beforeName, uint? afterId, string afterName)
	{
		return NormalizeId(beforeId) != NormalizeId(afterId)
			|| !string.Equals(NormalizeName(beforeName), NormalizeName(afterName), StringComparison.Ordinal);
	}

	public static string ChampsJson(uint? beforeId, string beforeName, uint? afterId, string afterName)
	{
		var payload = new
		{
			I_RefGroupe = new { avant = IdToken(beforeId), apres = IdToken(afterId) },
			I_GroupeEventuel = new { avant = NormalizeName(beforeName), apres = NormalizeName(afterName) }
		};
		return JsonSerializer.Serialize(payload);
	}

	private static string IdToken(uint? id)
	{
		uint? normalized = NormalizeId(id);
		return normalized.HasValue ? normalized.Value.ToString() : null;
	}
}
