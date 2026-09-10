#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace ApiPerleRare.Helpers;

public class EncodingHelper
{
	private class Node
	{
		public const char NotSet = '\0';

		public readonly Dictionary<char, Node> Children = new Dictionary<char, Node>();

		public readonly char CorrectCharacter;

		public readonly string Key;

		public Node(string key, char correctChar = '\0')
		{
			Key = key;
			CorrectCharacter = correctChar;
		}

		public void Set(string toRecognized, char correctChar)
		{
			Node node = this;
			for (int i = 0; i < toRecognized.Length; i++)
			{
				char ch = toRecognized[i];
				if (node.Children.ContainsKey(ch))
				{
					if (node.CorrectCharacter != 0)
					{
						throw new Exception("Ne devrait pas arriver");
					}
					node = node.Children[ch];
				}
				else if (i == toRecognized.Length - 1)
				{
					node.Children.Add(ch, new Node(toRecognized, correctChar));
				}
				else
				{
					Node newNode = new Node(toRecognized.Substring(0, i + 1));
					node.Children.Add(ch, newNode);
					node = newNode;
				}
			}
		}
	}

	private static readonly Node _automate;

	private static readonly char[] _firstLetters;

	private static Dictionary<Type, PropertyInfo[]> cache_stringProperties;

	public static List<string> Forbidden { get; }

	static EncodingHelper()
	{
		_automate = new Node("ROOT");
		Forbidden = new List<string>();
		cache_stringProperties = new Dictionary<Type, PropertyInfo[]>();
		Set("Ã©", "é");
		Set("Ã§", "ç");
		Set("Ã\u00a8", "è");
		Set("Â°", "°");
		Set("Ã^", "È");
		Set("Ã¢", "â");
		Set("Ã\u00b4", "ô");
		Set("Ã¶", "ô");
		Set("Ã‰", "É");
		Set("Ãˆ", "È");
		Set("Ã‡", "Ç");
		Set("Ã®", "î");
		Set("Ã«", "ë");
		Set("Ã‹", "Ë");
		Set("Ãª", "ê");
		Set("ÃŠ", "ê");
		Set("Ã\u00a0", "à");
		Set("â€“", "à");
		Set("Ã€", "À");
		Set("Â²", "²");
		Set("Ã¹", "ù");
		Set("Ã»", "û");
		Set("Ã\u00af", "ï");
		Set("Ã\u008f", "ï");
		Set("â‚¬", "€");
		Set("â€™", "'");
		Set("Â€™", "'");
		Set("â€Ž", " ");
		Set("â€¢", "-");
		_firstLetters = _automate.Children.Keys.ToArray();
	}

	private static void Set(string before, string after)
	{
		_automate.Set(before, after.Single());
		Forbidden.Add(before);
	}

	public static string FixEncoding(string s)
	{
		if (s == null || s.Length == 0)
		{
			return s;
		}
		// Fast path: most CRM strings are already correct UTF-8.
		if (s.IndexOfAny(_firstLetters) < 0)
		{
			return s;
		}
		int pos = 0;
		while (true)
		{
			int i = s.IndexOfAny(_firstLetters, pos);
			if (i == -1)
			{
				break;
			}
			Node n = _automate;
			for (int j = i; j < s.Length; j++)
			{
				char ch = s[j];
				if (!n.Children.ContainsKey(ch))
				{
					Trace.WriteLine($"Encoding manquant:{n.Key}{ch} pour {s}");
					break;
				}
				n = n.Children[ch];
				if (n.CorrectCharacter != 0)
				{
					s = s.Replace(n.Key, n.CorrectCharacter.ToString());
					break;
				}
			}
			pos = i + 1;
		}
		return s;
	}

	public static bool FixEncodingInStringProperties(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		bool hasChanged = false;
		PropertyInfo[] stringProperties = GetStringProperties(obj.GetType());
		foreach (PropertyInfo prop in stringProperties)
		{
			string val = (string)prop.GetValue(obj);
			string newVal = FixEncoding(val);
			if (!(newVal == val))
			{
				prop.SetValue(obj, newVal);
				hasChanged = true;
			}
		}
		return hasChanged;
	}

	public static void FixEncodingInEnumerable<T>(IEnumerable<T> items)
	{
		if (items == null)
		{
			return;
		}
		PropertyInfo[] props = GetStringProperties(typeof(T));
		foreach (T item in items)
		{
			PropertyInfo[] array = props;
			foreach (PropertyInfo prop in array)
			{
				string val = (string)prop.GetValue(item);
				string newVal = FixEncoding(val);
				if (!(newVal == val))
				{
					prop.SetValue(item, newVal);
				}
			}
		}
	}

	public static PropertyInfo[] GetStringProperties(Type type)
	{
		if (!cache_stringProperties.TryGetValue(type, out var props))
		{
			props = (from p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty)
				where p.PropertyType == typeof(string)
				select p).ToArray();
			cache_stringProperties.Add(type, props);
		}
		return props;
	}
}
