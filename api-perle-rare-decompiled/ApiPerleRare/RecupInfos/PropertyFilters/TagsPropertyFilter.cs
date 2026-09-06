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

		public bool IsChecked { get; set; }

		public override string ToString()
		{
			return "\"" + Value + "\":\"" + (IsChecked ? "O" : "N") + "\"";
		}
	}

	private static Regex rgx_v = new Regex("^\"(?<v>\\d+)\":\"(?<res>O|N)\"$", RegexOptions.Compiled);

	private readonly Tag[] _tags;

	public TagsPropertyFilter(string value)
	{
		if (value == "")
		{
			return;
		}
		string json = PhpSerializer.ToJson(value);
		List<Tag> tags = new List<Tag>();
		string list = json.Substring(1, json.Length - 2);
		string[] array = list.Split(',');
		foreach (string item in array)
		{
			Tag tag = new Tag();
			Match m = rgx_v.Match(item);
			if (!m.Success)
			{
				throw new Exception("Non reconnu");
			}
			tag.Value = int.Parse(m.Groups["v"].Value);
			tag.IsChecked = m.Groups["res"].Value == "O";
			tags.Add(tag);
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
		foreach (Tag t in tags)
		{
			if (t.IsChecked && property.PListeTags.IndexOf($"[{t.Value}]") == -1)
			{
				return $"Manque le tag {t.Value}";
			}
			if (!t.IsChecked && property.PListeTags.IndexOf($"[{t.Value}]") != -1)
			{
				return $"Tag {t.Value} présent et ne devrait pas";
			}
		}
		return null;
	}
}
