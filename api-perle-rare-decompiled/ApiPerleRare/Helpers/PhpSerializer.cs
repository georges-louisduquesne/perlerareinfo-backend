using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ApiPerleRare.Helpers;

public class PhpSerializer
{
	private readonly NumberFormatInfo _nfi;

	public Encoding StringEncoding = new UTF8Encoding();

	public bool XmlSafe = false;

	private int _pos;

	private Dictionary<List<object>, bool> _seenArrayLists;

	private Dictionary<Dictionary<object, object>, bool> _seenHashtables;

	public PhpSerializer()
	{
		_nfi = new NumberFormatInfo
		{
			NumberGroupSeparator = "",
			NumberDecimalSeparator = "."
		};
	}

	public string Serialize(object obj)
	{
		_seenArrayLists = new Dictionary<List<object>, bool>();
		_seenHashtables = new Dictionary<Dictionary<object, object>, bool>();
		return InternalSerialize(obj, new StringBuilder()).ToString();
	}

	private StringBuilder InternalSerialize(object obj, StringBuilder sb)
	{
		if (obj == null)
		{
			return sb.Append("N;");
		}
		if (obj is string)
		{
			string str = (string)obj;
			if (XmlSafe)
			{
				str = str.Replace("rn", "n");
				str = str.Replace("r", "n");
			}
			return sb.Append("s:" + StringEncoding.GetByteCount(str) + ":\"" + str + "\";");
		}
		if (obj is bool b)
		{
			return sb.Append("b:" + (b ? "1" : "0") + ";");
		}
		if (obj is int ii)
		{
			return sb.Append("i:" + ii.ToString(_nfi) + ";");
		}
		if (obj is double d)
		{
			return sb.Append("d:" + d.ToString(_nfi) + ";");
		}
		if (obj is List<object> list)
		{
			if (_seenArrayLists.ContainsKey(list))
			{
				return sb.Append("N;");
			}
			_seenArrayLists.Add(list, value: true);
			sb.Append("a:" + list.Count + ":{");
			for (int i = 0; i < list.Count; i++)
			{
				InternalSerialize(i, sb);
				InternalSerialize(list[i], sb);
			}
			sb.Append("}");
			return sb;
		}
		if (obj is Dictionary<object, object> a)
		{
			if (_seenHashtables.ContainsKey(a))
			{
				return sb.Append("N;");
			}
			_seenHashtables.Add(a, value: true);
			sb.Append("a:" + a.Count + ":{");
			foreach (KeyValuePair<object, object> entry in a)
			{
				InternalSerialize(entry.Key, sb);
				InternalSerialize(entry.Value, sb);
			}
			sb.Append("}");
			return sb;
		}
		if (obj is JArray ja)
		{
			List<object> nl = new List<object>(ja);
			return InternalSerialize(nl, sb);
		}
		if (obj is JValue jv)
		{
			return InternalSerialize(jv.Value, sb);
		}
		if (obj is JObject jo)
		{
			Dictionary<object, object> dict = new Dictionary<object, object>();
			foreach (KeyValuePair<string, JToken> val in jo)
			{
				object key = val.Key;
				if (int.TryParse(val.Key, out var iKey))
				{
					key = iKey;
				}
				dict.Add(key, val.Value);
			}
			return InternalSerialize(dict, sb);
		}
		throw new NotImplementedException("Type '" + obj.GetType().FullName + "' non géré");
	}

	public object Deserialize(string str)
	{
		_pos = 0;
		return InternalDeserialize(str);
	}

	private object InternalDeserialize(string str)
	{
		if (str == null || str.Length <= _pos)
		{
			return new object();
		}
		switch (str[_pos])
		{
		case 'N':
			_pos += 2;
			return null;
		case 'b':
		{
			char chBool = str[_pos + 2];
			_pos += 4;
			return chBool == '1';
		}
		case 'i':
		{
			int start = str.IndexOf(":", _pos) + 1;
			int end = str.IndexOf(";", start);
			string stInt = str.Substring(start, end - start);
			_pos += 3 + stInt.Length;
			return int.Parse(stInt, _nfi);
		}
		case 'd':
		{
			int start = str.IndexOf(":", _pos) + 1;
			int end = str.IndexOf(";", start);
			string stDouble = str.Substring(start, end - start);
			_pos += 3 + stDouble.Length;
			return double.Parse(stDouble, _nfi);
		}
		case 's':
		{
			int start = str.IndexOf(":", _pos) + 1;
			int end = str.IndexOf(":", start);
			string stLen = str.Substring(start, end - start);
			int bytelen = int.Parse(stLen);
			int length = bytelen;
			if (end + 2 + length >= str.Length)
			{
				length = str.Length - 2 - end;
			}
			string stRet = str.Substring(end + 2, length);
			while (StringEncoding.GetByteCount(stRet) > bytelen)
			{
				length--;
				stRet = str.Substring(end + 2, length);
			}
			_pos += 6 + stLen.Length + length;
			if (XmlSafe)
			{
				stRet = stRet.Replace("n", "rn");
			}
			return stRet;
		}
		case 'a':
		{
			int start = str.IndexOf(":", _pos) + 1;
			int end = str.IndexOf(":", start);
			string stLen = str.Substring(start, end - start);
			int length = int.Parse(stLen);
			Dictionary<object, object> htRet = new Dictionary<object, object>(length);
			List<object> alRet = new List<object>(length);
			_pos += 4 + stLen.Length;
			for (int i = 0; i < length; i++)
			{
				object key = InternalDeserialize(str);
				object val = InternalDeserialize(str);
				if (alRet != null)
				{
					if (key is int && (int)key == alRet.Count)
					{
						alRet.Add(val);
					}
					else
					{
						alRet = null;
					}
				}
				htRet[key] = val;
			}
			_pos++;
			if (_pos < str.Length && str[_pos] == ';')
			{
				_pos++;
			}
			return (alRet != null) ? ((ICollection)alRet) : ((ICollection)htRet);
		}
		default:
			return "";
		}
	}

	public static string ToJson(string php)
	{
		object obj = new PhpSerializer().Deserialize(php);
		return JsonConvert.SerializeObject(obj);
	}

	public static string FromJson(string json)
	{
		if (json == "{}")
		{
			return "";
		}
		object obj = JsonConvert.DeserializeObject(json);
		return new PhpSerializer().Serialize(obj);
	}
}
