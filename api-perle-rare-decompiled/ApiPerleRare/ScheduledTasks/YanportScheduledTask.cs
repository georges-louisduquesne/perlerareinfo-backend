using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using ApiPerleRare.Models;
using ApiPerleRare.YanportModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiPerleRare.ScheduledTasks;

public class YanportScheduledTask : AbstractScheduledTask
{
	private class RequestInfos
	{
		public int Id { get; internal set; }

		public string Cp { get; internal set; }

		public short? Type { get; internal set; }

		public DateTime Date { get; internal set; }
	}

	private class PacketInfo
	{
		public DateTime Start { get; set; }

		public DateTime End { get; set; }

		public int Num { get; set; }

		public int Size { get; set; }

		public string GetLog()
		{
			string log = $"Packet {Num}, {Size} hits : ";
			if (End.Ticks == 0)
			{
				return log + Start.ToString("T") + "...";
			}
			return log + (End - Start).TotalMinutes.ToString("N1") + " mins";
		}
	}

	public class ImportDataStats
	{
		public int Updated { get; set; }

		public int Inserted { get; set; }

		public int Deactivated { get; set; }

		public int Total { get; set; }

		public void Add(ImportDataStats stats)
		{
			Updated += stats.Updated;
			Inserted += stats.Inserted;
			Deactivated += stats.Deactivated;
			Total += stats.Total;
		}
	}

	private abstract class TempField
	{
		public abstract bool IsDefined { get; }

		public abstract string Name { get; }

		public PropertyInfo AGProp { get; protected set; }

		public abstract object GetValue(ImportContext ic);

		public abstract string GetIgnoreReason(ImportContext ic);
	}

	private class TempField<TValue> : TempField
	{
		private readonly string _name;

		private readonly Func<ImportContext, TValue> _getVal;

		private readonly Func<ImportContext, string> _ignore;

		public int MaxLength { get; internal set; }

		public override bool IsDefined => _getVal != null;

		public override string Name => _name;

		public TempField(string name, Expression<Func<AnnoncesGlobales, TValue>> agProp, Func<ImportContext, TValue> getVal, Func<ImportContext, string> ignore)
		{
			_name = name;
			_getVal = getVal;
			_ignore = ignore;
			if (agProp == null)
			{
				return;
			}
			MemberExpression ma;
			if (agProp.Body is MemberExpression)
			{
				ma = (MemberExpression)agProp.Body;
			}
			else
			{
				if (!(agProp.Body is UnaryExpression ue))
				{
					throw new Exception(agProp.Body.GetType().FullName + " non géré");
				}
				ma = (MemberExpression)ue.Operand;
			}
			base.AGProp = (PropertyInfo)ma.Member;
			if (base.AGProp == null)
			{
				throw new Exception("Il faut set AGProp");
			}
		}

		public override object GetValue(ImportContext ic)
		{
			object val = _getVal(ic);
			if (val is string s && MaxLength > 0 && s.Length > MaxLength)
			{
				val = s.Substring(0, MaxLength);
			}
			return val;
		}

		public override string GetIgnoreReason(ImportContext ic)
		{
			return (_ignore == null) ? null : _ignore(ic);
		}
	}

	public class ImportContext
	{
		private readonly IExchangeService _exchangeService;

		public ImportDbContext DbContext { get; }

		public Hit Hit { get; }

		public Ad Ad { get; }

		public Dealer Dealer { get; }

		public UrlSearch Us { get; }

		public Source Source { get; }

		public ImportContext(ImportDbContext dbContext, Hit hit, Ad ad, Dealer dealer, UrlSearch us, Source source, IExchangeService exchangeService)
		{
			DbContext = dbContext;
			Hit = hit;
			Ad = ad;
			Dealer = dealer;
			Us = us;
			Source = source;
			_exchangeService = exchangeService;
		}

		public string GetLienId()
		{
			return Source.NomMoteur + "-AdId:" + Ad.Id;
		}

		internal string GetTypeTransaction()
		{
			return (Hit.Marketing.Type == "SALE") ? "A" : "L";
		}

		public void LogError(string subject, string error)
		{
			if (_exchangeService == null)
			{
				Console.WriteLine("Error '" + subject + "' : " + error);
			}
			else
			{
				_exchangeService.SendMail("huberje@yahoo.fr", subject, error);
			}
		}
	}

	public class ImportDbContext
	{
		private readonly ApplicationDbContext _dbContext;

		private Dictionary<long, Quartiers> cache_quartiers = new Dictionary<long, Quartiers>();

		private TagsAnnonce[] _tagsAnnonces;

		public TagsAnnonce[] TagsAnnonces
		{
			get
			{
				if (_tagsAnnonces == null && _dbContext != null)
				{
					_tagsAnnonces = _dbContext.TagsAnnonce.Where((TagsAnnonce ta) => ta.TaAlias != null && ta.TaAlias != "").ToArray();
				}
				return _tagsAnnonces;
			}
		}

		public ImportDbContext(ApplicationDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		public Quartiers GetQuartier(long quarterId)
		{
			if (quarterId == 0)
			{
				return null;
			}
			if (!cache_quartiers.TryGetValue(quarterId, out var name))
			{
				name = _dbContext.Quartiers.FirstOrDefault((Quartiers q) => q.QQuarterIdYanport == quarterId);
				cache_quartiers.Add(quarterId, name);
			}
			return name;
		}
	}

	public class Source
	{
		private readonly Regex _idRegex;

		public string NomMoteur { get; }

		public Source(string nomMoteur, string regex)
		{
			NomMoteur = nomMoteur;
			_idRegex = new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
		}

		public string GetIdFromUrl(string url)
		{
			Match m = _idRegex.Match(url);
			if (m.Success)
			{
				return m.Groups["id"].Value;
			}
			return "";
		}
	}

	private readonly IYanportService _yanportService;

	private readonly ILogger<YanportScheduledTask> _logger;

	private readonly IServiceScopeFactory _serviceScopeFactory;

	private readonly IWebSiteService _webSiteService;

	private readonly IExchangeService _exchangeService;

	private const string NbRequestMaxParam = "_nb_requests_max";

	private const string NbRequestMinParam = "_nb_requests_min";

	private const string NbHitsByPacket = "_nb_hits_per_packet";

	private int _nbHitsPerPacket;

	private const int Shift = 30;

	private static List<string> _unkownSources;

	internal static Dictionary<string, Source> _sources;

	private static TempField[] _importFields;

	public static Dictionary<string, Source> Sources => _sources;

	public YanportScheduledTask(IYanportService yanportService, ILogger<YanportScheduledTask> logger, IServiceScopeFactory serviceScopeFactory, IWebSiteService webSiteService, IExchangeService exchangeService)
	{
		_yanportService = yanportService;
		_logger = logger;
		_serviceScopeFactory = serviceScopeFactory;
		_webSiteService = webSiteService;
		_exchangeService = exchangeService;
	}

	public override string Execute(DateTime lastExecution)
	{
		return Run();
	}

	public string Run()
	{
		LogInfo("Import Yanport...");
		RequestInfos[] requestInfos;
		int nbRequestsMax;
		int nbRequestsMin;
		using (IServiceScope scope = _serviceScopeFactory.CreateScope())
		{
			using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			YanportInterruptions interruption = dbContext.YanportInterruptions.Where((YanportInterruptions i) => i.YiDateDebut <= DateTime.Now && (i.YiDateFin >= DateTime.Now || i.YiDateFin == null)).FirstOrDefault();
			if (interruption != null)
			{
				LogInfo("Interruption '" + interruption.YiRaison + "'");
				return "Interruption '" + interruption.YiRaison + "'";
			}
			requestInfos = (from u in dbContext.UrlSearch
				where u.UsSuspendu && ((int?)u.UsType == (int?)9 || (int?)u.UsType == (int?)10) && u.UsProchainPassage <= DateTime.Now && u.UsUrl.StartsWith("https://api.yanport.com/properties") && u.UsEnCours == false
				orderby u.UsType descending, u.UsProchainPassage
				select new RequestInfos
				{
					Id = u.UsCle,
					Cp = u.UsLocalite,
					Type = u.UsType,
					Date = u.UsProchainPassage
				}).ToArray();
			if (requestInfos.Length == 0)
			{
				return "Aucun url_search à traiter";
			}
			nbRequestsMax = Math.Max(2, GetParam(dbContext, "_nb_requests_max", 5));
			nbRequestsMin = Math.Min(3, GetParam(dbContext, "_nb_requests_min", 3));
			_nbHitsPerPacket = GetParam(dbContext, "_nb_hits_per_packet", 50);
		}
		DateTime start = DateTime.Now;
		StringBuilder logs = new StringBuilder();
		LogInfo($"Import Yanport : {requestInfos.Length} requêtes trouvées");
		bool made9type = false;
		int nbRequests = 0;
		StringBuilder stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler;
		using (IServiceScope scope2 = _serviceScopeFactory.CreateScope())
		{
			using ApplicationDbContext dbContext2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			RequestInfos[] array = requestInfos;
			foreach (RequestInfos ri in array)
			{
				LogInfo($"Import Yanport #{ri.Id} : {ri.Cp} {ri.Type} {ri.Date:G}");
				UrlSearch request = dbContext2.UrlSearch.Find(ri.Id);
				if (request == null || request.UsEnCours)
				{
					continue;
				}
				if (ri.Type == 9)
				{
					if (made9type)
					{
						LogInfo("Skipped car déjà suffisamment de 9 de fait...");
						continue;
					}
					made9type = true;
				}
				request.UsEnCours = true;
				dbContext2.SaveChanges();
				DateTime date = DateTime.Now;
				try
				{
					int nb = (request.UsLastCount = ImportRequest(request));
					request.UsMaxCount = Math.Max(request.UsMaxCount, nb);
					request.UsDernierPassage = date;
					request.UsProchainPassage = DateTime.Now.AddMinutes(request.UsMajoration);
					TimeSpan d = DateTime.Now - date;
					stringBuilder = logs;
					StringBuilder stringBuilder2 = stringBuilder;
					handler = new StringBuilder.AppendInterpolatedStringHandler(41, 4, stringBuilder);
					handler.AppendLiteral("url_search #");
					handler.AppendFormatted(request.UsCle);
					handler.AppendLiteral(" (");
					handler.AppendFormatted(ri.Type);
					handler.AppendLiteral(") ok : ");
					handler.AppendFormatted(nb);
					handler.AppendLiteral(" biens trouvés en ");
					handler.AppendFormatted(d.TotalSeconds, "N0");
					handler.AppendLiteral(" s");
					stringBuilder2.AppendLine(ref handler);
				}
				catch (Exception ex)
				{
					if (request.UsLastCount >= 0)
					{
						request.UsLastCount = -1;
					}
					else
					{
						request.UsLastCount--;
					}
					if (request.UsLastCount <= -5)
					{
						LogError(_exchangeService, "Erreur import request " + ri.Id, ex.ToString());
					}
					stringBuilder = logs;
					StringBuilder stringBuilder3 = stringBuilder;
					handler = new StringBuilder.AppendInterpolatedStringHandler(25, 2, stringBuilder);
					handler.AppendLiteral("url_search #");
					handler.AppendFormatted(request.UsCle);
					handler.AppendLiteral(" exception : ");
					handler.AppendFormatted(ex.Message);
					stringBuilder3.AppendLine(ref handler);
				}
				finally
				{
					request.UsEnCours = false;
					dbContext2.SaveChanges();
				}
				Thread.Sleep(150);
				LogInfo($"Import Yanport : {ri.Cp} {ri.Type} {ri.Date:G} OK");
				nbRequests++;
				if (!AbstractScheduledTask.IsPauseTime && (nbRequests < nbRequestsMax || !((DateTime.Now - start).TotalMinutes > 5.0)) && (nbRequests < nbRequestsMin || !((DateTime.Now - start).TotalMinutes >= 30.0)))
				{
					continue;
				}
				break;
			}
		}
		stringBuilder = logs;
		StringBuilder stringBuilder4 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(31, 2, stringBuilder);
		handler.AppendFormatted(nbRequests);
		handler.AppendLiteral(" url_search traités sur ");
		handler.AppendFormatted(requestInfos.Length);
		handler.AppendLiteral(" dispos");
		stringBuilder4.AppendLine(ref handler);
		TimeSpan duration = DateTime.Now - start;
		LogInfo($"Import Yanport terminé en {duration.TotalSeconds:N2} secondes ({nbRequests} appels Yanport)");
		return logs.ToString();
	}

	private void LogInfo(string message, bool debugOnly = false)
	{
		if (!debugOnly)
		{
			_logger.LogInformation(message);
			Log(message);
		}
	}

	private int ImportRequest(UrlSearch request)
	{
		DateTime from = request.UsDernierPassage.AddSeconds(-30.0);
		DateTime start = DateTime.Now;
		string url = request.UsUrl;
		string time = from.ToUniversalTime().ToString("s");
		string stockTime = start.AddMonths(-5).ToString("s");
		UrlLogs log = new UrlLogs
		{
			UlDate = start
		};
		switch (request.UsType)
		{
		case 9:
			url = url + "&sort=marketing.publicationStartDate:desc&updateDateMin=" + stockTime;
			log.UlIsIn = 1;
			break;
		case 10:
			url = url + "&updateDateMin=" + time;
			log.UlIsIn = 1;
			break;
		default:
			return -1;
		}
		LogInfo($"Type {request.UsType} : appel à {url}...");
		string json = _yanportService.Get(request.UsCle, url);
		PropertiesResponse res = JsonSerializer.Deserialize<PropertiesResponse>(json, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});
		LogInfo($"Hits : {res.Hits.Length}, Ads : {res.Hits.Where((Hit h) => h.Ads != null).Sum((Hit h) => h.Ads.Length)}");
		int nb = 0;
		Hit[][] packets = (from v in res.Hits.Select((Hit h, int i) => new { h, i })
			group v by v.i / _nbHitsPerPacket into v
			select v.Select(t => t.h).ToArray()).ToArray();
		int packetNb = 0;
		Hit[][] array = packets;
		foreach (Hit[] hits in array)
		{
			using IServiceScope scope = _serviceScopeFactory.CreateScope();
			using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			try
			{
				packetNb++;
				LogInfo($"Packet {packetNb}/{packets.Length}...");
				YanportImporter importer = new YanportImporter(dbContext, delegate(string m)
				{
					LogError(_exchangeService, "YanportImporter exception", m);
				});
				Hit[] array2 = hits;
				foreach (Hit hit in array2)
				{
					if (hit.Ads == null)
					{
						continue;
					}
					hit.Ads = hit.Ads.Where((Ad a) => !a.MustBeIgnored).ToArray();
					if (hit.Ads.Length == 0)
					{
						continue;
					}
					if (hit.Marketing?.Dealers != null)
					{
						hit.Marketing.Dealers = hit.Marketing.Dealers.Where((Dealer d) => d.SubType != "REALTY_DEVELOPER").ToArray();
						if (hit.Marketing.Dealers.Length == 0)
						{
							continue;
						}
					}
					LogInfo($"Packet {packetNb}/{packets.Length} - {nb}/{res.Hits.Length}...", debugOnly: true);
					importer.Import(hit, delegate(string m)
					{
						LogInfo($"Packet {packetNb}/{packets.Length} - {nb}/{res.Hits.Length} - {m}...", debugOnly: true);
					});
					nb++;
				}
				dbContext.SaveChanges();
				LogInfo($"Packet {packetNb}/{packets.Length} OK");
			}
			catch (Exception ex)
			{
				LogError(_exchangeService, $"UrlSearch#{request.UsCle} : erreur YanportImporter ({res?.Hits?.Length} biens)", ex.ToString());
				_logger.LogError(ex, "Exception importer");
			}
		}
		using (IServiceScope scope2 = _serviceScopeFactory.CreateScope())
		{
			using ApplicationDbContext dbContext2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			log.UlUrl = url;
			log.UlNb = nb;
			dbContext2.UrlLogs.Add(log);
			dbContext2.SaveChanges();
		}
		return nb;
	}

	public static int VerifPerimData(ApplicationDbContext dbContext, IExchangeService exchangeService, UrlSearch request, PropertiesResponse res)
	{
		int nb = 0;
		ImportDbContext idc = new ImportDbContext(dbContext);
		foreach (ImportContext ic in BrowseAllAnnonces(idc, exchangeService, request, res))
		{
			nb++;
			if (!DesactivateIfNecessary(dbContext, ic))
			{
				LogError(exchangeService, $"UrlSearch#{request.UsCle} : pas de PublicationEndDate pour {ic.Hit.Id} / {ic.GetLienId()}", "");
			}
		}
		return nb;
	}

	public static bool DesactivateIfNecessary(ApplicationDbContext dbContext, ImportContext ic)
	{
		DateTime? newAgTimeEndY = ic.Hit.Marketing?.PublicationEndDate.ToLocalTime();
		if (!newAgTimeEndY.HasValue)
		{
			return false;
		}
		AnnoncesGlobales[] annonces = dbContext.AnnoncesGlobales.Where((AnnoncesGlobales ag) => ag.AgIdPropertyYanport == ((object)ic.Hit.Id).ToString()).ToArray();
		annonces = annonces.Where((AnnoncesGlobales annoncesGlobales) => !annoncesGlobales.AgTimeEndY.HasValue || annoncesGlobales.AgTimeEndY.Value.Year < 1980).ToArray();
		if (annonces.Length != 0)
		{
			AnnoncesGlobales[] array = annonces;
			foreach (AnnoncesGlobales a in array)
			{
				a.AgTimeEndY = newAgTimeEndY;
				a.AgDateFin = DateTime.Now;
			}
			dbContext.SaveChanges();
		}
		return true;
	}

	public static void LogError(IExchangeService exchangeService, string subject, string error)
	{
		if (exchangeService == null)
		{
			Console.WriteLine("Error '" + subject + "' : " + error);
		}
		else
		{
			exchangeService.SendMail("huberje@yahoo.fr", subject, error);
		}
	}

	public static IEnumerable<ImportContext> BrowseAllAnnonces(ImportDbContext dbContext, IExchangeService exchangeService, UrlSearch request, PropertiesResponse properties)
	{
		Dictionary<Source, List<string>> urlWithoutId = new Dictionary<Source, List<string>>();
		Hit[] hits = properties.Hits;
		foreach (Hit hit in hits)
		{
			Ad[] ads = hit.Ads;
			foreach (Ad ad in ads)
			{
				if (ad.Id == Guid.Empty)
				{
					continue;
				}
				int index = Array.IndexOf(hit.Ads, ad);
				Dealer dealer = ((hit.Marketing?.Dealers == null || index >= hit.Marketing.Dealers.Length) ? null : hit.Marketing.Dealers[index]);
				if (!_sources.TryGetValue(ad.CrawlSource, out var source))
				{
					if (!_unkownSources.Contains(ad.CrawlSource))
					{
						_unkownSources.Add(ad.CrawlSource);
						LogError(exchangeService, $"UrlSearch#{request?.UsCle} : source {ad.CrawlSource} inconnue", "");
					}
				}
				else
				{
					yield return new ImportContext(dbContext, hit, ad, dealer, request, source, exchangeService);
					source = null;
				}
			}
		}
		foreach (KeyValuePair<Source, List<string>> err in urlWithoutId)
		{
			LogError(exchangeService, $"UrlSearch#{request.UsCle} : erreurs récupération ID de l'URL pour '{err.Key.NomMoteur}'", string.Join("<br/>", err.Value));
		}
	}

	public static ImportDataStats ImportData(ApplicationDbContext dbContext, IExchangeService exchangeService, IWebSiteService webSiteService, UrlSearch us, DateTime start, PropertiesResponse res, ILogger logger)
	{
		ImportDataStats stats = new ImportDataStats();
		ImportDbContext idc = new ImportDbContext(dbContext);
		List<string> notExisting = new List<string>();
		foreach (IGrouping<Source, ImportContext> ic_moteur in from importContext in BrowseAllAnnonces(idc, exchangeService, us, res)
			group importContext by importContext.Source)
		{
			string nomMoteur = ic_moteur.Key.NomMoteur;
			int refMoteur = (from mm in dbContext.MetaMoteurs
				where mm.MNom == nomMoteur
				select mm.MInterindirect).FirstOrDefault();
			if (refMoteur == 0)
			{
				continue;
			}
			logger.LogInformation($"Moteur {nomMoteur} - {ic_moteur.Count()} annonces...");
			Dictionary<string, string> liensSimplifiés = new Dictionary<string, string>();
			List<Guid> properties = new List<Guid>();
			List<uint> histoAdded = new List<uint>();
			foreach (ImportContext ic in ic_moteur)
			{
				logger.LogInformation($"   {ic.Hit.Id} - {ic.Ad.Id}...");
				stats.Total++;
				if (DesactivateIfNecessary(dbContext, ic))
				{
					logger.LogInformation("   bien désactivé");
					stats.Deactivated++;
					continue;
				}
				Guid propId = ic.Hit.Id;
				if (us.UsType == 9 && !properties.Contains(propId))
				{
					properties.Add(propId);
				}
				try
				{
					string ls = ic.GetLienId();
					if (liensSimplifiés.ContainsKey(ls))
					{
						logger.LogError($"Plusieurs annonces {nomMoteur} avec même lien id : {ic.Ad.Url} et {liensSimplifiés[ls]}");
						continue;
					}
					liensSimplifiés.Add(ls, ic.Ad.Url);
					var values = (from i in _importFields
						where i.IsDefined
						select new
						{
							Name = i.Name,
							Value = i.GetValue(ic)
						} into anon
						where anon.Value != null
						select anon).ToArray();
					AnnoncesGlobales[] agRecords = dbContext.AnnoncesGlobales.Where((AnnoncesGlobales annoncesGlobales) => annoncesGlobales.AgIdAnnonceYanport == ((object)ic.Ad.Id).ToString() || annoncesGlobales.AgLien == ic.Ad.Url).ToArray();
					AnnoncesGlobales[] recordsWithDifferencePropIds = agRecords.Where((AnnoncesGlobales annoncesGlobales) => annoncesGlobales.AgIdPropertyYanport != ic.Hit.Id.ToString()).ToArray();
					if (recordsWithDifferencePropIds.Length != 0)
					{
						AnnoncesGlobales[] array = recordsWithDifferencePropIds;
						foreach (AnnoncesGlobales ag in array)
						{
							dbContext.AnnoncesGlobales.Remove(ag);
						}
						dbContext.SaveChanges();
						agRecords = agRecords.Except(recordsWithDifferencePropIds).ToArray();
					}
					AnnoncesGlobales agRecord = agRecords.FirstOrDefault((AnnoncesGlobales annoncesGlobales) => annoncesGlobales.AgLien == ic.Ad.Url);
					if (agRecord == null)
					{
						agRecord = agRecords.FirstOrDefault();
					}
					if (agRecord != null && agRecords.Length > 1)
					{
						AnnoncesGlobales[] array2 = agRecords;
						foreach (AnnoncesGlobales toRemove in array2)
						{
							if (toRemove != agRecord)
							{
								string diff = GetDifferences(ic, toRemove);
								dbContext.AnnoncesGlobales.Remove(toRemove);
								LogError(exchangeService, "Info : suppression annonce '" + toRemove.AgIdAnnonceYanport + "' car même id d'annonce mais url différente", diff);
							}
						}
						dbContext.SaveChanges();
					}
					if (agRecord != null)
					{
						if (agRecord.AgIdAnnonceYanport != ic.Ad.Id.ToString())
						{
							string diff2 = GetDifferences(ic, agRecord);
							LogError(exchangeService, $"Info : changement d'id d'annonce : {agRecord.AgIdAnnonceYanport} => {ic.Ad.Id}", diff2);
						}
						List<string> changes = new List<string>();
						uint? prixAvant = agRecord.AgPrix;
						foreach (TempField f in _importFields.Where((TempField tempField) => tempField.AGProp != null && tempField.Name != "A_Lien"))
						{
							object val = f.GetValue(ic);
							if (val == null && f.AGProp.Name != "AgTimeEndY")
							{
								continue;
							}
							if (f.AGProp.PropertyType == typeof(byte?) && val is int)
							{
								val = Convert.ToByte(val);
							}
							if (f.AGProp.PropertyType == typeof(ushort?) && val is uint valuint)
							{
								if (valuint > 65535)
								{
									LogError(exchangeService, $"UrlSearch#{us.UsCle} : erreur insertion enregistrement {ic.Hit.Id}/{ic.Ad.Id} - {f.AGProp.Name}", $"Valeur trop grande pour '{f.AGProp.Name}':{valuint}");
									continue;
								}
								val = Convert.ToUInt16(val);
							}
							if (f.AGProp.PropertyType == typeof(short?) && val is int)
							{
								val = Convert.ToInt16(val);
							}
							if (f.AGProp.PropertyType == typeof(float?) && val is double)
							{
								val = Convert.ToSingle(val);
							}
							object actual = f.AGProp.GetValue(agRecord);
							if (!AreIdentical(actual, val) && !(f.AGProp.Name == "AgTimeY"))
							{
								changes.Add(f.AGProp.Name);
								f.AGProp.SetValue(agRecord, val);
							}
						}
						if (changes.Count > 0)
						{
							dbContext.SaveChanges();
							stats.Updated++;
							logger.LogInformation("   mis à jour : " + string.Join(", ", changes));
						}
						uint? prixApres = agRecord.AgPrix;
						if (prixAvant.HasValue && prixApres.HasValue && (double)Math.Abs(prixAvant.Value - prixApres.Value) < 0.1 && !histoAdded.Contains(agRecord.AgRef))
						{
							string v = ((prixAvant.Value < prixApres.Value) ? "AUG" : "DIM");
							dbContext.Database.ExecuteSqlRaw($"INSERT IGNORE INTO `historique_annonces` \r\n                                    (`H_RefAnnonce`, `H_Date`, `H_TypeVariation`, `H_AncienneValeur`, `H_NouvelleValeur`) VALUES \r\n                                    ({agRecord.AgRef}, '{DateTime.Today:yyyy-MM-dd}', '{v}', {prixAvant.Value}, {prixApres.Value})");
						}
					}
					else
					{
						agRecord = dbContext.AnnoncesGlobales.FirstOrDefault((AnnoncesGlobales annoncesGlobales) => annoncesGlobales.AgIdPropertyYanport == ((object)ic.Hit.Id).ToString());
						AnnoncesGlobales newRecord = new AnnoncesGlobales();
						foreach (TempField f2 in _importFields.Where((TempField tempField) => tempField.AGProp != null))
						{
							object val2 = f2.GetValue(ic);
							if (val2 == null)
							{
								continue;
							}
							if (f2.AGProp.PropertyType == typeof(byte?) && val2 is int)
							{
								val2 = Convert.ToByte(val2);
							}
							if (f2.AGProp.PropertyType == typeof(ushort?) && val2 is uint valuint2)
							{
								if (valuint2 > 65535)
								{
									LogError(exchangeService, $"UrlSearch#{us.UsCle} : erreur insertion enregistrement {ic.Hit.Id}/{ic.Ad.Id} - {f2.AGProp.Name}", $"Valeur trop grande pour '{f2.AGProp.Name}':{valuint2}");
									continue;
								}
								val2 = Convert.ToUInt16(val2);
							}
							if (f2.AGProp.PropertyType == typeof(short?) && val2 is int)
							{
								val2 = Convert.ToInt16(val2);
							}
							if (f2.AGProp.PropertyType == typeof(float?) && val2 is double)
							{
								val2 = Convert.ToSingle(val2);
							}
							f2.AGProp.SetValue(newRecord, val2);
						}
						if (agRecord == null)
						{
							if (us.UsType == 9)
							{
								notExisting.Add(ic.Hit.Id.ToString());
							}
						}
						else
						{
							newRecord.AgRefAgc = agRecord.AgRefAgc;
						}
						newRecord.AgRefMetaMoteur = refMoteur;
						newRecord.AgTime = DateTime.Now;
						dbContext.AnnoncesGlobales.Add(newRecord);
						dbContext.SaveChanges();
						if (newRecord.AgRefAgc == 0)
						{
							if (newRecord.AgRef == 0)
							{
								throw new Exception("Ne devrait pas être 0 !!!");
							}
							newRecord.AgRefAgc = newRecord.AgRef;
							dbContext.SaveChanges();
						}
						stats.Inserted++;
					}
					PhotosAnnonces[] actualPhotos = dbContext.PhotosAnnonces.Where((PhotosAnnonces p) => p.PaIdPropertyYanport == ((object)propId).ToString()).ToArray();
					if (actualPhotos.Length < ic.Hit.Features.Visual.Images.Length)
					{
						dbContext.PhotosAnnonces.RemoveRange(actualPhotos);
						dbContext.SaveChanges();
						string[] images = ic.Hit.Features.Visual.Images;
						foreach (string imgUrl in images)
						{
							dbContext.PhotosAnnonces.Add(new PhotosAnnonces
							{
								PaUrl = imgUrl,
								PaIdPropertyYanport = propId.ToString()
							});
						}
						dbContext.SaveChanges();
					}
				}
				catch (Exception ex)
				{
					LogError(exchangeService, $"UrlSearch#{us.UsCle} : exception insertion enregistrement {ic.Hit.Id}/{ic.Ad.Id}", ex.ToString());
				}
			}
			if (properties.Count <= 0)
			{
				continue;
			}
			try
			{
				string[] propsAsString = properties.Select((Guid p) => p.ToString()).ToArray();
				string[] existing = (from annoncesGlobales in dbContext.AnnoncesGlobales
					where propsAsString.Contains(annoncesGlobales.AgIdPropertyYanport)
					select annoncesGlobales.AgIdPropertyYanport).Distinct().ToArray();
				notExisting.AddRange(propsAsString.Except(existing).Except(notExisting));
			}
			catch (Exception ex2)
			{
				LogError(exchangeService, $"UrlSearch#{us.UsCle} : exception warning requête 9", ex2.ToString());
			}
		}
		try
		{
			if (notExisting.Count > 10)
			{
				LogError(exchangeService, $"UrlSearch#{us.UsCle} : requête type 9 avec des biens n'existant pas", string.Join("<br/>", notExisting));
			}
		}
		catch (Exception ex3)
		{
			LogError(exchangeService, $"UrlSearch#{us.UsCle} : exception warning requête 9", ex3.ToString());
		}
		return stats;
	}

	private static bool AreIdentical(object x, object y)
	{
		if (x == null && y == null)
		{
			return true;
		}
		if (x == null || y == null)
		{
			return false;
		}
		if (x is string sx && y is string sy)
		{
			return sx == sy;
		}
		IComparable c = (IComparable)x;
		return c.CompareTo(y) == 0;
	}

	private static string GetDifferences(ImportContext ic, AnnoncesGlobales agRecord)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("Détail des modifications souhaitées : <br/>");
		foreach (TempField f in _importFields.Where((TempField tempField) => tempField.AGProp != null))
		{
			object actualValue = f.AGProp.GetValue(agRecord);
			object newValue = f.GetValue(ic);
			StringBuilder stringBuilder = sb;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 3, stringBuilder);
			handler.AppendFormatted(f.AGProp.Name);
			handler.AppendLiteral(" : ");
			handler.AppendFormatted(StringVal(actualValue));
			handler.AppendLiteral(" => ");
			handler.AppendFormatted(StringVal(newValue));
			handler.AppendLiteral(" <br/>");
			stringBuilder.AppendLine(ref handler);
		}
		return sb.ToString();
	}

	private static string StringVal(object v)
	{
		if (v == null)
		{
			return "<null>";
		}
		if (v is DateTime dt)
		{
			return dt.ToString("G");
		}
		if (v is string s)
		{
			return "\"" + s + "\"";
		}
		return v.ToString();
	}

	private static void CheckRes(IExchangeService exchangeService, string step, string url, string res)
	{
		if (Regex.IsMatch(res, "err(eu|o)r|warning|notice"))
		{
			LogError(exchangeService, "Erreur intégration données", $"step={step}<br/>url={url}<br/>res={res}");
		}
	}

	public static bool TryGetSource(string crawlSource, out Source source)
	{
		if (crawlSource.EndsWith("_NEUF"))
		{
			crawlSource = crawlSource.Substring(0, crawlSource.Length - 5);
		}
		return _sources.TryGetValue(crawlSource, out source);
	}

	static YanportScheduledTask()
	{
		_unkownSources = new List<string>();
		_sources = new Dictionary<string, Source>();
		List<TempField> list = new List<TempField>();
		AddField(list, (AnnoncesGlobales ag) => ag.AgIdAnnonceYanport, "A_IdAnnonceYanport", 40, (ImportContext ic) => ic.Ad.Id.ToString());
		AddField(list, (AnnoncesGlobales ag) => ag.AgIdPropertyYanport, "A_IdPropertyYanport", 40, (ImportContext ic) => ic.Hit.Id.ToString());
		AddField(list, (AnnoncesGlobales ag) => ag.AgLien, "A_Lien", 255, (ImportContext ic) => ic.Ad.Url);
		AddField(list, (AnnoncesGlobales ag) => ag.AgLienId, "A_LienSimplifie", 255, (ImportContext ic) => ic.GetLienId());
		AddField(list, (AnnoncesGlobales ag) => ag.AgType, "A_Type", 50, (ImportContext ic) => GetType(ic));
		AddField(list, (AnnoncesGlobales ag) => ag.AgImage, "A_Image", 255, (ImportContext ic) => ic.Hit.Features?.Visual?.Images?.FirstOrDefault());
		AddField(list, (AnnoncesGlobales ag) => ag.AgDesc, "A_Desc", 3000, (ImportContext ic) => ic.Hit.Features?.Descriptive?.Description);
		AddField(list, (AnnoncesGlobales ag) => ag.AgAnnonceur, "A_Annonceur", 50, (ImportContext ic) => ic.Dealer?.Name);
		AddField(list, (AnnoncesGlobales ag) => ag.AgTelAnnonceur, "A_TelAnnonceur", 14, (ImportContext ic) => AdjustTel(ic.Dealer?.PhoneNumber));
		AddField(list, (AnnoncesGlobales ag) => ag.AgQuartier, "A_Quartier", 100, (ImportContext ic) => GetQuartier(ic.DbContext, ic.Hit.Address?.QuarterId));
		AddField(list, (AnnoncesGlobales ag) => ag.AgQuartier2, "A_Quartier2", 100, (ImportContext ic) => GetQuartier2(ic.DbContext, ic.Hit.Address?.QuarterId));
		AddField(list, (AnnoncesGlobales ag) => ag.AgListeTags, "A_ListeTags", 100, (ImportContext ic) => GetListeTags(ic));
		AddField(list, null, "A_SiteSource", 20, (ImportContext ic) => ic.Source.NomMoteur);
		AddField(list, (AnnoncesGlobales ag) => ag.AgAdresseNum, "A_AdresseNum", 10, (ImportContext ic) => ic.Hit.Address?.StreetNumber ?? "");
		AddField(list, (AnnoncesGlobales ag) => ag.AgAdresseRue, "A_AdresseRue", 100, (ImportContext ic) => ic.Hit.Address?.Street ?? "");
		AddField(list, (AnnoncesGlobales ag) => ag.AgCp, "A_CP", 10, (ImportContext ic) => ic.Hit.Address.ZipCode);
		AddField(list, (AnnoncesGlobales ag) => ag.AgTypeTransaction, "A_TypeTransaction", 1, (ImportContext ic) => ic.GetTypeTransaction());
		AddField(list, (AnnoncesGlobales ag) => ag.AgAsc, "A_Asc", (ImportContext ic) => ic.Hit.Features?.AdditionalFeatures?.Any(delegate(AdditionalFeature af)
		{
			Features features = af.Features;
			return features != null && features.Descriptive?.Equipments?.Elevator == true;
		}));
		AddField(list, (AnnoncesGlobales ag) => ag.AgIdAgenceYanport, "A_IdAgenceYanport", (ImportContext ic) => ic.Dealer?.Id);
		AddField(list, (AnnoncesGlobales ag) => ag.AgPrix, "A_Prix", (ImportContext ic) => ToInt(ic.Hit.Marketing?.Price));
		AddField(list, (AnnoncesGlobales ag) => ag.AgNbChambres, "A_NbChambres", (ImportContext ic) => ic.Hit.Features?.Geometry?.AreaCount?.Bedroom);
		AddField(list, (AnnoncesGlobales ag) => ag.AgNbPieces, "A_NbPieces", (ImportContext ic) => ic.Hit.Features?.Geometry?.RoomCount);
		AddField(list, (AnnoncesGlobales ag) => ag.AgSurface, "A_Surface", (ImportContext ic) => ToInt(ic.Hit.Features?.Geometry?.Surface));
		AddField(list, (AnnoncesGlobales ag) => ag.AgEtage, "A_Etage", (ImportContext ic) => ic.Hit.Features?.Geometry?.Floors?.FirstOrDefault()?.Level);
		AddField(list, (AnnoncesGlobales ag) => ag.AgEstExclusif, "A_EstExclusif", (ImportContext ic) => (ic.Hit.Marketing?.ExclusiveMandate == true) ?? false);
		AddField(list, (AnnoncesGlobales ag) => ag.AgEstDernierEtage, "A_EstDernierEtage", (ImportContext ic) => EstDernierEtage(ic.Hit.Features));
		AddField(list, (AnnoncesGlobales ag) => ag.AgDateDebut, "A_DateDebut", (ImportContext ic) => GetDateOnly(ic.Hit.Marketing?.PublicationStartDate.ToLocalTime()?.Date));
		AddField(list, (AnnoncesGlobales ag) => ag.AgAnnee, "A_Annee", (ImportContext ic) => ic.Hit.Features?.Construction?.Year);
		AddField(list, (AnnoncesGlobales ag) => ag.AgEstRecent, "A_EstRecent", (ImportContext ic) => ic.Hit.Features?.Construction?.NewBuild);
		AddField(list, (AnnoncesGlobales ag) => ag.AgNbEtages, "A_NbEtages", (ImportContext ic) => ic.Hit.Features?.AdditionalFeatures?.FirstOrDefault((AdditionalFeature af) => af.Type == "BUILDING")?.Features?.Geometry?.FloorCount);
		AddField(list, (AnnoncesGlobales ag) => ag.AgAdresseLat, "A_AdresseLat", (ImportContext ic) => ic.Hit.Address?.Location?.Lat);
		AddField(list, (AnnoncesGlobales ag) => ag.AgAdresseLon, "A_AdresseLon", (ImportContext ic) => ic.Hit.Address?.Location?.Lon);
		AddField(list, (AnnoncesGlobales ag) => ag.AgTimeY, "A_TimeY", (ImportContext ic) => ic.Hit.Marketing?.PublicationStartDate.ToLocalTime());
		AddField(list, (AnnoncesGlobales ag) => ag.AgTimeEndY, "A_TimeEndY", (ImportContext ic) => ic.Hit.Marketing?.PublicationEndDate.ToLocalTime());
		AddField(list, (AnnoncesGlobales ag) => ag.AgEmailAnnonceur, "A_EmailAnnonceur", 100, (ImportContext ic) => ic.Dealer?.Email);
		AddField(list, (AnnoncesGlobales ag) => ag.AgTypeAnnonceur, "A_TypeAnnonceur", 20, (ImportContext ic) => ic.Dealer?.Type);
		AddField(list, (AnnoncesGlobales ag) => ag.AgSsTypeAnnonceur, "A_SsTypeAnnonceur", 20, (ImportContext ic) => ic.Dealer?.SubType);
		AddField(list, (AnnoncesGlobales ag) => ag.AgEstOccupe, "A_EstOccupe", delegate(ImportContext ic)
		{
			Marketing marketing = ic.Hit.Marketing;
			return (marketing != null && marketing.Occupied == true) ? new byte?(1) : new byte?(0);
		});
		AddField(list, (AnnoncesGlobales ag) => ag.AgIdVille, "A_IdVille", (ImportContext ic) => ic.Hit.Address?.CityId);
		AddField(list, (AnnoncesGlobales ag) => ag.AgIdQuartier, "A_IdQuartier", (ImportContext ic) => ic.Hit.Address?.QuarterId);
		AddField(list, (AnnoncesGlobales ag) => ag.AgConsumptionLetter, "A_ConsumptionLetter", (ImportContext ic) => ic.Hit.Features?.Energy?.ConsumptionLetter.Truncate(3));
		AddField(list, (AnnoncesGlobales ag) => ag.AgGreenhouseGasConsumptionLetter, "A_GreenhouseGasConsumptionLetter", (ImportContext ic) => ic.Hit.Features?.Energy?.GreenhouseGasConsumptionLetter.Truncate(3));
		_importFields = list.ToArray();
		_sources.Add("LE_BON_COIN", new Source("leboncoin", "/(?<id>\\d+)\\.htm(?:/|\\?.+)?$"));
		_sources.Add("A_VENDRE_A_LOUER", new Source("avendrealouer", "/fd-(?<id>\\d+)\\.html$"));
		_sources.Add("SE_LOGER", new Source("seloger", "/(?<id>\\d+)(?:/detail)?\\.htm$"));
		_sources.Add("LOGIC_IMMO", new Source("logicimmo", "-(?<id>[a-f0-9]+)\\.htm(?:\\?.+)?$"));
		_sources.Add("MEILLEURS_AGENTS", new Source("meilleursagents", "/(?:annonce-)?(?<id>\\d+)/$"));
		_sources.Add("EXPLORIMMO", new Source("figaroimmo", "-(?<id>\\d+).html$"));
		_sources.Add("BELLES_DEMEURES", new Source("bellesdemeures", "/(?<id>\\d+)(?:\\.htm|/)$"));
		_sources.Add("BIEN_ICI", new Source("bienici", "^https://www.bienici.com/annonce/(?<id>.+)$"));
		_sources.Add("IMMONOT", new Source("immonot", "__(?<id>\\w\\d+)[_\\/]|annonce-immobiliere/(?<id>[a-z0-9]+(?:[-_][a-z0-9]+)*)_*/|detail-annonce/(?<id>\\d+_\\d+)/"));
		_sources.Add("LUX_RESIDENCE", new Source("luxresidence", "/(?<id>[A-Z0-9-]+)/(tt-[\\w-]+)?(?:[?#].+)?$"));
		_sources.Add("PAP", new Source("pap", "-(?<id>r\\d+)"));
		_sources.Add("PARU_VENDU", new Source("paruvendu", "\\/(?<id>\\d+)"));
		_sources.Add("PROPRIETES_DE_FRANCE", new Source("proprietesdefrance", "/(?<id>\\d+(?:-\\d+)?)/$"));
	}

	private static DateOnly GetDateOnly(DateTime? date)
	{
		if (!date.HasValue)
		{
			return default(DateOnly);
		}
		return DateOnly.FromDateTime(date.Value);
	}

	public static string GetListeTags(ImportContext ic)
	{
		string res = "";
		if ((ic.Hit.Features?.Geometry?.AreaCount?.Balcony).GetValueOrDefault() > 0)
		{
			res += "[1]";
		}
		if ((ic.Hit.Features?.Geometry?.AreaCount?.Terrace).GetValueOrDefault() > 0)
		{
			res += "[3]";
		}
		Features features = ic.Hit.Features;
		if (features != null && features.Descriptive?.Equipments?.Furniture == true)
		{
			res += "[43]";
		}
		if ((ic.Hit.Features?.Geometry?.AreaCount?.Parking).GetValueOrDefault() > 0)
		{
			res += "[5]";
		}
		string desc = ic.Hit.Features?.Descriptive?.Description;
		if (ic.DbContext.TagsAnnonces != null && !string.IsNullOrWhiteSpace(desc))
		{
			try
			{
				string typeB = GetType(ic);
				string typeT = ic.GetTypeTransaction();
				TagsAnnonce[] tagsAnnonces = ic.DbContext.TagsAnnonces;
				foreach (TagsAnnonce ta in tagsAnnonces)
				{
					if (ta.TaRef == 1 || ta.TaRef == 3 || ta.TaRef == 5 || ta.TaRef == 43 || !ta.TaTypeT.Contains(typeT, StringComparison.InvariantCultureIgnoreCase) || !ta.TaTypeB.Contains(typeB, StringComparison.InvariantCultureIgnoreCase))
					{
						continue;
					}
					bool ok = false;
					if (ta.TaAlias.StartsWith("+"))
					{
						if (Regex.IsMatch(desc, ta.TaAlias.Substring(1), RegexOptions.IgnoreCase))
						{
							ok = true;
						}
					}
					else if (ta.TaAlias.StartsWith("!"))
					{
						string[] tmp = ta.TaAlias.Substring(1).Split("|");
						ok = true;
						string[] array = tmp;
						foreach (string v in array)
						{
							if (v.StartsWith("!"))
							{
								if (desc.Contains(v.Substring(1), StringComparison.InvariantCultureIgnoreCase))
								{
									ok = false;
								}
							}
							else if (!desc.Contains(v, StringComparison.InvariantCultureIgnoreCase))
							{
								ok = false;
							}
						}
					}
					else if (desc.Contains(ta.TaAlias, StringComparison.InvariantCultureIgnoreCase))
					{
						ok = true;
					}
					if (ok)
					{
						res += $"[{ta.TaRef}]";
					}
				}
			}
			catch (Exception ex)
			{
				ic.LogError("Erreur à la détermination des tags", ex.ToString());
			}
		}
		return res;
	}

	private static string AdjustTel(string phoneNumber)
	{
		if (string.IsNullOrEmpty(phoneNumber))
		{
			return phoneNumber;
		}
		if (phoneNumber[0] == '1' || phoneNumber[0] == '6' || phoneNumber[0] == '7')
		{
			return "0" + phoneNumber;
		}
		return phoneNumber;
	}

	private static string GetQuartier(ImportDbContext idc, long? quarterId)
	{
		if (!quarterId.HasValue)
		{
			return "";
		}
		return idc.GetQuartier(quarterId.Value)?.QTag ?? "";
	}

	private static string GetQuartier2(ImportDbContext idc, long? quarterId)
	{
		if (!quarterId.HasValue)
		{
			return "";
		}
		Quartiers q = idc.GetQuartier(quarterId.Value);
		if (q == null)
		{
			return "";
		}
		return $"[{q.QRef}]";
	}

	private static bool EstDernierEtage(Features features)
	{
		byte? level = features?.Geometry?.Floors?.FirstOrDefault()?.Level;
		if (!level.HasValue)
		{
			return false;
		}
		byte? lastLevel = features?.AdditionalFeatures?.FirstOrDefault((AdditionalFeature af) => af.Type == "BUILDING")?.Features?.Geometry?.Floors?.Max((Floor f) => f.Level);
		if (!lastLevel.HasValue)
		{
			return false;
		}
		return level == lastLevel;
	}

	private static uint? ToInt(decimal? price)
	{
		if (!price.HasValue)
		{
			return null;
		}
		return (uint)price.Value;
	}

	private static uint? ToInt(float? price)
	{
		if (!price.HasValue)
		{
			return null;
		}
		return (uint)price.Value;
	}

	private static string GetType(ImportContext ic)
	{
		switch (ic.Hit.Type)
		{
		case "APARTMENT":
		case "APARTEMENT":
			return "Appartement";
		case "PREMISES":
			return "Locaux pro";
		case "HOUSE":
			return "Maison";
		default:
			return ic.Us.UsBien;
		}
	}

	private static void AddField(List<TempField> list, Expression<Func<AnnoncesGlobales, string>> agProp, string name, int maxLength, Func<ImportContext, string> getVal = null, Func<ImportContext, string> ignore = null)
	{
		list.Add(new TempField<string>(name, agProp, getVal, ignore)
		{
			MaxLength = maxLength
		});
	}

	private static void AddField<TValue>(List<TempField> list, Expression<Func<AnnoncesGlobales, TValue>> agProp, string name, Func<ImportContext, TValue> getVal, Func<ImportContext, string> ignore = null)
	{
		list.Add(new TempField<TValue>(name, agProp, getVal, ignore));
	}
}
