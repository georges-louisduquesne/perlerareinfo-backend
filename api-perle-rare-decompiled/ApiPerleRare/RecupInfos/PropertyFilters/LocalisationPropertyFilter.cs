using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;

namespace ApiPerleRare.RecupInfos.PropertyFilters;

public class LocalisationPropertyFilter : AbstractPropertyFilter
{
	public class LocalisationInfo
	{
		public string[] CP { get; set; }

		[JsonPropertyName("quartiers")]
		public JsonNode Quartiers { get; set; }

		[JsonPropertyName("infosCP")]
		public JsonNode InfosCP { get; set; }
	}

	public class LocalisationInfoQuartier
	{
		public string CP { get; set; }

		public string[] Quartiers { get; set; }
	}

	public class LocalisationInfoQuartiers
	{
		[JsonPropertyName("75001")]
		public string[] CP75001 { get; set; }

		[JsonPropertyName("75002")]
		public string[] CP75002 { get; set; }

		[JsonPropertyName("75003")]
		public string[] CP75003 { get; set; }

		[JsonPropertyName("75004")]
		public string[] CP75004 { get; set; }

		[JsonPropertyName("75005")]
		public string[] CP75005 { get; set; }

		[JsonPropertyName("75006")]
		public string[] CP75006 { get; set; }

		[JsonPropertyName("75007")]
		public string[] CP75007 { get; set; }

		[JsonPropertyName("75008")]
		public string[] CP75008 { get; set; }

		[JsonPropertyName("75009")]
		public string[] CP75009 { get; set; }

		[JsonPropertyName("75010")]
		public string[] CP75010 { get; set; }

		[JsonPropertyName("75011")]
		public string[] CP75011 { get; set; }

		[JsonPropertyName("75012")]
		public string[] CP75012 { get; set; }

		[JsonPropertyName("75013")]
		public string[] CP75013 { get; set; }

		[JsonPropertyName("75014")]
		public string[] CP75014 { get; set; }

		[JsonPropertyName("75015")]
		public string[] CP75015 { get; set; }

		[JsonPropertyName("75016")]
		public string[] CP75016 { get; set; }

		[JsonPropertyName("75017")]
		public string[] CP75017 { get; set; }

		[JsonPropertyName("75018")]
		public string[] CP75018 { get; set; }

		[JsonPropertyName("75019")]
		public string[] CP75019 { get; set; }

		[JsonPropertyName("75020")]
		public string[] CP75020 { get; set; }

		[JsonPropertyName("92100")]
		public string[] CP92100 { get; set; }

		[JsonPropertyName("92130")]
		public string[] CP92130 { get; set; }

		[JsonPropertyName("92170")]
		public string[] CP92170 { get; set; }

		[JsonPropertyName("92200")]
		public string[] CP92200 { get; set; }

		[JsonPropertyName("92300")]
		public string[] CP92300 { get; set; }

		[JsonPropertyName("92400")]
		public string[] CP92400 { get; set; }

		[JsonPropertyName("92800")]
		public string[] CP92800 { get; set; }

		[JsonPropertyName("94160")]
		public string[] CP94160 { get; set; }

		[JsonPropertyName("94220")]
		public string[] CP94220 { get; set; }

		[JsonPropertyName("94300")]
		public string[] CP94300 { get; set; }
	}

	private readonly LocalisationInfo _infos;

	private readonly LocalisationInfoQuartier[] _quartiers;

	public LocalisationPropertyFilter(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return;
		}
		string json = PhpSerializer.ToJson(value);
		_infos = JsonSerializer.Deserialize<LocalisationInfo>(json);
		if (_infos.Quartiers == null)
		{
			return;
		}
		List<LocalisationInfoQuartier> list = new List<LocalisationInfoQuartier>();
		foreach (KeyValuePair<string, JsonNode> child in _infos.Quartiers.AsObject())
		{
			string[] quartiers = ((IEnumerable<JsonNode>)child.Value.AsArray()).Select((JsonNode v) => (string)(JsonNode)(object)v.AsValue()).ToArray();
			LocalisationInfoQuartier quartier = new LocalisationInfoQuartier
			{
				CP = child.Key,
				Quartiers = quartiers
			};
			list.Add(quartier);
		}
		_quartiers = list.ToArray();
	}

	public override void BuildSearch(PropertyFilterSearch search)
	{
		if (_infos != null)
		{
			string json = JsonSerializer.Serialize(_infos, new JsonSerializerOptions
			{
				DefaultIgnoreCondition = (JsonIgnoreCondition)3
			});
			search.Localisation = PhpSerializer.FromJson(json);
		}
	}

	public override string GetNotMatchReason(Property property)
	{
		if (_infos == null)
		{
			return "Infos de localisation absentes !";
		}
		if (_infos.CP != null && _infos.CP.Length != 0 && _infos.CP.Contains(property.PCp))
		{
			return null;
		}
		if (_quartiers != null)
		{
			LocalisationInfoQuartier[] quartiers = _quartiers;
			foreach (LocalisationInfoQuartier q in quartiers)
			{
				if (property.PCp != q.CP)
				{
					continue;
				}
				string[] quartiers2 = q.Quartiers;
				foreach (string q2 in quartiers2)
				{
					if (q2 == "NC" && string.IsNullOrEmpty(property.PQuartier2))
					{
						return null;
					}
					if (property.PQuartier2 != null && property.PQuartier2.Contains("[" + q2 + "]"))
					{
						return null;
					}
				}
			}
		}
		return "Mauvaise localisation";
	}
}
