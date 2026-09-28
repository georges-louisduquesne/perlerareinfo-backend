using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace ApiPerleRare.Services;

public class SearchService : ISearchService
{
	private class SearchConfig
	{
		public string CategoryId { get; set; }

		public string Category { get; set; }

		public string Select { get; set; }

		public Formula Url { get; set; }

		public SearchConfigRights Rights { get; set; } = SearchConfigRights.Tous;

		public Formula Output { get; set; }

		public SearchQueryType SearchQueryType { get; set; }

		public string TableName { get; set; }

		public string IdField { get; set; }

		public string Color { get; set; }

		public string DefaultColor { get; set; } = "#FFFFFF";

		public bool JoinMode { get; set; }

		public int HasAccessFieldIndex { get; set; }
	}

	[Flags]
	private enum SearchConfigRights
	{
		Tous = 0,
		Associés = 1,
		Admin = 2
	}

	public class Formula
	{
		private readonly AbstractValue[] _values;

		private Formula(AbstractValue[] values)
		{
			_values = values;
		}

		public static Formula Parse(string output, List<string> fields, string[] fieldsToFilter = null)
		{
			List<AbstractValue> values = new List<AbstractValue>();
			if (output.Contains('@'))
			{
				int pos = 0;
				foreach (Match m in rgx_field.Matches(output))
				{
					if (m.Index > pos)
					{
						values.Add(new ConstValue(output.Substring(pos, m.Index - pos)));
					}
					string fn = m.Groups["field"].Value;
					if (fn == "id")
					{
						values.Add(new FieldValue(0));
					}
					else
					{
						if (!fields.Contains(fn))
						{
							fields.Add(fn);
						}
						if (fieldsToFilter != null && fieldsToFilter.Contains(fn))
						{
							values.Add(new FilteredFieldValue(fields.IndexOf(fn)));
						}
						else
						{
							values.Add(new FieldValue(fields.IndexOf(fn)));
						}
					}
					pos = m.Index + m.Length;
				}
				if (pos < output.Length)
				{
					values.Add(new ConstValue(output.Substring(pos)));
				}
			}
			else
			{
				fields.Add(output);
				values.Add(new FieldValue(1));
			}
			return new Formula(values.ToArray());
		}

		public string Eval(MySqlDataReader reader)
		{
			return Eval(reader, null, SearchQueryType.Contains);
		}

		public string Eval(MySqlDataReader reader, SearchQuery query, SearchQueryType type)
		{
			return string.Concat(_values.Select((AbstractValue o) => o.GetVal(reader, query, type, query != null)));
		}
	}

	private abstract class AbstractValue
	{
		public abstract string GetVal(MySqlDataReader reader, SearchQuery query, SearchQueryType type, bool html);
	}

	private class ConstValue : AbstractValue
	{
		private string _value;

		public ConstValue(string value)
		{
			_value = value;
		}

		public override string GetVal(MySqlDataReader reader, SearchQuery query, SearchQueryType type, bool html)
		{
			return _value;
		}
	}

	private class FieldValue : AbstractValue
	{
		private int _drIndex;

		private static CultureInfo ci_fr = new CultureInfo("fr-fr");

		public FieldValue(int drIndex)
		{
			_drIndex = drIndex;
		}

		public override string GetVal(MySqlDataReader reader, SearchQuery query, SearchQueryType type, bool html)
		{
			if (reader.IsDBNull(_drIndex))
			{
				return "";
			}
			object val = reader.GetValue(_drIndex);
			string sVal = ((!(val is string s)) ? ((val is DateTime dt) ? dt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : ((val is MySqlDateTime md) ? $"{md.Day.ToString().PadLeft(2, '0')}/{md.Month.ToString().PadLeft(2, '0')}/{md.Year}" : ((!(val is int i) || 1 == 0) ? Convert.ToString(val, ci_fr) : i.ToString()))) : s);
			string res = EncodingHelper.FixEncoding(sVal);
			if (html)
			{
				return HttpUtility.HtmlEncode(res);
			}
			return res;
		}
	}

	private class FilteredFieldValue : FieldValue
	{
		public FilteredFieldValue(int drIndex)
			: base(drIndex)
		{
		}

		public override string GetVal(MySqlDataReader reader, SearchQuery query, SearchQueryType type, bool html)
		{
			string res = base.GetVal(reader, query, type, html);
			if (!html)
			{
				return res;
			}
			string filter = query.HtmlFilter;
			switch (type)
			{
			case SearchQueryType.Equal:
				if (string.Compare(filter, res, ignoreCase: true) == 0)
				{
					return Highlight(res);
				}
				return res;
			case SearchQueryType.StartsWith:
				if (res.StartsWith(filter, StringComparison.InvariantCultureIgnoreCase))
				{
					return Highlight(res.Substring(0, filter.Length)) + res.Substring(filter.Length);
				}
				return res;
			case SearchQueryType.Contains:
			{
				int pos = 0;
				string res2 = "";
				while (true)
				{
					int index = res.IndexOf(filter, pos, StringComparison.InvariantCultureIgnoreCase);
					if (index == -1)
					{
						break;
					}
					if (index > pos)
					{
						res2 += res.Substring(pos, index - pos);
					}
					res2 += Highlight(res.Substring(index, filter.Length));
					pos = index + filter.Length;
				}
				return res2 + res.Substring(pos);
			}
			default:
				throw new NotImplementedException();
			}
		}

		private string Highlight(string text)
		{
			return "<span class='hl'>" + text + "</span>";
		}
	}

	private class SearchStore
	{
		public Dictionary<string, HashSet<int>> IdsPerTable { get; set; } = new Dictionary<string, HashSet<int>>();

		public HashSet<int> Ids(string tableName)
		{
			if (!IdsPerTable.TryGetValue(tableName, out HashSet<int> ids))
			{
				ids = new HashSet<int>();
				IdsPerTable.Add(tableName, ids);
			}
			return ids;
		}
	}

	private readonly ApplicationDbContext _dbContext;

	private readonly IUserSessionService _userSessionService;

	private static readonly List<SearchConfig> _configs;

	private static Regex rgx_field;

	private const int PAGESIZE = 15;

	private static Regex rgx_urlPage;

	public static int NbConfigs => _configs.Count;

	public static IEnumerable<string> Categories => from c in _configs.Select((SearchConfig c) => c.Category).Distinct()
		orderby c
		select c;

	static SearchService()
	{
		_configs = new List<SearchConfig>();
		rgx_field = new Regex("@(?<field>(?:[a-zA-Z]+\\.)?[a-zA-Z_][a-zA-Z_0-9]*)");
		rgx_urlPage = new Regex("perle-rare\\.info/index\\.php\\?page=(?<page>\\w+)(?:&(?<extra>.+))?$");
		SearchConfig b1 = AddConfig("B1", "Conseiller", "https://perle-rare.info/index.php?page=conseillers_personnels_form&ref=@id", "conseillers_personnels", "@CP_Prenom @CP_NomFamille", SearchConfigRights.Admin, "CP_RefConseiller", new string[2] { "CP_NomFamille", "CP_TelPersonnel" }, "CP_Statut='Agent Commercial' OR CP_Statut='Associé'", "CP_RefConseiller", null, null, null, SearchQueryType.Equal, "#505569");
		SearchConfig b2 = AddConfig("B2", "Client actif", "https://perle-rare.info/index.php?page=contacts_recherche_form&ref=@id", "contacts_recherche", "@C_Prenom @C_NomFamille (négo: @C_Negociateur)", SearchConfigRights.Tous, "C_RefContact", new string[9] { "C_NomFamille", "C_TelPersonnel1", "C_TelPersonnel2", "C_TelProfessionnel1", "C_TelProfessionnel2", "C_TelMobile1", "C_TelMobile2", "C_Mel1", "C_Mel2" }, "C_Statut='CLIENT ACTIF'", "C_RefContact", null, null, null, SearchQueryType.Equal, "#72AC7A", "C_Apporteur=@conseillerid OR C_2emeApporteur=@conseillerid OR C_Negociateur=@conseillerid OR C_2emeNegociateur=@conseillerid OR C_NomFamilleConseiller=@conseillerid OR C_2emeConseiller=@conseillerid OR @isAdmin=1");
		SearchConfig b3 = AddConfig("B3", "Prospect actif", "https://perle-rare.info/index.php?page=contacts_recherche_form&ref=@id", "contacts_recherche", "@C_Prenom @C_NomFamille (négo: @C_Negociateur)", SearchConfigRights.Tous, "C_RefContact", new string[9] { "C_NomFamille", "C_TelPersonnel1", "C_TelPersonnel2", "C_TelProfessionnel1", "C_TelProfessionnel2", "C_TelMobile1", "C_TelMobile2", "C_Mel1", "C_Mel2" }, "C_Statut='PROSPECT ACTIF'", "C_RefContact", null, null, null, SearchQueryType.Equal, "#F3AD4D", "C_Apporteur=@conseillerid OR C_2emeApporteur=@conseillerid OR C_Negociateur=@conseillerid OR C_2emeNegociateur=@conseillerid OR C_NomFamilleConseiller=@conseillerid OR C_2emeConseiller=@conseillerid OR @isAdmin=1");
		SearchConfig b3bis = AddConfig("B3bis", "Collaborateur actif", "https://perle-rare.info/index.php?popup=interlocuteur&intermed_ref=@id.I_RefIntermediaire&ref_ctcinter=@id", "contact_intermediaire ci", "@ci.C_Prenom @ci.C_Nom (agence: @ci.C_NomIntermediaire)", SearchConfigRights.Tous, "ci.C_RefCtcInter", new string[3] { "ci.C_Nom", "ci.C_Tel", "ci.C_Mel" }, "ci.C_Statut='Actif' AND id.I_Actif=1", "ci.C_RefCtcInter", "intermediaires_directs id", "ci.C_RefIntermediaire=id.I_RefIntermediaire", "id.I_RefIntermediaire");
		SearchConfig b4 = AddConfig("B4", "Agence active", "https://perle-rare.info/index.php?popup=inter_direct&ref=@id", "intermediaires_directs", "@I_NomIntermediaire (@I_Adresse1 @I_CodePostal @I_Ville)", SearchConfigRights.Tous, "I_RefIntermediaire", new string[3] { "I_NomIntermediaire", "I_Telephone", "I_Telephone2" }, "I_Actif=1", "I_RefIntermediaire");
		SearchConfig b5 = AddConfig("B5", "Réseau d'agences actif", "https://perle-rare.info/index.php?popup=inter_indirect&ref=@id", "intermediaires_indirects", "@I2_NomInterm_indirect @I2_Categorie", SearchConfigRights.Tous, "I2_RefInterm_indirect", new string[1] { "I2_NomInterm_indirect" }, "I2_Actif=1", "I2_RefInterm_indirect");
		SearchConfig b6 = AddConfig("B6", "Client inactif", "https://perle-rare.info/index.php?page=contacts_recherche_form&ref=@id", "contacts_recherche", "@C_Prenom @C_NomFamille (négo: @C_Negociateur)", SearchConfigRights.Tous, "C_RefContact", new string[9] { "C_NomFamille", "C_TelPersonnel1", "C_TelPersonnel2", "C_TelProfessionnel1", "C_TelProfessionnel2", "C_TelMobile1", "C_TelMobile2", "C_Mel1", "C_Mel2" }, "C_Statut='CLIENT MORT'", "C_RefContact", null, null, null, SearchQueryType.Equal, "#72AC7A4C", "C_Apporteur=@conseillerid OR C_2emeApporteur=@conseillerid OR C_Negociateur=@conseillerid OR C_2emeNegociateur=@conseillerid OR C_NomFamilleConseiller=@conseillerid OR C_2emeConseiller=@conseillerid OR @isAdmin=1");
		SearchConfig b7 = AddConfig("B7", "Candidat", "https://perle-rare.info/index.php?page=conseillers_personnels_form&ref=@id", "conseillers_personnels", "@CP_Prenom @CP_NomFamille", SearchConfigRights.Admin, "CP_RefConseiller", new string[2] { "CP_NomFamille", "CP_TelPersonnel" }, "CP_Statut='Candidat'", "CP_RefConseiller", null, null, null, SearchQueryType.Equal, "#5A819E");
		SearchConfig b8 = AddConfig("B8", "Prospect inactif", "https://perle-rare.info/index.php?page=contacts_recherche_form&ref=@id", "contacts_recherche", "@C_Prenom @C_NomFamille (négo: @C_Negociateur)", SearchConfigRights.Tous, "C_RefContact", new string[9] { "C_NomFamille", "C_TelPersonnel1", "C_TelPersonnel2", "C_TelProfessionnel1", "C_TelProfessionnel2", "C_TelMobile1", "C_TelMobile2", "C_Mel1", "C_Mel2" }, "C_Statut='PROSPECT MORT'", "C_RefContact", null, null, null, SearchQueryType.Equal, "#F3AD4D4C", "C_Apporteur=@conseillerid OR C_2emeApporteur=@conseillerid OR C_Negociateur=@conseillerid OR C_2emeNegociateur=@conseillerid OR C_NomFamilleConseiller=@conseillerid OR C_2emeConseiller=@conseillerid OR @isAdmin=1");
		SearchConfig b9 = AddConfig("B9", "Agence inactive", "https://perle-rare.info/index.php?popup=inter_direct&ref=@id", "intermediaires_directs", "@I_NomIntermediaire (@I_Adresse1 @I_CodePostal @I_Ville)", SearchConfigRights.Tous, "I_RefIntermediaire", new string[3] { "I_NomIntermediaire", "I_Telephone", "I_Telephone2" }, "I_Actif=0", "I_RefIntermediaire");
		SearchConfig b10 = AddConfig("B10", "Bien", "https://perle-rare.info/index.php?page=contacts_recherche_form&ref=@id", "biens b", "@b.B_Adresse @b.B_CP @e.E_TypeEvenement @e.E_Date pour le client @e.E_NomContact", SearchConfigRights.Tous, "e.E_RefContact", new string[1] { "b.B_Adresse" }, "", "e.E_RefContact", "evenements e", "e.E_RefBien=b.B_Ref", "e.E_RefEvenement");
		SearchConfig b11 = AddConfig(b1, "B11", SearchQueryType.StartsWith);
		SearchConfig b12 = AddConfig(b2, "B12", SearchQueryType.StartsWith);
		SearchConfig b13 = AddConfig(b3, "B13", SearchQueryType.StartsWith);
		SearchConfig b13bis = AddConfig(b3bis, "B13bis", SearchQueryType.StartsWith);
		SearchConfig b14 = AddConfig(b4, "B14", SearchQueryType.StartsWith);
		SearchConfig b15 = AddConfig(b5, "B15", SearchQueryType.StartsWith);
		SearchConfig b16 = AddConfig(b6, "B16", SearchQueryType.StartsWith);
		SearchConfig b17 = AddConfig(b7, "B17", SearchQueryType.StartsWith);
		SearchConfig b18 = AddConfig(b8, "B18", SearchQueryType.StartsWith);
		SearchConfig b19 = AddConfig(b9, "B19", SearchQueryType.StartsWith);
		SearchConfig b20 = AddConfig(b10, "B20", SearchQueryType.StartsWith);
		SearchConfig b21 = AddConfig(b1, "B21", SearchQueryType.Contains);
		SearchConfig b22 = AddConfig(b2, "B22", SearchQueryType.Contains);
		SearchConfig b23 = AddConfig(b3, "B23", SearchQueryType.Contains);
		SearchConfig b23bis = AddConfig(b3bis, "B23bis", SearchQueryType.Contains);
		SearchConfig b24 = AddConfig(b4, "B24", SearchQueryType.Contains);
		SearchConfig b25 = AddConfig(b5, "B25", SearchQueryType.Contains);
		SearchConfig b26 = AddConfig(b6, "B26", SearchQueryType.Contains);
		SearchConfig b27 = AddConfig(b7, "B27", SearchQueryType.Contains);
		SearchConfig b28 = AddConfig(b8, "B28", SearchQueryType.Contains);
		SearchConfig b29 = AddConfig(b9, "B29", SearchQueryType.Contains);
		SearchConfig b30 = AddConfig(b10, "B30", SearchQueryType.Contains);
		SearchConfig b31 = AddConfig("B31", "Client ou prospect", "https://perle-rare.info/index.php?page=contacts_recherche_form&ref=@id", "contacts_recherche", "@C_Prenom @C_NomFamille (négo: @C_Negociateur)", SearchConfigRights.Tous, "C_RefContact", new string[3] { "C_Remarques", "C_Condition_Com", "C_Recherche_Com" }, "(C_Apporteur=@conseillerid OR C_2emeApporteur=@conseillerid OR C_Negociateur=@conseillerid OR C_2emeNegociateur=@conseillerid OR C_NomFamilleConseiller=@conseillerid OR C_2emeConseiller=@conseillerid OR @isAdmin=1) AND C_Statut='PROSPECT MORT'", "C_RefContact", null, null, null, SearchQueryType.Contains);
	}

	private static SearchConfig AddConfig(string categoryId, string category, string url, string tableName, string output, SearchConfigRights rights, string id, string[] fieldsToFilter, string where, string orderByDesc, string joinTableName = null, string joinOn = null, string joinId = null, SearchQueryType searchQueryType = SearchQueryType.Equal, string defaultColor = "#FFFFFF", string hasAccess = "")
	{
		List<string> fields = new List<string>();
		fields.Add(id);
		Formula outputFormula = Formula.Parse(output, fields, fieldsToFilter);
		Formula urlFormula = Formula.Parse(url, fields);
		int hasAccessFieldIndex;
		if (string.IsNullOrWhiteSpace(hasAccess))
		{
			hasAccessFieldIndex = -1;
		}
		else
		{
			fields.Add("(" + hasAccess + ") AS HasAccess");
			hasAccessFieldIndex = fields.Count - 1;
		}
		if (joinTableName != null && joinId == null)
		{
			throw new ArgumentNullException("JoinId", $"Nécessaire pour '{categoryId}/{category}'");
		}
		string select = "SELECT DISTINCT " + string.Join(", ", fields) + " FROM " + tableName;
		if (joinTableName != null)
		{
			select = select + " INNER JOIN " + joinTableName + " ON " + joinOn;
		}
		select += " WHERE (";
		select = select + string.Join(" OR ", fieldsToFilter.Select((string f) => f + " like @filter")) + ") *extrawhere*";
		if (!string.IsNullOrWhiteSpace(where))
		{
			select = select + " AND (" + where + ")";
		}
		select = select + " ORDER BY " + orderByDesc + " DESC";
		if (joinId != null)
		{
			select = select + ", " + joinId + " DESC";
		}
		if (_configs.Any((SearchConfig c) => c.CategoryId == categoryId))
		{
			throw new Exception("Plusieurs configs '" + categoryId + "'");
		}
		SearchConfig searchConfig = new SearchConfig
		{
			CategoryId = categoryId,
			Category = category,
			Select = select,
			Url = urlFormula,
			Rights = rights,
			Output = outputFormula,
			SearchQueryType = searchQueryType,
			TableName = tableName,
			IdField = id,
			DefaultColor = defaultColor,
			JoinMode = (joinId != null),
			HasAccessFieldIndex = hasAccessFieldIndex
		};
		_configs.Add(searchConfig);
		return searchConfig;
	}

	private static SearchConfig AddConfig(SearchConfig model, string categoryId, SearchQueryType searchQueryType)
	{
		if (_configs.Any((SearchConfig c) => c.CategoryId == categoryId))
		{
			throw new Exception("Plusieurs catégories '" + categoryId + "'");
		}
		SearchConfig searchConfig = new SearchConfig
		{
			CategoryId = categoryId,
			SearchQueryType = searchQueryType,
			Category = model.Category,
			Output = model.Output,
			Rights = model.Rights,
			Select = model.Select,
			Url = model.Url,
			TableName = model.TableName,
			IdField = model.IdField,
			DefaultColor = model.DefaultColor,
			JoinMode = model.JoinMode,
			HasAccessFieldIndex = model.HasAccessFieldIndex
		};
		_configs.Add(searchConfig);
		return searchConfig;
	}

	public SearchService(ApplicationDbContext dbContext, IUserSessionService userSessionService)
	{
		_dbContext = dbContext;
		_userSessionService = userSessionService;
	}

	public List<SelectResult> Search(SearchQuery query)
	{
		List<SelectResult> res = new List<SelectResult>();
		if (!string.IsNullOrWhiteSpace(query.Filter))
		{
			bool hasChanged = false;
			foreach (SearchConfig config in _configs)
			{
				if (config.Color != null)
				{
					continue;
				}
				lock (this)
				{
					if (config.Color == null)
					{
						RechercheGlobalConfig rgc = _dbContext.RechercheGlobalConfig.Find(config.Category);
						if (rgc == null)
						{
							rgc = new RechercheGlobalConfig
							{
								RgcCategorie = config.Category,
								RgcCouleur = config.DefaultColor
							};
							_dbContext.RechercheGlobalConfig.Add(rgc);
							hasChanged = true;
						}
						config.Color = rgc.RgcCouleur;
					}
				}
			}
			if (hasChanged)
			{
				_dbContext.SaveChanges();
			}
			using MySqlConnection connection = (MySqlConnection)_dbContext.Database.GetDbConnection();
			SearchConfigRights rights = SearchConfigRights.Tous;
			if (_userSessionService.Statut == "Associé")
			{
				rights |= SearchConfigRights.Associés;
			}
			if (!_userSessionService.Filter)
			{
				rights |= SearchConfigRights.Admin;
			}
			SearchStore searchStore = new SearchStore();
			CancellationToken ct = query.Cancellation;
			List<SearchConfig> allowed = _configs.Where((SearchConfig c) => (c.Rights & rights) == c.Rights).ToList();
			int prefetch = PrefetchSize(query.Max);
			string login = _userSessionService.Login;
			bool mustFilter = _userSessionService.Filter;
			List<List<SearchConfig>> tiers = ConsecutiveTiers(allowed, (SearchConfig c) => c.SearchQueryType);
			Task<PrefetchedRows[]>[] pending = new Task<PrefetchedRows[]>[tiers.Count];
			Task<PrefetchedRows[]> StartTier(int t) => pending[t] ??= Task.WhenAll(tiers[t].Select((SearchConfig c) =>
				PrefetchAsync(connection, c, query, login, mustFilter, prefetch, ct)));
			try
			{
				for (int t = 0; t < tiers.Count; t++)
				{
					ct.ThrowIfCancellationRequested();
					Task<PrefetchedRows[]> current = StartTier(t);
					if (t + 1 < tiers.Count && StartsAhead(tiers[t + 1][0].SearchQueryType))
					{
						StartTier(t + 1);
					}
					PrefetchedRows[] tierRows = current.GetAwaiter().GetResult();
					List<SearchConfig> tier = tiers[t];
					for (int i = 0; i < tier.Count && res.Count < query.Max; i++)
					{
						SearchConfig config2 = tier[i];
						HashSet<int> ids = searchStore.Ids(config2.TableName);
						if (!TakeFromPrefetch(tierRows[i], ids, res, query.Max - res.Count))
						{
							if (connection.State != System.Data.ConnectionState.Open)
							{
								connection.Open();
							}
							res.AddRange(Search(connection, config2, query, login, searchStore, mustFilter, query.Max - res.Count));
						}
					}
					if (res.Count >= query.Max)
					{
						break;
					}
				}
			}
			finally
			{
				foreach (Task<PrefetchedRows[]> task in pending)
				{
					task?.ContinueWith((Task<PrefetchedRows[]> x) => x.Exception, TaskContinuationOptions.OnlyOnFaulted);
				}
			}
		}
		return res.Take(query.Max).ToList();
	}

	internal sealed class PrefetchedRows
	{
		public List<(int Id, SelectResult Row)> Rows { get; } = new List<(int Id, SelectResult Row)>();

		public bool Truncated { get; set; }
	}

	/// <summary>
	/// Enough rows to skip every id already taken this search (at most max) and still fill a sequential page.
	/// </summary>
	internal static int PrefetchSize(int max)
	{
		return Math.Max(max, PAGESIZE) + Math.Max(max, 0);
	}

	/// <summary>
	/// Prefix/exact tiers are cheap enough to run speculatively alongside the previous tier;
	/// "%x%" scans stay on demand.
	/// </summary>
	internal static bool StartsAhead(SearchQueryType type)
	{
		return type != SearchQueryType.Contains;
	}

	internal static List<List<T>> ConsecutiveTiers<T, TKey>(IEnumerable<T> items, Func<T, TKey> key)
	{
		List<List<T>> tiers = new List<List<T>>();
		foreach (T item in items)
		{
			if (tiers.Count == 0 || !EqualityComparer<TKey>.Default.Equals(key(tiers[^1][0]), key(item)))
			{
				tiers.Add(new List<T>());
			}
			tiers[^1].Add(item);
		}
		return tiers;
	}

	/// <summary>
	/// Same stream as the sequential NOT IN paging: rows in SQL order, skipping ids already taken.
	/// Returns false when the prefetched prefix ran out before <paramref name="need"/> and more rows may exist.
	/// </summary>
	internal static bool TakeFromPrefetch(PrefetchedRows rows, HashSet<int> ids, List<SelectResult> res, int need)
	{
		int added = 0;
		foreach ((int id, SelectResult row) in rows.Rows)
		{
			if (added >= need)
			{
				return true;
			}
			if (ids.Contains(id))
			{
				continue;
			}
			res.Add(row);
			ids.Add(id);
			added++;
		}
		return added >= need || !rows.Truncated;
	}

	private static async Task<PrefetchedRows> PrefetchAsync(MySqlConnection connection, SearchConfig config, SearchQuery query, string login, bool mustFilter, int limit, CancellationToken ct)
	{
		PrefetchedRows rows = new PrefetchedRows();
		using MySqlConnection c2 = connection.Clone();
		await c2.OpenAsync(ct);
		using MySqlCommand cmd = c2.CreateCommand();
		cmd.CommandText = config.Select.Replace("*extrawhere*", "") + $" LIMIT {limit}";
		AddSearchParameters(cmd, config, query, login, mustFilter);
		using MySqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
		int read = 0;
		while (await reader.ReadAsync(ct))
		{
			read++;
			rows.Rows.Add((reader.GetInt32(0), BuildResult(reader, config, query)));
		}
		rows.Truncated = read >= limit;
		return rows;
	}

	private static void AddSearchParameters(MySqlCommand cmd, SearchConfig config, SearchQuery query, string login, bool mustFilter)
	{
		string filter = config.SearchQueryType switch
		{
			SearchQueryType.Equal => query.SqlFilter, 
			SearchQueryType.StartsWith => query.SqlFilter + "%", 
			SearchQueryType.Contains => "%" + query.SqlFilter + "%", 
			_ => throw new NotImplementedException($"Type de filtre {config.SearchQueryType} non implémenté"), 
		};
		cmd.Parameters.AddWithValue("@filter", filter);
		cmd.Parameters.AddWithValue("@conseillerid", login);
		cmd.Parameters.AddWithValue("@isAdmin", (!mustFilter) ? 1 : 0);
	}

	private static SelectResult BuildResult(MySqlDataReader reader, SearchConfig config, SearchQuery query)
	{
		bool hasAccess = config.HasAccessFieldIndex <= 0 || reader.GetBoolean(config.HasAccessFieldIndex);
		string url = config.Url.Eval(reader);
		return new SelectResult
		{
			CategoryId = config.CategoryId,
			Category = config.Category,
			Color = config.Color,
			Id = reader.GetInt32(0),
			Description = config.Output.Eval(reader, query, config.SearchQueryType),
			Url = (hasAccess ? url : "#"),
			Url2 = (hasAccess ? GetRightUrl(url) : "#")
		};
	}

	private IEnumerable<SelectResult> Search(MySqlConnection connection, SearchConfig config, SearchQuery query, string login, SearchStore store, bool mustFilter, int max)
	{
		HashSet<int> ids = store.Ids(config.TableName);
		bool again = true;
		List<SelectResult> res = new List<SelectResult>();
		int pageSize = Math.Max(max, 15);
		while (again)
		{
			again = false;
			string sql = config.Select;
			sql += $" LIMIT {pageSize}";
			sql = ((ids.Count != 0) ? sql.Replace("*extrawhere*", $" AND {config.IdField} NOT IN ({string.Join(", ", ids)} )") : sql.Replace("*extrawhere*", ""));
			int read = 0;
			using (MySqlCommand cmd = connection.CreateCommand())
			{
				cmd.CommandText = sql;
				AddSearchParameters(cmd, config, query, login, mustFilter);
				query.Cancellation.ThrowIfCancellationRequested();
				using CancellationTokenRegistration cancel = query.Cancellation.Register(cmd.Cancel);
				using MySqlDataReader reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					read++;
					int id = reader.GetInt32(0);
					if (!ids.Contains(id))
					{
						res.Add(BuildResult(reader, config, query));
						ids.Add(id);
						if (res.Count == max)
						{
							break;
						}
					}
				}
			}
			if (read == pageSize && res.Count < max)
			{
				again = true;
			}
		}
		return res;
	}

	public static string GetRightUrl(string url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return url;
		}
		Match m = rgx_urlPage.Match(url);
		if (m.Success)
		{
			url = "https://perle-rare.info/#/view/page/" + m.Groups["page"].Value;
			if (m.Groups["extra"].Success)
			{
				url = url + "&" + m.Groups["extra"].Value.Replace("=", "%3D");
			}
		}
		return url;
	}

	public void ClearCache()
	{
		lock (this)
		{
			foreach (SearchConfig c in _configs)
			{
				c.Color = null;
			}
		}
	}
}
