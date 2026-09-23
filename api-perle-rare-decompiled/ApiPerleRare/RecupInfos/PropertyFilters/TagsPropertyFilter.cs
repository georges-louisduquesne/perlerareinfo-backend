using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;

namespace ApiPerleRare.RecupInfos.PropertyFilters;

public class TagsPropertyFilter : AbstractPropertyFilter
{
	private class Tag
	{
		public int Value { get; set; }

		public string Flag { get; set; }

		public bool IsRequired => Flag == "O";

		public bool IsAnyOf => Flag == "U";

		public bool IsExcluded => Flag == "N";

		public override string ToString()
		{
			return "\"" + Value + "\":\"" + Flag + "\"";
		}
	}

	private static Regex rgx_v = new Regex("^\"(?<v>\\d+)\":\"(?<res>O|N|U)\"$", RegexOptions.Compiled);

	private static Regex rgx_mode = new Regex("^\"_mode\":\"(?<res>and|or)\"$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

	private readonly Tag[] _tags;

	public TagsPropertyFilter(string value)
	{
		if (value == "")
		{
			return;
		}
		string json = PhpSerializer.ToJson(value);
		if (string.IsNullOrEmpty(json) || json.Length < 2)
		{
			return;
		}
		List<Tag> tags = new List<Tag>();
		bool orMode = false;
		string list = json.Substring(1, json.Length - 2);
		if (string.IsNullOrWhiteSpace(list))
		{
			_tags = tags.ToArray();
			return;
		}
		string[] array = list.Split(',');
		foreach (string raw in array)
		{
			string item = raw.Trim();
			if (item.Length == 0)
			{
				continue;
			}
			Match mode = rgx_mode.Match(item);
			if (mode.Success)
			{
				orMode = string.Equals(mode.Groups["res"].Value, "or", StringComparison.OrdinalIgnoreCase);
				continue;
			}
			Match m = rgx_v.Match(item);
			if (!m.Success)
			{
				continue;
			}
			Tag tag = new Tag();
			tag.Value = int.Parse(m.Groups["v"].Value);
			tag.Flag = m.Groups["res"].Value;
			tags.Add(tag);
		}
		if (orMode)
		{
			foreach (Tag tag in tags)
			{
				if (tag.Flag == "O")
				{
					tag.Flag = "U";
				}
			}
		}
		_tags = tags.ToArray();
	}

	public override void BuildSearch(PropertyFilterSearch search)
	{
		if (_tags != null)
		{
			search.Tags = PhpSerializer.FromJson("{" + string.Join(",", _tags.Select((Tag t) => t.ToString())) + "}");
		}
	}

	public override string GetNotMatchReason(Property property)
	{
		if (_tags == null)
		{
			return null;
		}
		if (property.PListeTags == null)
		{
			return (_tags.Length == 0) ? null : "Bien sans tags";
		}
		Tag[] tags = _tags;
		List<Tag> required = new List<Tag>();
		List<Tag> anyOf = new List<Tag>();
		foreach (Tag t in tags)
		{
			if (t.IsExcluded)
			{
				if (property.PListeTags.IndexOf($"[{t.Value}]") != -1)
				{
					return $"Tag {t.Value} présent et ne devrait pas";
				}
			}
			else if (t.IsAnyOf)
			{
				anyOf.Add(t);
			}
			else if (t.IsRequired)
			{
				required.Add(t);
			}
		}
		foreach (Tag t in required)
		{
			if (property.PListeTags.IndexOf($"[{t.Value}]") == -1)
			{
				return $"Manque le tag {t.Value}";
			}
		}
		if (anyOf.Count != 0)
		{
			foreach (Tag t in anyOf)
			{
				if (property.PListeTags.IndexOf($"[{t.Value}]") != -1)
				{
					return null;
				}
			}
			return "Manque au moins un tag";
		}
		return null;
	}
}
