using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using ApiPerleRare.Predicates;
using ApiPerleRare.Properties;
using ApiPerleRare.RecupInfos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AnnoncesGlobalesController : ControllerBase
{
	public class InfoAnnonces
	{
		public InfoAnnoncesRes[] Res { get; set; }

		public string Error { get; set; }
	}

	private class ValuesGroups
	{
		public string[] Values { get; set; }
	}

	[DebuggerDisplay("{Field}-{Value} : {Count}")]
	public class InfoAnnoncesRes
	{
		public string Field { get; set; }

		public string Value { get; set; }

		public long Count { get; set; }
	}

	public class FilterDef
	{
		public string Where { get; set; }

		public uint ContactRef { get; set; }

		public bool Apply { get; set; }

		public int Limit { get; set; } = 5000;

		public FilterDefFieldInfo[] Fields { get; set; }
	}

	public class FilterDefFieldInfo
	{
		public string Field { get; set; }

		public string[] CheckedValues { get; set; }

		public string[] UncheckedValues { get; set; }
	}

	private class RecupState
	{
		private static List<RecupState> _states = new List<RecupState>();

		public uint ContactRef { get; set; }

		public static RecupState Start(uint contactRef)
		{
			if (contactRef == 0)
			{
				return null;
			}
			RecupState res;
			lock (_states)
			{
				_states.RemoveAll((RecupState r) => r.ContactRef == contactRef);
				res = new RecupState
				{
					ContactRef = contactRef
				};
				_states.Add(res);
			}
			return res;
		}

		public static bool IsCanceled(RecupState state)
		{
			if (state == null)
			{
				return false;
			}
			lock (_states)
			{
				return !_states.Contains(state);
			}
		}

		public static void End(RecupState state)
		{
			if (state == null)
			{
				return;
			}
			lock (_states)
			{
				if (_states.Contains(state))
				{
					_states.Remove(state);
				}
			}
		}
	}

	private readonly ApplicationDbContext _context;

	private readonly IExchangeService _exchangeService;

	private readonly IMemoryCache _memoryCache;

	private static Regex rgx_intBetween;

	private static Regex rgx_date;

	private static Regex rgx_in;

	private static Regex rgx_with;

	private static Regex rgx_fields;

	public AnnoncesGlobalesController(ApplicationDbContext context, IExchangeService exchangeService, IMemoryCache memoryCache)
	{
		_context = context;
		_exchangeService = exchangeService;
		_memoryCache = memoryCache;
	}

	[HttpGet]
	public async Task<ActionResult<SelectResult<AnnoncesGlobales>>> GetAnnoncesGlobales([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0, [FromQuery] int photos = 0)
	{
		IQueryable<AnnoncesGlobales> query = _context.AnnoncesGlobales;
		SelectResult<AnnoncesGlobales> annonces = await EFHelper<AnnoncesGlobales>.Select(query, where, orderby, take, skip, select);
		if (photos != 0)
		{
			string[] yanportProps = (from i in annonces.Items
				select i.AgIdPropertyYanport into p
				where !string.IsNullOrWhiteSpace(p)
				select p).Distinct().ToArray();
			PhotosAnnonces[] allPhotos = ((yanportProps.Length == 0) ? new PhotosAnnonces[0] : (await (from photosAnnonces in _context.PhotosAnnonces
				where yanportProps.Contains(photosAnnonces.PaIdPropertyYanport)
				orderby photosAnnonces.PaRef
				select photosAnnonces).ToArrayAsync()));
			AnnoncesGlobales[] items = annonces.Items;
			foreach (AnnoncesGlobales a in items)
			{
				a.Photos = allPhotos.Where((PhotosAnnonces p) => p.PaIdPropertyYanport == a.AgIdPropertyYanport).ToArray();
			}
		}
		return annonces;
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<AnnoncesGlobales>> GetAnnoncesGlobales(uint id, [FromQuery] int photos = 0)
	{
		IQueryable<AnnoncesGlobales> query = _context.AnnoncesGlobales;
		AnnoncesGlobales annoncesGlobales = await query.FirstOrDefaultAsync((AnnoncesGlobales ag) => ag.AgRef == id);
		if (annoncesGlobales == null)
		{
			return NotFound();
		}
		if (photos != 0)
		{
			if (string.IsNullOrWhiteSpace(annoncesGlobales.AgIdPropertyYanport))
			{
				annoncesGlobales.Photos = new PhotosAnnonces[0];
			}
			else
			{
				AnnoncesGlobales annoncesGlobales2 = annoncesGlobales;
				annoncesGlobales2.Photos = await (from a in _context.PhotosAnnonces
					where a.PaIdPropertyYanport == annoncesGlobales.AgIdPropertyYanport
					orderby a.PaRef
					select a).ToArrayAsync();
			}
		}
		return annoncesGlobales;
	}

	static AnnoncesGlobalesController()
	{
		rgx_intBetween = new Regex("^\\s*(?<from>\\d+)\\s*\\.\\.\\s*(?<to>\\d+)\\s*$", RegexOptions.Compiled);
		rgx_date = new Regex("^<=\\s*(?<nb>-?\\d+)\\s*(?<unit>[djm])\\s*$", RegexOptions.Compiled);
		rgx_in = new Regex("^in\\s*\\(\\s*(?<v>!?\\d+)\\s*(?:,\\s*(?<v>!?\\d+))*\\s*\\)\\s*$|^\\[\\s*(?<v>!?\\d+)\\s*(?:,\\s*(?<v>!?\\d+))*\\s*\\]\\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		rgx_with = new Regex("^(?<before>.*\\S)\\s+with\\s+(?<field>\\S.+)\\s*=(?<value>.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		rgx_fields = new Regex("Ag\\w+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
	}

	[HttpPost("RecupInfosAnnonces")]
	public async Task<ActionResult<InfoAnnonces>> RecupInfosAnnonces(FilterDef filterDef)
	{
		RecupState state = RecupState.Start(filterDef.ContactRef);
		try
		{
			string defaultWhere = GetDefaultWhere(filterDef);
			DbConnection c = _context.Database.GetDbConnection();
			if (c.State != ConnectionState.Open)
			{
				await c.OpenAsync();
			}
			List<InfoAnnoncesRes> res = new List<InfoAnnoncesRes>();
			for (int i = 0; i < filterDef.Fields.Length; i++)
			{
				if (RecupState.IsCanceled(state))
				{
					return new InfoAnnonces
					{
						Error = $"Cancelled (field {i})"
					};
				}
				string where = defaultWhere;
				for (int j = 0; j < filterDef.Fields.Length; j++)
				{
					if (i == j)
					{
						continue;
					}
					FilterDefFieldInfo f = filterDef.Fields[j];
					string[] vals = f.CheckedValues;
					if (vals.Length != 0)
					{
						where = ((!(f.Field == "AgListeTags")) ? (where + " AND (" + string.Join(" OR ", vals.Select((string v) => MakePredicate(f.Field, v))) + ")") : (where + " AND (" + string.Join(" AND ", vals.Select((string v) => MakePredicate(f.Field, v))) + ")"));
					}
				}
				int total;
				using (DbCommand cmd = c.CreateCommand())
				{
					cmd.CommandText = "SELECT COUNT(*) FROM annonces_globales WHERE " + where;
					total = Convert.ToInt32(cmd.ExecuteScalar());
					res.Add(new InfoAnnoncesRes
					{
						Field = filterDef.Fields[i].Field,
						Value = "Total",
						Count = total
					});
				}
				if (total == 0)
				{
					continue;
				}
				FilterDefFieldInfo f2 = filterDef.Fields[i];
				GetSqlFieldName(new string[1] { f2.Field });
				string[] allVals = f2.CheckedValues.Union(f2.UncheckedValues).ToArray();
				ValuesGroups[] groups = new ValuesGroups[1]
				{
					new ValuesGroups
					{
						Values = allVals
					}
				};
				ValuesGroups[] array = groups;
				foreach (ValuesGroups group in array)
				{
					if (RecupState.IsCanceled(state))
					{
						return new InfoAnnonces
						{
							Error = $"Cancelled (field2 {i})"
						};
					}
					if (group.Values.Length == 0)
					{
						continue;
					}
					string groupWhere = "(" + string.Join(" OR ", group.Values.Select((string v) => MakePredicate(f2.Field, v))) + ")";
					string[] colums = group.Values.Select((string v, int value) => $"CASE WHEN {MakePredicate(f2.Field, v)} THEN 1 ELSE 0 END AS V{value}").ToArray();
					string sql = $"SELECT DISTINCT {string.Join(", ", colums)}, \r\n                            AG_Ref_AGC\r\n                            FROM annonces_globales WHERE {where} AND {groupWhere}\r\n                            ORDER BY AG_Ref_AGC";
					using DbCommand cmd2 = c.CreateCommand();
					cmd2.CommandText = sql;
					using DbDataReader reader = cmd2.ExecuteReader();
					int[] counters = new int[group.Values.Length + 1];
					long agRefAgc = 0L;
					while (reader.Read())
					{
						long newAgRefAgc = reader.GetInt64(group.Values.Length);
						if (newAgRefAgc != agRefAgc)
						{
							agRefAgc = newAgRefAgc;
							for (int j2 = 0; j2 < group.Values.Length; j2++)
							{
								counters[j2] += reader.GetInt32(j2);
							}
							counters[group.Values.Length]++;
						}
					}
					for (int j3 = 0; j3 < group.Values.Length; j3++)
					{
						res.Add(new InfoAnnoncesRes
						{
							Field = f2.Field,
							Value = group.Values[j3],
							Count = counters[j3]
						});
					}
					res.Add(new InfoAnnoncesRes
					{
						Field = f2.Field,
						Value = "NB",
						Count = counters[group.Values.Length]
					});
				}
			}
			if (filterDef.Apply && filterDef.ContactRef != 0)
			{
				if (RecupState.IsCanceled(state))
				{
					return new InfoAnnonces
					{
						Error = "Cancelled (apply)"
					};
				}
				Dictionary<string, string> importParams = GetImportParams();
				string tableName = $"annonces_refcontact_{filterDef.ContactRef}";
				if (!c.DoesTableExist(tableName))
				{
					using DbCommand cmd3 = c.CreateCommand();
					cmd3.CommandText = Resources.CreateContactAnnonces.Replace("{contactRef}", filterDef.ContactRef.ToString());
					cmd3.ExecuteNonQuery();
				}
				else
				{
					using DbCommand cmd4 = c.CreateCommand();
					cmd4.CommandText = "TRUNCATE TABLE " + tableName;
					cmd4.ExecuteNonQuery();
				}
				ContactsRecherche contact = _context.ContactsRecherche.Find(filterDef.ContactRef);
				bool isAlertOn = contact.CAlerteMail == 1;
				if (isAlertOn)
				{
					contact.CAlerteMail = 0;
					await _context.SaveChangesAsync();
				}
				string sql2 = ConvertFilterDefToAnnoncesGlobalesSQL(filterDef, defaultWhere, importParams);
				List<object[]> dataToInsert = new List<object[]>();
				string[] fields = null;
				using (DbCommand cmd5 = c.CreateCommand())
				{
					cmd5.CommandText = sql2;
					using DbDataReader reader2 = cmd5.ExecuteReader();
					while (reader2.Read())
					{
						if (fields == null)
						{
							fields = new string[reader2.FieldCount];
							for (int i2 = 0; i2 < fields.Length; i2++)
							{
								fields[i2] = reader2.GetName(i2);
							}
						}
						object[] row = new object[fields.Length];
						reader2.GetValues(row);
						dataToInsert.Add(row);
					}
				}
				foreach (object[] row2 in dataToInsert)
				{
					if (RecupState.IsCanceled(state))
					{
						break;
					}
					try
					{
						using MySqlCommand insertCmd = (MySqlCommand)c.CreateCommand();
						List<string> sqlFields = new List<string>();
						List<string> sqlValues = new List<string>();
						for (int i3 = 0; i3 < fields.Length; i3++)
						{
							object val = row2[i3];
							if (val != null && !(val is DBNull))
							{
								string toField = importParams[fields[i3]];
								insertCmd.Parameters.AddWithValue("@" + toField, val);
								sqlFields.Add(toField);
								sqlValues.Add("@" + toField);
							}
						}
						insertCmd.CommandText = $"INSERT INTO {tableName} ({string.Join(",", sqlFields)}) VALUES ({string.Join(",", sqlValues)})";
						insertCmd.ExecuteNonQuery();
					}
					catch (Exception ex)
					{
						_exchangeService.LogException("Erreur ajout enregistrement table " + tableName, filterDef, ex);
					}
				}
				if (isAlertOn)
				{
					contact.CAlerteMail = 1;
					await _context.SaveChangesAsync();
				}
			}
			return new InfoAnnonces
			{
				Res = res.ToArray()
			};
		}
		catch (Exception ex2)
		{
			Exception ex3 = ex2;
			_exchangeService.LogException("Erreur RecupInfosAnnonces", filterDef, ex3);
			return new InfoAnnonces
			{
				Error = ex3.ToString()
			};
		}
		finally
		{
			RecupState.End(state);
		}
	}

	[HttpPost("RecupInfosAnnonces2")]
	public ActionResult<RecupInfoResponse> RecupInfosAnnonces2(Filter filter)
	{
		MySqlConnection c = (MySqlConnection)_context.Database.GetDbConnection();
		if (c.State != ConnectionState.Open)
		{
			c.Open();
		}
		return (RecupInfoResponse)RecupInfoSearcher.Search(_context, c, filter, _exchangeService, _memoryCache);
	}

	[HttpGet("ViderInfosAnnonces/{contactRef}")]
	public ActionResult ViderInfosAnnonces(uint contactRef)
	{
		MySqlConnection c = (MySqlConnection)_context.Database.GetDbConnection();
		if (c.State != ConnectionState.Open)
		{
			c.Open();
		}
		string tableName = $"annonces_refcontact_{contactRef}";
		if (c.DoesTableExist(tableName))
		{
			using MySqlCommand cmd = c.CreateCommand();
			cmd.CommandText = "TRUNCATE TABLE " + tableName;
			cmd.ExecuteNonQuery();
		}
		return Ok();
	}

	private static string GetDefaultWhere(FilterDef filterDef)
	{
		if (string.IsNullOrWhiteSpace(filterDef.Where))
		{
			return "1=1";
		}
		return Parser.Parse(filterDef.Where).GetSQL(GetSqlFieldName);
	}

	public static string ConvertFilterDefToAnnoncesGlobalesSQL(FilterDef filterDef)
	{
		return ConvertFilterDefToAnnoncesGlobalesSQL(filterDef, GetDefaultWhere(filterDef), GetImportParams());
	}

	private static string ConvertFilterDefToAnnoncesGlobalesSQL(FilterDef filterDef, string defaultWhere, Dictionary<string, string> importParams)
	{
		string where = defaultWhere;
		for (int j = 0; j < filterDef.Fields.Length; j++)
		{
			FilterDefFieldInfo f = filterDef.Fields[j];
			string[] vals = f.CheckedValues;
			if (vals.Length != 0)
			{
				where = ((!(f.Field == "AgListeTags")) ? (where + " AND (" + string.Join(" OR ", vals.Select((string v) => MakePredicate(f.Field, v))) + ")") : (where + " AND (" + string.Join(" AND ", vals.Select((string v) => MakePredicate(f.Field, v))) + ")"));
			}
		}
		string[] agFields = importParams.Keys.Where((string n) => n.StartsWith("AG_")).ToArray();
		string sql = "SELECT " + string.Join(",", agFields) + ", \r\n                        mm.M_Nom AS Source, CURDATE() AS Date_Aspi, '0000-00-00' AS Date_Aff, '' AS Com  FROM annonces_globales AS ag\r\n                        LEFT JOIN meta_moteurs as mm ON ag.AG_RefMetaMoteur = mm.M_interindirect \r\n                        WHERE " + where;
		if (filterDef.Limit > 0)
		{
			sql += $" LIMIT {filterDef.Limit}";
		}
		return sql;
	}

	private static Dictionary<string, string> GetImportParams()
	{
		Regex rgx_line = new Regex("^(?<from>[A-Z_]+)\\s*>\\s*(?<to>[A-Z_]+)$|^`(?<to>[A-Z_]+)`", RegexOptions.IgnoreCase);
		Dictionary<string, string> importParams = new Dictionary<string, string>();
		string[] array = Resources.CreateContactAnnoncesParameters.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
		foreach (string line in array)
		{
			if (line.StartsWith("//"))
			{
				continue;
			}
			Match m = rgx_line.Match(line);
			if (!m.Success)
			{
				throw new Exception("Paramétrage '" + line + "' non comprise");
			}
			string to = m.Groups["to"].Value;
			string from;
			if (m.Groups["from"].Success)
			{
				from = m.Groups["from"].Value;
			}
			else
			{
				if (!to.StartsWith("A_"))
				{
					throw new Exception("'" + to + "' devrait commencé par 'A_'");
				}
				from = "AG_" + to.Substring(2);
			}
			importParams[from] = to;
		}
		return importParams;
	}

	private static string MakePredicate(string field, string value)
	{
		Match m = rgx_with.Match(value);
		if (m.Success)
		{
			string p = MakePredicate(field, m.Groups["before"].Value.Trim());
			string otherField = GetSqlFieldName(new string[1] { m.Groups["field"].Value });
			string otherValue = m.Groups["value"].Value;
			string otherLit = SqlSafety.IsNumber(otherValue) ? SqlSafety.NumberLiteral(otherValue) : SqlSafety.Quote(otherValue);
			return $"(({p}) AND {otherField}={otherLit})";
		}
		if (value == "*")
		{
			return "1=1";
		}
		string fn = GetSqlFieldName(new string[1] { field });
		if (string.Compare(value, "null", ignoreCase: true) == 0)
		{
			return fn + " IS NULL";
		}
		if (string.Compare(value, "not null", ignoreCase: true) == 0)
		{
			return fn + " IS NOT NULL";
		}
		if (value.StartsWith(">="))
		{
			return fn + " >= " + SqlSafety.NumberLiteral(value.Substring(2, value.Length - 2).Trim());
		}
		if (value == "NC")
		{
			return $"({fn} IS NULL OR {fn}='')";
		}
		m = rgx_intBetween.Match(value);
		if (m.Success)
		{
			return $"({fn}>={m.Groups["from"].Value} AND {fn}<={m.Groups["to"].Value})";
		}
		m = rgx_date.Match(value);
		if (m.Success)
		{
			string unit = m.Groups["unit"].Value;
			string nb = m.Groups["nb"].Value;
			if (unit == "d" || unit == "j")
			{
				unit = "day";
			}
			else
			{
				if (!(unit == "m"))
				{
					throw new Exception("Unité '" + unit + "' non géré");
				}
				unit = "month";
			}
			return $"{fn}<=DATE_ADD(NOW(), INTERVAL {SqlSafety.NumberLiteral(nb)} {unit})";
		}
		m = rgx_in.Match(value);
		if (m.Success)
		{
			var values = m.Groups["v"].Captures.Select((Capture c) => new
			{
				IsNeg = (c.Value[0] == '!'),
				Val = int.Parse(c.Value.TrimStart('!'))
			}).ToArray();
			string positives = "(" + string.Join(" OR ", from v in values
				where !v.IsNeg
				select $"INSTR({fn}, '[{v.Val}]')>0") + ")";
			string negatives = string.Join(" AND ", from v in values
				where v.IsNeg
				select $"INSTR({fn}, '[{v.Val}]')=0");
			if (negatives.Length == 0)
			{
				return positives;
			}
			if (positives.Length == 2)
			{
				return negatives;
			}
			return positives + " AND " + negatives;
		}
		return fn + " = " + SqlSafety.Quote(value);
	}

	private static string GetSqlFieldName(string[] propNames)
	{
		string propName = propNames.Single();
		string sqlName = rgx_fields.Replace(propName, delegate(Match m)
		{
			if (string.Compare(m.Value, "AgRefAgc", ignoreCase: true) == 0)
			{
				return "AG_Ref_AGC";
			}
			string value = m.Value;
			return "AG_" + value.Substring(2, value.Length - 2);
		});
		return SqlSafety.Identifier(sqlName);
	}
}
