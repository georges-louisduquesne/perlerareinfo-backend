using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ApiPerleRare.Application.Abstractions;
using ApiPerleRare.Models;
using ApiPerleRare.Properties;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;

namespace ApiPerleRare.RecupInfos;

public class RecupInfoSearcher
{
	private enum FilterName
	{
		CpAndQuartiers,
		NbPieces,
		NbChambres,
		Etage,
		DernierEtage,
		Exclusif,
		BaissePrix,
		Surface,
		Budget,
		BudgetSurface,
		Anciennete,
		Tags,
		TypeBien,
		TypeTransaction
	}

	public static bool Debug { get; set; }

	public static AbstractRecupInfoResponse Search(IApplicationDbContext context, MySqlConnection connection, Filter filter, IExchangeService exchangeService, IMemoryCache memoryCache)
	{
		try
		{
			long token;
			if (filter.ContactRef != 0)
			{
				token = DateTime.Now.Ticks;
				AbstractRecupInfoResponse<RecupInfoCount>.SearchTokens[filter.ContactRef] = token;
			}
			else
			{
				token = 0L;
			}
			if (filter.TypeTransaction == TypeTransaction.None)
			{
				throw new ArgumentException("Type de transaction obligatoire");
			}
			if (filter.TypeBien == null || filter.TypeBien.Length == 0)
			{
				throw new ArgumentException("Type de bien obligatoire");
			}
			// Recherche / RecupInfosAnnonces2 counts `property` (P_*), like PHP / GET Property (PState eq 1).
			// Do not require P_DateFin IS NULL: Yanport rows often keep DateFin = 0000-00-00, which is not SQL NULL.
			string where = "P_State = 1";
			const string countExpr = "COUNT(DISTINCT P_PropertyId) AS NbAnnonces";
			AbstractRecupInfoResponse response = new RecupInfoResponse();
			response.Token = token;
			response.ContactRef = filter.ContactRef;
			response.Infos = filter.Infos;
			Dictionary<FilterName, string> fieldFilters = new Dictionary<FilterName, string>();
			fieldFilters.Add(FilterName.TypeTransaction, $"P_TypeTransaction='{filter.TypeTransaction}'");
			fieldFilters.Add(FilterName.TypeBien, string.Join(" OR ", filter.TypeBien.Select((TypeBien tb) => "P_Type='" + tb.GetStringValue() + "'")));
			if ((filter.CP != null && filter.CP.Length != 0) || (filter.Quartiers != null && filter.Quartiers.Length != 0))
			{
				if (filter.CP == null)
				{
					filter.CP = new int[0];
				}
				if (filter.Quartiers == null)
				{
					filter.Quartiers = new string[0];
				}
				fieldFilters.Add(FilterName.CpAndQuartiers, string.Join(" OR ", filter.CP.Select((int p) => $"P_CP='{p}'").Union(filter.Quartiers.Select((string p) => MakeQuartierFilter(p)))));
			}
			if (filter.NbPieces != null && filter.NbPieces.Length != 0)
			{
				fieldFilters.Add(FilterName.NbPieces, string.Join(" OR ", filter.NbPieces.Select((string p) => MakeIntFilter("P_NbPieces", p))));
			}
			if (filter.NbChambres != null && filter.NbChambres.Length != 0)
			{
				fieldFilters.Add(FilterName.NbChambres, string.Join(" OR ", filter.NbChambres.Select((string p) => MakeIntFilter("P_NbChambres", p))));
			}
			if (filter.Etage != null && filter.Etage.Length != 0)
			{
				fieldFilters.Add(FilterName.Etage, string.Join(" OR ", filter.Etage.Select((string p) => MakeIntFilter("P_Etage", p))));
			}
			if (filter.EstDernierEtage.HasValue)
			{
				fieldFilters.Add(FilterName.DernierEtage, "P_EstDernierEtage = " + ((filter.EstDernierEtage == true) ? 1 : 0));
			}
			if (filter.EstExclusif.HasValue)
			{
				fieldFilters.Add(FilterName.Exclusif, "P_EstExclusif = " + ((filter.EstExclusif == true) ? 1 : 0));
			}
			if (filter.AvecBaissePrix == true)
			{
				fieldFilters.Add(FilterName.BaissePrix, "P_PrixEvol = 1");
			}
			else if (filter.AvecBaissePrix == false)
			{
				fieldFilters.Add(FilterName.BaissePrix, "(P_PrixEvol IS NULL OR P_PrixEvol <> 1)");
			}
			if (filter.SurfaceMax > 0)
			{
				fieldFilters.Add(FilterName.Surface, $"P_Surface BETWEEN {filter.SurfaceMin} AND {filter.SurfaceMax}");
			}
			if (filter.BudgetMax > 0)
			{
				fieldFilters.Add(FilterName.Budget, $"P_Prix BETWEEN {filter.BudgetMin} AND {filter.BudgetMax}");
			}
			if (filter.BudgetSurfaceMax > 0)
			{
				fieldFilters.Add(FilterName.BudgetSurface, $"FLOOR(P_Prix / P_Surface) BETWEEN {filter.BudgetSurfaceMin} AND {filter.BudgetSurfaceMax}");
			}
			if (filter.AncienneteMax > 0)
			{
				fieldFilters.Add(FilterName.Anciennete, $"ABS(DATEDIFF(CURRENT_DATE(), `P_DateDebut`) ) BETWEEN {filter.AncienneteMin} AND {filter.AncienneteMax}");
			}
			if (filter.Tags != null && filter.Tags.Length != 0)
			{
				string tagWhere = string.Join(" AND ", from t in filter.Tags
					where t > 0
					select $"P_ListeTags LIKE '%[{t}]%'");
				if (filter.Tags.Any((int t) => t < 0))
				{
					if (tagWhere.Length > 0)
					{
						tagWhere += " AND ";
					}
					tagWhere += "(P_ListeTags IS NULL OR (";
					tagWhere += string.Join(" AND ", from t in filter.Tags
						where t < 0
						select $"P_ListeTags NOT LIKE '%[{-t}]%'");
					tagWhere += "))";
				}
				where = where + " AND " + tagWhere;
			}
			List<Task> queryTasks = new List<Task>();
			queryTasks.Add(response.AddAsync(connection, memoryCache, "TypeTransaction", "SELECT P_TypeTransaction, " + countExpr + " \r\nFROM property \r\nWHERE " + MakeWhere(where, fieldFilters, FilterName.TypeTransaction) + "\r\nGROUP BY P_TypeTransaction"));
			queryTasks.Add(response.AddAsync(connection, memoryCache, "TypeBien", "SELECT P_Type, " + countExpr + " \r\nFROM property \r\nWHERE " + MakeWhere(where, fieldFilters, FilterName.TypeBien) + " \r\nGROUP BY P_Type"));
			queryTasks.Add(response.AddAsync(connection, memoryCache, "Cp", "SELECT P_CP, " + countExpr + " FROM property WHERE " + MakeWhere(where, fieldFilters, default(FilterName)) + " GROUP BY P_CP"));
			queryTasks.Add(response.AddAsync(connection, memoryCache, "Quartiers", "SELECT CONCAT(P_CP, '>', P_Quartier2), " + countExpr + " \r\nFROM (\r\n  SELECT P_PropertyId, P_CP, IF(P_Quartier2 IS NULL OR P_Quartier2 = '', 'NC', P_Quartier2) AS P_Quartier2 \r\n  FROM property \r\n  WHERE " + MakeWhere(where, fieldFilters, default(FilterName)) + " \r\n  GROUP BY P_PropertyId, P_CP, P_Quartier2 \r\n) a \r\nGROUP BY P_CP, P_Quartier2"));
			queryTasks.Add(response.AddAsync(connection, memoryCache, "NbPieces", "SELECT IFNULL(P_NbPieces, 'NC'), " + countExpr + " \r\nFROM property \r\nWHERE " + MakeWhere(where, fieldFilters, FilterName.NbPieces) + " \r\nGROUP BY IFNULL(P_NbPieces, 'NC')"));
			queryTasks.Add(response.AddAsync(connection, memoryCache, "NbChambres", "SELECT IFNULL(P_NbChambres, 'NC') AS P_NbChambres, " + countExpr + " \r\nFROM property \r\nWHERE " + MakeWhere(where, fieldFilters, FilterName.NbChambres) + " GROUP BY P_NbChambres"));
			queryTasks.Add(response.AddAsync(connection, memoryCache, "Surface", "SELECT IFNULL(FLOOR(P_Surface / 5) * 5, 'NC'), " + countExpr + " FROM property WHERE " + MakeWhere(where, fieldFilters, FilterName.Surface) + " GROUP BY IFNULL(FLOOR(P_Surface / 5) * 5, 'NC')"));
			queryTasks.Add(response.AddAsync(connection, memoryCache, "Prix", "SELECT IFNULL(FLOOR(P_Prix/1000) * 1000, 'NC') AS floor, " + countExpr + "  FROM property WHERE " + MakeWhere(where, fieldFilters, FilterName.Budget) + " GROUP BY IFNULL(FLOOR(P_Prix/1000) * 1000, 'NC')"));
			queryTasks.Add(response.AddAsync(connection, memoryCache, "PrixSurface", "SELECT IFNULL(FLOOR( FLOOR(P_Prix / P_Surface) / 1) * 1, 'NC') AS floor, " + countExpr + " FROM property WHERE " + MakeWhere(where, fieldFilters, FilterName.BudgetSurface) + " GROUP BY floor"));
			queryTasks.Add(response.AddAsync(connection, memoryCache, "Etage", "SELECT IFNULL(P_Etage, 'NC') AS P_Etage, " + countExpr + " \r\nFROM property \r\nWHERE " + MakeWhere(where, fieldFilters, FilterName.Etage) + " \r\nGROUP BY IFNULL(P_Etage, 'NC')"));
			queryTasks.Add(response.AddAsync(connection, memoryCache, "EstDernierEtage", "SELECT P_EstDernierEtage, " + countExpr + " \r\nFROM property \r\nWHERE " + MakeWhere(where, fieldFilters, FilterName.DernierEtage) + " \r\nGROUP BY P_EstDernierEtage", 0, 1, isBoolean: true));
			queryTasks.Add(response.AddAsync(connection, memoryCache, "EstExclusif", "SELECT P_EstExclusif, " + countExpr + " \r\nFROM property \r\nWHERE " + MakeWhere(where, fieldFilters, FilterName.Exclusif) + " GROUP BY P_EstExclusif", 0, 1, isBoolean: true));
			queryTasks.Add(response.AddAsync(connection, memoryCache, "BaissePrix", "SELECT '0', " + countExpr + " FROM property WHERE " + MakeWhere(where, fieldFilters, FilterName.BaissePrix) + " AND (P_PrixEvol IS NULL OR P_PrixEvol <> 1)"));
			queryTasks.Add(response.AddAsync(connection, memoryCache, "BaissePrix", "SELECT '1', " + countExpr + " FROM property WHERE " + MakeWhere(where, fieldFilters, FilterName.BaissePrix) + " AND P_PrixEvol = 1"));
			queryTasks.Add(response.AddAsync(connection, memoryCache, "Anciennete", "SELECT IFNULL(ABS(DATEDIFF(CURRENT_DATE(), `P_DateDebut`)), '') AS anciennT, " + countExpr + " \r\nFROM property \r\nWHERE " + MakeWhere(where, fieldFilters, FilterName.Anciennete) + " GROUP BY anciennT \r\nORDER BY anciennT ASC"));
			queryTasks.Add(response.AddAsync(connection, memoryCache, "Tag", "SELECT P_ListeTags, " + countExpr + " \r\nFROM property \r\nWHERE " + MakeWhere(where, fieldFilters, FilterName.Tags) + " GROUP BY P_ListeTags"));
			Task.WaitAll(queryTasks.ToArray());
			response.AdjustQuartiers();
			response.SumIntValues("NbPieces", 6);
			response.SumIntValues("NbChambres", 6);
			response.SumIntValues("Etage", 4);
			response.AdjustTags();
			return response;
		}
		catch (Exception ex)
		{
			return new RecupInfoResponse
			{
				Error = ex.ToString()
			};
		}
	}

	private static async Task FillAnnoncesAsync(IApplicationDbContext context, DbConnection connection, Filter filter, IExchangeService exchangeService, string where, AbstractRecupInfoResponse response, Dictionary<FilterName, string> fieldFilters)
	{
		DateTime start = DateTime.Now;
		Dictionary<string, string> importParams = GetImportParams();
		string tableName = $"annonces_refcontact_{filter.ContactRef}";
		if (!connection.DoesTableExist(tableName))
		{
			using DbCommand cmd = connection.CreateCommand();
			cmd.CommandText = Resources.CreateContactAnnonces.Replace("{contactRef}", filter.ContactRef.ToString());
			await cmd.ExecuteNonQueryAsync();
		}
		else
		{
			using DbCommand cmd2 = connection.CreateCommand();
			cmd2.CommandText = "TRUNCATE TABLE " + tableName;
			await cmd2.ExecuteNonQueryAsync();
		}
		ContactsRecherche contact = await context.ContactsRecherche.FindAsync(filter.ContactRef);
		bool isAlertOn = contact.CAlerteMail == 1;
		if (isAlertOn)
		{
			contact.CAlerteMail = 0;
			await context.SaveChangesAsync();
		}
		string[] agFields = importParams.Keys.Where((string n) => n.StartsWith("AG_")).ToArray();
		string sql = "SELECT " + string.Join(",", agFields) + ", \r\n                        mm.M_Nom AS Source, CURDATE() AS Date_Aspi, '0000-00-00' AS Date_Aff, '' AS Com  FROM annonces_globales AS ag\r\n                        LEFT JOIN meta_moteurs as mm ON ag.AG_RefMetaMoteur = mm.M_interindirect \r\n                        WHERE " + MakeWhere(where, fieldFilters);
		List<object[]> dataToInsert = new List<object[]>();
		string[] fields = null;
		using (DbCommand cmd3 = connection.CreateCommand())
		{
			cmd3.CommandText = sql;
			using DbDataReader reader = await cmd3.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				if (fields == null)
				{
					fields = new string[reader.FieldCount];
					for (int i = 0; i < fields.Length; i++)
					{
						fields[i] = reader.GetName(i);
					}
				}
				object[] row = new object[fields.Length];
				reader.GetValues(row);
				dataToInsert.Add(row);
			}
		}
		foreach (object[] row2 in dataToInsert)
		{
			try
			{
				using MySqlCommand insertCmd = (MySqlCommand)connection.CreateCommand();
				List<string> sqlFields = new List<string>();
				List<string> sqlValues = new List<string>();
				for (int i2 = 0; i2 < fields.Length; i2++)
				{
					object val = row2[i2];
					if (val != null && !(val is DBNull))
					{
						string toField = importParams[fields[i2]];
						insertCmd.Parameters.AddWithValue("@" + toField, val);
						sqlFields.Add(toField);
						sqlValues.Add("@" + toField);
					}
				}
				insertCmd.CommandText = $"INSERT INTO {tableName} ({string.Join(",", sqlFields)}) VALUES ({string.Join(",", sqlValues)})";
				await insertCmd.ExecuteNonQueryAsync();
			}
			catch (Exception ex)
			{
				exchangeService.LogException("Erreur ajout enregistrement table " + tableName, filter, ex);
			}
		}
		if (isAlertOn)
		{
			contact.CAlerteMail = 1;
			context.SaveChanges();
		}
		TimeSpan duration = DateTime.Now - start;
		response.Perfs.Add(new RecupInfoPerf
		{
			Duration = duration.TotalSeconds,
			Field = "Inserts",
			Start = start
		});
	}

	private static void FillAnnonces(IApplicationDbContext context, DbConnection connection, Filter filter, IExchangeService exchangeService, string where, AbstractRecupInfoResponse response, Dictionary<FilterName, string> fieldFilters)
	{
		DateTime start = DateTime.Now;
		Dictionary<string, string> importParams = GetImportParams();
		string tableName = $"annonces_refcontact_{filter.ContactRef}";
		if (!connection.DoesTableExist(tableName))
		{
			using DbCommand cmd = connection.CreateCommand();
			cmd.CommandText = Resources.CreateContactAnnonces.Replace("{contactRef}", filter.ContactRef.ToString());
			cmd.ExecuteNonQuery();
		}
		else
		{
			using DbCommand cmd2 = connection.CreateCommand();
			cmd2.CommandText = "TRUNCATE TABLE " + tableName;
			cmd2.ExecuteNonQuery();
		}
		ContactsRecherche contact = context.ContactsRecherche.Find(filter.ContactRef);
		bool isAlertOn = contact.CAlerteMail == 1;
		if (isAlertOn)
		{
			contact.CAlerteMail = 0;
			context.SaveChanges();
		}
		string[] agFields = importParams.Keys.Where((string n) => n.StartsWith("AG_")).ToArray();
		string sql = "SELECT " + string.Join(",", agFields) + ", \r\n                        mm.M_Nom AS Source, CURDATE() AS Date_Aspi, '0000-00-00' AS Date_Aff, '' AS Com  FROM annonces_globales AS ag\r\n                        LEFT JOIN meta_moteurs as mm ON ag.AG_RefMetaMoteur = mm.M_interindirect \r\n                        WHERE " + MakeWhere(where, fieldFilters);
		List<object[]> dataToInsert = new List<object[]>();
		string[] fields = null;
		using (DbCommand cmd3 = connection.CreateCommand())
		{
			cmd3.CommandText = sql;
			using DbDataReader reader = cmd3.ExecuteReader();
			while (reader.Read())
			{
				if (fields == null)
				{
					fields = new string[reader.FieldCount];
					for (int i = 0; i < fields.Length; i++)
					{
						fields[i] = reader.GetName(i);
					}
				}
				object[] row = new object[fields.Length];
				reader.GetValues(row);
				dataToInsert.Add(row);
			}
		}
		foreach (object[] row2 in dataToInsert)
		{
			try
			{
				using MySqlCommand insertCmd = (MySqlCommand)connection.CreateCommand();
				List<string> sqlFields = new List<string>();
				List<string> sqlValues = new List<string>();
				for (int i2 = 0; i2 < fields.Length; i2++)
				{
					object val = row2[i2];
					if (val != null && !(val is DBNull))
					{
						string toField = importParams[fields[i2]];
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
				exchangeService.LogException("Erreur ajout enregistrement table " + tableName, filter, ex);
			}
		}
		if (isAlertOn)
		{
			contact.CAlerteMail = 1;
			context.SaveChanges();
		}
		TimeSpan duration = DateTime.Now - start;
		response.Perfs.Add(new RecupInfoPerf
		{
			Duration = duration.TotalSeconds,
			Field = "Inserts",
			Start = start
		});
	}

	private static string MakeQuartierFilter(string cpAndQ)
	{
		int i = cpAndQ.IndexOf('>');
		string cp = cpAndQ.Substring(0, i);
		string q = cpAndQ.Substring(i + 1);
		if (q == "NC")
		{
			return "P_CP='" + cp + "' AND (P_Quartier2 IS NULL OR P_Quartier2='')";
		}
		return "P_Quartier2 LIKE '%[" + q + "]%'";
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

	private static string MakeWhere(string where, Dictionary<FilterName, string> filters, params FilterName[] fns)
	{
		string w = where;
		foreach (KeyValuePair<FilterName, string> f in filters)
		{
			if (!fns.Contains(f.Key))
			{
				w = w + " AND (" + f.Value + ")";
			}
		}
		return w;
	}

	private static string MakeIntFilter(string field, string value)
	{
		if (value == "NC")
		{
			return field + " IS NULL";
		}
		if (value.StartsWith(">="))
		{
			string text = value;
			return field + " >= " + text.Substring(2, text.Length - 2);
		}
		if (value.EndsWith("+"))
		{
			string text = value;
			return field + " >= " + text.Substring(0, text.Length - 1);
		}
		return field + " = " + value;
	}
}
