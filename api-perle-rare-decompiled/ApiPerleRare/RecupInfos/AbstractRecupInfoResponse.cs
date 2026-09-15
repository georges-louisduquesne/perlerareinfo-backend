#define TRACE
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;

namespace ApiPerleRare.RecupInfos;

public abstract class AbstractRecupInfoResponse
{
	public string Error { get; set; }

	public bool Canceled { get; set; }

	internal long Token { get; set; }

	internal uint ContactRef { get; set; }

	public string[] Infos { get; set; }

	public List<RecupInfoPerf> Perfs { get; set; } = new List<RecupInfoPerf>();

	public abstract Task AddAsync(MySqlConnection c, IMemoryCache memoryCache, string field, string sql, int valueFieldIndex = 0, int countFieldIndex = 1, bool isBoolean = false);

	public abstract void Add(MySqlConnection c, IMemoryCache memoryCache, string field, string sql, int valueFieldIndex = 0, int countFieldIndex = 1, bool isBoolean = false);

	public abstract void AdjustTags();

	public abstract void AdjustQuartiers();

	public abstract void SumIntValues(string field, int maxValue);
}
public abstract class AbstractRecupInfoResponse<TCount> : AbstractRecupInfoResponse where TCount : RecupInfoCount
{
	private static Regex rgx_tag = new Regex("^(?:\\[(?<id>\\d+)\\])+$", RegexOptions.Compiled);

	internal static ConcurrentDictionary<uint, long> SearchTokens { get; } = new ConcurrentDictionary<uint, long>();

	public List<TCount> Counts { get; set; } = new List<TCount>();

	public override async Task AddAsync(MySqlConnection c, IMemoryCache memoryCache, string field, string sql, int valueFieldIndex = 0, int countFieldIndex = 1, bool isBoolean = false)
	{
		if (base.Infos != null && base.Infos.Length != 0 && !base.Infos.Contains(field))
		{
			return;
		}
		Log(field, sql);
		if (base.ContactRef != 0 && AbstractRecupInfoResponse<RecupInfoCount>.SearchTokens[base.ContactRef] != base.Token)
		{
			base.Canceled = true;
			if (RecupInfoSearcher.Debug)
			{
				Console.ForegroundColor = ConsoleColor.DarkMagenta;
				Console.WriteLine("IGNORE car autre appel");
				Console.WriteLine("");
				Console.ResetColor();
			}
			return;
		}
		try
		{
			TCount[] counts = await memoryCache.GetOrCreateAsync(sql, delegate(ICacheEntry ce)
			{
				ce.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1.0);
				return GetCountsAsync(c, field, sql, valueFieldIndex, countFieldIndex, isBoolean);
			});
			if (counts == null)
			{
				return;
			}
			counts = counts.Where((TCount val) => val != null).ToArray();
			lock (Counts)
			{
				Counts.AddRange(counts);
			}
			Log(field, $"Add to Counts : {counts.Length}");
		}
		catch (Exception ex)
		{
			Log(field, "facet skipped: " + ex.Message);
		}
	}

	public override void Add(MySqlConnection c, IMemoryCache memoryCache, string field, string sql, int valueFieldIndex = 0, int countFieldIndex = 1, bool isBoolean = false)
	{
		if (base.Infos != null && base.Infos.Length != 0 && !base.Infos.Contains(field))
		{
			return;
		}
		Log(field, sql);
		if (base.ContactRef != 0 && AbstractRecupInfoResponse<RecupInfoCount>.SearchTokens[base.ContactRef] != base.Token)
		{
			base.Canceled = true;
			if (RecupInfoSearcher.Debug)
			{
				Console.ForegroundColor = ConsoleColor.DarkMagenta;
				Console.WriteLine("IGNORE car autre appel");
				Console.WriteLine("");
				Console.ResetColor();
			}
			return;
		}
		try
		{
			TCount[] counts = memoryCache.GetOrCreate(sql, delegate(ICacheEntry ce)
			{
				ce.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1.0);
				return GetCounts(c, field, sql, valueFieldIndex, countFieldIndex, isBoolean);
			});
			if (counts == null)
			{
				return;
			}
			counts = counts.Where((TCount val) => val != null).ToArray();
			lock (Counts)
			{
				Counts.AddRange(counts);
			}
			Log(field, $"Add to Counts : {counts.Length}");
		}
		catch (Exception ex)
		{
			Log(field, "facet skipped: " + ex.Message);
		}
	}

	protected virtual string AdjustSql(string sql)
	{
		return sql;
	}

	private static string ReadFacetValue(MySqlDataReader reader, bool isBoolean)
	{
		if (reader.IsDBNull(0))
		{
			return isBoolean ? "0" : "NC";
		}
		object raw = reader.GetValue(0);
		if (isBoolean)
		{
			if (raw is bool flag)
			{
				return flag ? "1" : "0";
			}
			try
			{
				return Convert.ToInt64(raw) != 0 ? "1" : "0";
			}
			catch
			{
				string flagText = Convert.ToString(raw) ?? "";
				return flagText == "1" || flagText.Equals("true", StringComparison.OrdinalIgnoreCase) ? "1" : "0";
			}
		}
		string text = Convert.ToString(raw);
		return string.IsNullOrEmpty(text) ? "NC" : text;
	}

	public TCount[] GetCounts(MySqlConnection c, string field, string sql, int valueFieldIndex = 0, int countFieldIndex = 1, bool isBoolean = false)
	{
		try
		{
			if (valueFieldIndex != 0 || countFieldIndex != 1)
			{
				throw new Exception("Pas bon !");
			}
			DateTime start = DateTime.Now;
			List<TCount> list = new List<TCount>();
			using (MySqlCommand cmd = c.CreateCommand())
			{
				cmd.CommandText = AdjustSql(sql);
				using MySqlDataReader reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					AddInfoFromDataReader(list, reader, field, ReadFacetValue(reader, isBoolean));
				}
			}
			TimeSpan duration = DateTime.Now - start;
			base.Perfs.Add(new RecupInfoPerf
			{
				Start = start,
				Duration = duration.TotalSeconds,
				Field = field,
				Sql = sql
			});
			return list.ToArray();
		}
		catch (Exception innerException)
		{
			throw new Exception("Erreur avec SQL " + sql, innerException);
		}
	}

	public async Task<TCount[]> GetCountsAsync(MySqlConnection c, string field, string sql, int valueFieldIndex = 0, int countFieldIndex = 1, bool isBoolean = false)
	{
		try
		{
			if (valueFieldIndex != 0 || countFieldIndex != 1)
			{
				throw new Exception("Pas bon !");
			}
			DateTime start = DateTime.Now;
			List<TCount> list = new List<TCount>();
			using (MySqlConnection c2 = c.Clone())
			{
				await c2.OpenAsync();
				using MySqlCommand cmd = c2.CreateCommand();
				cmd.CommandText = AdjustSql(sql);
				using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
				while (await reader.ReadAsync())
				{
					AddInfoFromDataReader(list, reader, field, ReadFacetValue(reader, isBoolean));
				}
			}
			TimeSpan duration = DateTime.Now - start;
			base.Perfs.Add(new RecupInfoPerf
			{
				Start = start,
				Duration = duration.TotalSeconds,
				Field = field,
				Sql = sql
			});
			return list.ToArray();
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			throw new Exception("Erreur avec SQL " + sql, ex2);
		}
	}

	public static void Log(string field, string sql)
	{
		if (RecupInfoSearcher.Debug)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("**** " + field + " ****");
			Console.WriteLine(sql ?? "");
			Console.WriteLine("");
			Console.ResetColor();
		}
	}

	public override void SumIntValues(string field, int maxValue)
	{
		List<TCount> ToRemove = new List<TCount>();
		TCount sum = null;
		lock (Counts)
		{
			foreach (TCount c in Counts)
			{
				if (!(c.Field != field) && int.TryParse(c.Value, out var v) && v >= maxValue)
				{
					if (sum == null)
					{
						sum = Clone(c);
						sum.Value = $"{maxValue}+";
					}
					else
					{
						Merge(sum, c);
					}
					ToRemove.Add(c);
				}
			}
			if (sum != null)
			{
				Counts.Add(sum);
			}
			Trace.WriteLine("toto");
			foreach (TCount c2 in ToRemove)
			{
				Counts.Remove(c2);
			}
		}
	}

	public override void AdjustTags()
	{
		Dictionary<int, TCount> nb = new Dictionary<int, TCount>();
		lock (Counts)
		{
			foreach (TCount t in Counts)
			{
				if (t.Field != "Tag" || string.IsNullOrEmpty(t.Value))
				{
					continue;
				}
				Match m = rgx_tag.Match(t.Value);
				if (!m.Success)
				{
					continue;
				}
				foreach (Capture c in m.Groups["id"].Captures)
				{
					int tagId = int.Parse(c.Value);
					if (!nb.TryGetValue(tagId, out var count))
					{
						count = Clone(t);
						count.Value = $"[{tagId}]";
						nb.Add(tagId, count);
					}
					else
					{
						Merge(count, t);
					}
				}
			}
			Counts.RemoveAll((TCount val) => val.Field == "Tag");
			foreach (KeyValuePair<int, TCount> c2 in nb.OrderBy((KeyValuePair<int, TCount> n) => n.Key))
			{
				Counts.Add(c2.Value);
			}
		}
	}

	public override void AdjustQuartiers()
	{
		Dictionary<string, TCount> nb = new Dictionary<string, TCount>();
		lock (Counts)
		{
			foreach (TCount t in Counts)
			{
				if (t.Field != "Quartiers" || string.IsNullOrEmpty(t.Value))
				{
					continue;
				}
				int i = t.Value.IndexOf('>');
				if (i <= 0)
				{
					continue;
				}
				string cp = t.Value.Substring(0, i);
				string quartier = t.Value.Substring(i + 1);
				string[] qs;
				if (quartier == "NC" || string.IsNullOrEmpty(quartier))
				{
					qs = new string[1] { "NC" };
				}
				else
				{
					Match m = rgx_tag.Match(quartier);
					qs = m.Success
						? m.Groups["id"].Captures.Select((Capture capture) => capture.Value).ToArray()
						: new string[1] { quartier };
				}
				string[] array = qs;
				foreach (string q in array)
				{
					string key = cp + ">" + q;
					if (!nb.TryGetValue(key, out var count))
					{
						count = Clone(t);
						count.Value = key;
						nb.Add(key, count);
					}
					else
					{
						Merge(count, t);
					}
				}
			}
			Counts.RemoveAll((TCount val) => val.Field == "Quartiers");
			foreach (KeyValuePair<string, TCount> c in nb.OrderBy((KeyValuePair<string, TCount> n) => n.Key))
			{
				Counts.Add(c.Value);
			}
		}
	}

	protected abstract void AddInfoFromDataReader(List<TCount> list, MySqlDataReader reader, string field, string value);

	protected abstract void Merge(TCount master, TCount slave);

	protected abstract TCount Clone(TCount count);
}
