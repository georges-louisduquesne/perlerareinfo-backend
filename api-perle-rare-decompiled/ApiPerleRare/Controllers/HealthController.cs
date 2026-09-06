using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using ApiPerleRare.Predicates;
using ApiPerleRare.YanportModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HealthController : ControllerBase
{
	public class Difference
	{
		public PropertyContact PC { get; set; }

		public AnnoncesRefContactInfo AR { get; set; }

		public AnnoncesRefContactProperty[] ARS { get; set; }
	}

	public class AnnoncesRefContactProperty
	{
		private AnnoncesRefContactInfo[] _annonces;

		public uint AGRefAGC { get; set; }

		public AnnoncesRefContactInfo[] Annonces
		{
			get
			{
				return _annonces;
			}
			set
			{
				_annonces = value;
				MainAnnonce = _annonces.SingleOrDefault((AnnoncesRefContactInfo a) => a.AGRef == a.AGRefAGC);
				MainAnnonce = _annonces.OrderBy((AnnoncesRefContactInfo a) => a.AGRef).First();
			}
		}

		public string PropertyId => MainAnnonce.AGIdPropertyYanport;

		public AnnoncesRefContactInfo MainAnnonce { get; set; }
	}

	public class AnnoncesRefContactInfo
	{
		public string AGIdPropertyYanport { get; set; }

		public long ARefAnn { get; set; }

		public uint AGRef { get; set; }

		public uint AGRefAGC { get; set; }

		public DateTime AGTime { get; set; }

		public bool AActif { get; set; }

		public string ACom { get; set; }

		public uint ARate { get; set; }

		public DateTime ADateAff { get; set; }

		public bool Vu => ADateAff.Year > 2000;
	}

	public class HealthResult
	{
		public int ContactsInError { get; set; }

		public int ContactsOK { get; internal set; }

		public AbstractHealth[] Errors { get; set; }

		public AnnoncesContactsHealth[] Warnings { get; set; }
	}

	public class TaskToUpdate
	{
		public int TRef { get; set; }

		public string PId { get; set; }
	}

	public class EventToUpdate
	{
		public int ERef { get; set; }

		public string PId { get; set; }
	}

	[JsonDerivedType(typeof(EvenementsHealth))]
	[JsonDerivedType(typeof(AnnoncesContactsHealth))]
	public abstract class AbstractHealth
	{
	}

	public class AnnoncesContactsHealth : AbstractHealth
	{
		public uint ContactId { get; set; }

		public int? MissingInPropertyContact { get; set; }

		public int? MissingInAnnoncesRefcontact { get; set; }

		public string Remark { get; set; }

		public Difference[] DifferenceDetails { get; set; }

		public int? Differences { get; set; }

		public string[] MissingInAnnoncesRefcontactIds { get; set; }

		public string[] MissingInPropertyContactIds { get; set; }

		public AnnoncesGlobales[] MissingInAnnoncesRefcontactDetails { get; set; }

		public Property[] MissingInAnnoncesRefcontactProperties { get; set; }

		public string[] Fixes { get; set; }

		public PropertyContact[] WithoutAdsProperties { get; set; }

		public int? WithoutAds { get; set; }

		public TaskToUpdate[] Tasks { get; set; }
	}

	public class EvenementsHealth : AbstractHealth
	{
		public EventToUpdate[] Evenements { get; set; }
	}

	private readonly ApplicationDbContext _context;

	public HealthController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	[Route("AnnoncesRefcontacts")]
	public HealthResult GetAnnoncesContactsHealths([FromQuery] string action = null)
	{
		return GetAnnoncesContactsHealths(0L, action);
	}

	[HttpGet]
	[Route("AnnoncesRefcontacts/{contactRef}")]
	public HealthResult GetAnnoncesContactsHealths(long contactRef, [FromQuery] string action = null)
	{
		uint[] contactIds = (from contactsRecherche in _context.ContactsRecherche
			where (long)contactsRecherche.CRefContact == contactRef || (contactRef == 0 && (contactsRecherche.CStatut == "CLIENT ACTIF" || contactsRecherche.CStatut == "PROSPECT ACTIF"))
			orderby contactsRecherche.CDateCreation descending, contactsRecherche.CNomFamille
			select contactsRecherche.CRefContact).ToArray();
		List<AbstractHealth> list = new List<AbstractHealth>();
		List<AnnoncesContactsHealth> warnings = new List<AnnoncesContactsHealth>();
		try
		{
			DateTime minDate = new DateTime(2023, 12, 1);
			using DbConnection c = _context.Database.GetDbConnection();
			c.Open();
			uint[] array = contactIds;
			foreach (uint contactId in array)
			{
				AnnoncesContactsHealth h = new AnnoncesContactsHealth
				{
					ContactId = contactId
				};
				string tableName = $"annonces_refcontact_{contactId}";
				AnnoncesRefContactProperty[] annoncesRefContactProperties;
				if (!c.DoesTableExist(tableName))
				{
					h.Remark = "Aucune table '" + tableName + "'";
					annoncesRefContactProperties = Array.Empty<AnnoncesRefContactProperty>();
				}
				else
				{
					AnnoncesRefContactInfo[] refContactsProperties;
					using (DbCommand cmd = c.CreateCommand())
					{
						cmd.CommandText = "\r\nSELECT DISTINCT ag.AG_IdPropertyYanport, ag.AG_Ref, ag.AG_Ref_AGC, ar.A_RefAnn, ar.A_Actif, ar.A_Com, ar.A_Rate, ar.A_Date_Aff, ag.AG_Time FROM " + tableName + " ar\r\nINNER JOIN annonces_globales ag ON ar.A_RefAnnGlob=ag.AG_Ref";
						using DbDataReader reader = cmd.ExecuteReader();
						DataReaderHelper<AnnoncesRefContactInfo> drh = new DataReaderHelper<AnnoncesRefContactInfo>(reader);
						refContactsProperties = drh.ReadAll();
					}
					annoncesRefContactProperties = (from annoncesRefContactInfo in refContactsProperties
						group annoncesRefContactInfo by annoncesRefContactInfo.AGRefAGC into g
						select new AnnoncesRefContactProperty
						{
							AGRefAGC = g.Key,
							Annonces = g.ToArray()
						}).ToArray();
					using DbCommand cmd2 = c.CreateCommand();
					cmd2.CommandText = $"SELECT t.T_Ref AS tRef,ag.AG_IdPropertyYanport AS pId FROM taches t \r\nINNER JOIN {tableName} ar ON t.T_Ref_Annonce=ar.A_RefAnn\r\nINNER JOIN annonces_globales ag ON ar.A_RefAnnGlob=ag.AG_Ref\r\nWHERE t.T_Ref_Contact={contactId} AND t.T_PropertyId IS NULL and ag.AG_IdPropertyYanport is not null";
					using DbDataReader reader2 = cmd2.ExecuteReader();
					DataReaderHelper<TaskToUpdate> drh2 = new DataReaderHelper<TaskToUpdate>(reader2);
					h.Tasks = drh2.ReadAll();
				}
				PropertyContact[] propertyContactInfos = _context.PropertyContact.Where((PropertyContact propertyContact) => propertyContact.PcRefContact == contactId).Include((PropertyContact propertyContact) => propertyContact.PcProperty).AsNoTracking()
					.ToArray();
				PropertyContact[] propertiesWithoutAds = propertyContactInfos.Where((PropertyContact propertyContact) => HasNoAds(propertyContact.PcProperty)).ToArray();
				if (propertiesWithoutAds.Length != 0)
				{
					if (contactRef != 0)
					{
						h.WithoutAdsProperties = propertiesWithoutAds;
					}
					else
					{
						h.WithoutAds = propertiesWithoutAds.Length;
					}
					propertyContactInfos = propertyContactInfos.Except(propertiesWithoutAds).ToArray();
				}
				List<Difference> differences = new List<Difference>();
				PropertyContact[] array2 = propertyContactInfos;
				foreach (PropertyContact pc in array2)
				{
					AnnoncesRefContactProperty[] ars = annoncesRefContactProperties.Where((AnnoncesRefContactProperty annoncesRefContactProperty) => annoncesRefContactProperty.PropertyId == pc.PcPropertyId).ToArray();
					if (ars.Length == 0)
					{
						continue;
					}
					if (ars.Length >= 2)
					{
						h.Remark = "Annonces_refcontacts avec différentes valeurs liées au dédoublonnage (" + pc.PcPropertyId + ")";
						differences.Add(new Difference
						{
							PC = pc,
							ARS = ars
						});
						continue;
					}
					AnnoncesRefContactInfo ar = ars[0].MainAnnonce;
					if (ar.Vu != pc.PcVu || ar.AActif != pc.PcActif || ar.ACom != pc.PcCom || ar.ARate != pc.PcRate)
					{
						differences.Add(new Difference
						{
							PC = pc,
							AR = ar
						});
					}
				}
				if (contactRef == 0)
				{
					h.Differences = differences.Count;
				}
				else
				{
					h.DifferenceDetails = differences.ToArray();
				}
				if (contactRef != 0)
				{
					h.MissingInPropertyContactIds = (from annoncesRefContactProperty in annoncesRefContactProperties
						where annoncesRefContactProperty.MainAnnonce.AGTime > minDate
						select annoncesRefContactProperty.PropertyId).Except(propertyContactInfos.Select((PropertyContact propertyContact) => propertyContact.PcPropertyId)).ToArray();
				}
				else
				{
					h.MissingInPropertyContact = (from annoncesRefContactProperty in annoncesRefContactProperties
						where annoncesRefContactProperty.MainAnnonce.AGTime > minDate
						select annoncesRefContactProperty.PropertyId).Except(propertyContactInfos.Select((PropertyContact propertyContact) => propertyContact.PcPropertyId)).Count();
				}
				if (contactRef != 0)
				{
					h.MissingInAnnoncesRefcontactIds = (from propertyContact in propertyContactInfos
						where propertyContact.PcProperty.PDoublonPropertyId == null
						select propertyContact.PcPropertyId).Except(annoncesRefContactProperties.Select((AnnoncesRefContactProperty annoncesRefContactProperty) => annoncesRefContactProperty.PropertyId)).ToArray();
				}
				else
				{
					h.MissingInAnnoncesRefcontact = (from propertyContact in propertyContactInfos
						where propertyContact.PcProperty.PDoublonPropertyId == null
						select propertyContact.PcPropertyId).Except(annoncesRefContactProperties.Select((AnnoncesRefContactProperty annoncesRefContactProperty) => annoncesRefContactProperty.PropertyId)).Count();
				}
				if (h.Differences == 0)
				{
					h.Differences = null;
				}
				Difference[] differenceDetails = h.DifferenceDetails;
				if (differenceDetails != null && differenceDetails.Length == 0)
				{
					h.DifferenceDetails = null;
				}
				if (h.MissingInPropertyContact == 0)
				{
					h.MissingInPropertyContact = null;
				}
				string[] missingInPropertyContactIds = h.MissingInPropertyContactIds;
				if (missingInPropertyContactIds != null && missingInPropertyContactIds.Length == 0)
				{
					h.MissingInPropertyContactIds = null;
				}
				if (h.MissingInAnnoncesRefcontact == 0)
				{
					h.MissingInAnnoncesRefcontact = null;
				}
				string[] missingInAnnoncesRefcontactIds = h.MissingInAnnoncesRefcontactIds;
				if (missingInAnnoncesRefcontactIds != null && missingInAnnoncesRefcontactIds.Length == 0)
				{
					h.MissingInAnnoncesRefcontactIds = null;
				}
				if (h.MissingInAnnoncesRefcontactIds != null)
				{
					h.MissingInAnnoncesRefcontactDetails = _context.AnnoncesGlobales.Where((AnnoncesGlobales a) => a.AgTime > minDate && h.MissingInAnnoncesRefcontactIds.Contains(a.AgIdPropertyYanport)).ToArray();
					h.MissingInAnnoncesRefcontactProperties = _context.Property.Where((Property a) => h.MissingInAnnoncesRefcontactIds.Contains(a.PPropertyId)).ToArray();
				}
				if (action == "fix")
				{
					List<string> fixes = new List<string>();
					PropertyContact[] propertiesTooMuch = (from propertyContact in propertyContactInfos
						where propertyContact.PcProperty.PDoublonPropertyId == null
						where !annoncesRefContactProperties.Any((AnnoncesRefContactProperty rc) => rc.PropertyId == propertyContact.PcPropertyId)
						select propertyContact).ToArray();
					PropertyContact[] array3 = propertiesTooMuch;
					foreach (PropertyContact p in array3)
					{
						AnnoncesRefContactProperty doublon = annoncesRefContactProperties.FirstOrDefault((AnnoncesRefContactProperty annoncesRefContactProperty) => annoncesRefContactProperty.Annonces.Any((AnnoncesRefContactInfo a) => a.AGIdPropertyYanport == annoncesRefContactProperty.PropertyId));
						if (doublon != null)
						{
							Property prop = _context.Property.Find(p.PcPropertyId);
							prop.PDoublonPropertyId = doublon.PropertyId;
							_context.SaveChanges();
							fixes.Add("Property#" + p.PcPropertyId + " dédoublonné avec " + doublon.PropertyId);
							continue;
						}
						PropertyContact pc2 = _context.PropertyContact.Find(p.PcId);
						_context.PropertyContact.Remove(pc2);
						_context.SaveChanges();
						fixes.Add($"PropertyContact#{p.PcId} lié à {p.PcPropertyId} supprimé");
					}
					foreach (Difference d in differences)
					{
						if (d.AR != null)
						{
							PropertyContact pc3 = _context.PropertyContact.Find(d.PC.PcId);
							pc3.PcRate = d.AR.ARate;
							pc3.PcVu = d.AR.Vu;
							pc3.PcCom = d.AR.ACom;
							pc3.PcActif = d.AR.AActif;
							_context.SaveChanges();
							fixes.Add($"PropertyContact#{pc3.PcId} mis à jour");
						}
					}
					AnnoncesRefContactProperty[] array4 = annoncesRefContactProperties;
					foreach (AnnoncesRefContactProperty prop2 in array4)
					{
						AnnoncesRefContactInfo[] annonces = prop2.Annonces;
						foreach (AnnoncesRefContactInfo an in annonces)
						{
							if (prop2.MainAnnonce != an && (an.AActif != prop2.MainAnnonce.AActif || an.ARate != prop2.MainAnnonce.ARate || !(an.ADateAff == prop2.MainAnnonce.ADateAff) || !(an.ACom == prop2.MainAnnonce.ACom)))
							{
								fixes.Add($"Annonce#{an.ARefAnn} alignée sur l'annonce#{prop2.MainAnnonce.ARefAnn}");
							}
						}
					}
					if (h.Tasks != null && h.Tasks.Length != 0)
					{
						TaskToUpdate[] tasks = h.Tasks;
						foreach (TaskToUpdate t in tasks)
						{
							using DbCommand cmd3 = c.CreateCommand();
							cmd3.CommandText = $"UPDATE taches SET T_PropertyId='{t.PId}' WHERE T_Ref={t.TRef}";
							cmd3.ExecuteNonQuery();
						}
						fixes.Add($"{h.Tasks.Length} tâches corrigées");
					}
					else
					{
						h.Tasks = null;
					}
					if (fixes.Count > 0)
					{
						h.Fixes = fixes.ToArray();
					}
				}
				if (h.MissingInAnnoncesRefcontact.HasValue || h.MissingInAnnoncesRefcontactIds != null || h.MissingInPropertyContact.HasValue || h.MissingInPropertyContactIds != null || h.Differences.HasValue || h.DifferenceDetails != null || h.Fixes != null || h.Tasks != null)
				{
					list.Add(h);
				}
				if (h.WithoutAds.HasValue || h.WithoutAdsProperties != null)
				{
					warnings.Add(h);
				}
			}
		}
		catch (Exception ex)
		{
			list.Add(new AnnoncesContactsHealth
			{
				Remark = ex.ToString()
			});
		}
		return new HealthResult
		{
			ContactsOK = contactIds.Length - list.Count,
			ContactsInError = list.Count,
			Errors = list.ToArray(),
			Warnings = warnings.ToArray()
		};
	}

	[HttpGet]
	[Route("Evenements")]
	public HealthResult GetEvenementsHealths([FromQuery] string action = null)
	{
		List<AbstractHealth> list = new List<AbstractHealth>();
		try
		{
			using DbConnection c = _context.Database.GetDbConnection();
			c.Open();
			EventToUpdate[] eventToUpdates;
			using (DbCommand cmd = c.CreateCommand())
			{
				cmd.CommandText = "SELECT DISTINCT e.E_RefEvenement AS eRef, ag.AG_IdPropertyYanport AS pId FROM evenements e\r\nINNER JOIN annonces_globales ag ON e.E_RefAnnAGC=ag.AG_Ref_AGC\r\nWHERE ag.AG_IdPropertyYanport IS NOT NULL AND e.E_PropertyId IS NULL";
				using DbDataReader reader = cmd.ExecuteReader();
				DataReaderHelper<EventToUpdate> drh = new DataReaderHelper<EventToUpdate>(reader);
				eventToUpdates = drh.ReadAll();
			}
			if (eventToUpdates.Length != 0)
			{
				list.Add(new EvenementsHealth
				{
					Evenements = eventToUpdates
				});
				if (action == "fix")
				{
					EventToUpdate[] array = eventToUpdates;
					foreach (EventToUpdate e in array)
					{
						using DbCommand cmd2 = c.CreateCommand();
						cmd2.CommandText = $"UPDATE evenements SET E_PropertyId='{e.PId}' WHERE E_RefEvenement={e.ERef}";
						cmd2.ExecuteNonQuery();
					}
				}
			}
		}
		catch (Exception ex)
		{
			list.Add(new AnnoncesContactsHealth
			{
				Remark = ex.ToString()
			});
		}
		return new HealthResult
		{
			Errors = list.ToArray()
		};
	}

	[HttpGet]
	[Route("ContactUrlSearches/{contactRef}")]
	public UrlSearch[] GetUrlSearchFromContact(long contactRef)
	{
		var info = (from c in _context.ContactsRecherche
			where (long)c.CRefContact == contactRef
			select new { c.CCodePostal, c.CTypeTransaction, c.CTypeBien }).Single();
		string transaction = ((info.CTypeTransaction == "A") ? "Achat" : "Location");
		string[] typeBiens = info.CTypeBien.Split(',');
		return _context.UrlSearch.Where((UrlSearch u) => u.UsTransaction == transaction && u.UsLocalite == info.CCodePostal && typeBiens.Contains(u.UsBien) && ((int?)u.UsType == (int?)9 || (int?)u.UsType == (int?)10) && u.UsSuspendu == true).AsNoTracking().ToArray();
	}

	[HttpGet]
	[Route("Migrate")]
	public string Migrate()
	{
		var missing = (from p in _context.Property
			where p.PPrixEvol == (sbyte?)null
			select new { p.PPropertyId, p.PPrixHistorique }).ToArray();
		var array = missing;
		foreach (var m in array)
		{
			PriceEvent[] values = JsonSerializer.Deserialize<PriceEvent[]>(m.PPrixHistorique);
			sbyte res = (sbyte)((values != null && values.Length > 1) ? ((values[^1].Price > values[^2].Price) ? 1 : (-1)) : 0);
			_context.Database.ExecuteSqlRaw($"UPDATE property SET P_PrixEvol={res} WHERE P_PropertyId='{m.PPropertyId}'");
		}
		return $"Nb de PPrixEvol spécifié : {missing.Length}";
	}

	[HttpGet]
	[Route("NormalizePhoneNumbers")]
	public string NormalizePhoneNumbers()
	{
		int nb = 0;
		int readed = 0;
		using (DbConnection c = _context.Database.GetDbConnection())
		{
			c.Open();
			Dictionary<string, string> updates = new Dictionary<string, string>();
			using (DbCommand cmd = c.CreateCommand())
			{
				cmd.CommandText = "SELECT P_PropertyId, P_Annonceurs FROM property WHERE P_Annonceurs LIKE '%PhoneNumber%'";
				using DbDataReader reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					readed++;
					string annonceurs = reader.GetString(1);
					string normalized = AnnonceursHelper.NormalizePhoneNumbers(annonceurs);
					if (!(normalized == annonceurs))
					{
						updates.Add(reader.GetString(0), normalized);
						if (updates.Count > 50000)
						{
							break;
						}
					}
				}
			}
			foreach (KeyValuePair<string, string> u in updates)
			{
				using MySqlCommand cmd2 = (MySqlCommand)c.CreateCommand();
				cmd2.CommandText = "UPDATE property SET P_Annonceurs=@annonceurs WHERE P_PropertyId=@id";
				cmd2.Parameters.AddWithValue("@annonceurs", u.Value);
				cmd2.Parameters.AddWithValue("@id", u.Key);
				nb += cmd2.ExecuteNonQuery();
			}
		}
		return $"{nb} enregistrements modifiés, {readed} enregistrements vérifiés";
	}

	[HttpGet]
	[Route("FixEncodings")]
	public string FixEncodings()
	{
		StringBuilder sb = new StringBuilder();
		FixEncoding(_context.ContactIntermediaire, sb);
		FixEncoding(_context.ContactsRecherche, sb);
		return sb.ToString();
	}

	private void FixEncoding<T>(DbSet<T> dbSet, StringBuilder logs) where T : class
	{
		PropertyInfo[] props = (from propertyInfo in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty)
			where propertyInfo.PropertyType == typeof(string)
			select propertyInfo).ToArray();
		string[] forbidden = EncodingHelper.Forbidden.Except(new string[1] { "ÃŠ" }).ToArray();
		PropertyInfo[] array = props;
		foreach (PropertyInfo p in array)
		{
			string where = string.Join(" or ", forbidden.Select((string f) => p.Name + " like '%" + f + "%'"));
			IValue val = Parser.Parse(where);
			if (!(val is IPredicateValue))
			{
				throw new Exception("'" + where + "' doit être un filtre/prédicat");
			}
			ParameterExpression param = Expression.Parameter(typeof(T));
			Expression exp = val.Eval(param);
			IQueryable<T> query = dbSet.Where(Expression.Lambda<Func<T, bool>>(exp, new ParameterExpression[1] { param }));
			T[] recs = query.ToArray();
			if (recs.Length == 0)
			{
				continue;
			}
			int nb = 0;
			T[] array2 = recs;
			foreach (T r in array2)
			{
				if (EncodingHelper.FixEncodingInStringProperties(r))
				{
					nb++;
				}
				if (nb >= 1000)
				{
					break;
				}
			}
			int updated = _context.SaveChanges();
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 4, logs);
			handler.AppendFormatted(typeof(T).Name);
			handler.AppendLiteral("/");
			handler.AppendFormatted(p.Name);
			handler.AppendLiteral(" : ");
			handler.AppendFormatted(updated);
			handler.AppendLiteral("/");
			handler.AppendFormatted(recs.Length);
			logs.AppendLine(ref handler);
		}
	}

	private bool HasNoAds(Property property)
	{
		if (property == null)
		{
			return true;
		}
		Ad[] ads = JsonSerializer.Deserialize<Ad[]>(property.PAds);
		return ads.Length == 0 || ads.All((Ad a) => a.Id == Guid.Empty);
	}
}
