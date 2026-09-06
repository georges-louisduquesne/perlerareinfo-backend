using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ApiPerleRare.Models;
using ApiPerleRare.RecupInfos;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace ApiPerleRare.YanportModels;

public class YanportImporter
{
	private class PropertyContact : PropertyFilterDef
	{
		private class PropertyInfos
		{
			public bool IsActif { get; internal set; }

			public string Com { get; internal set; }

			public uint Rate { get; internal set; }

			public string Id { get; internal set; }

			public bool Vu { get; internal set; }
		}

		private readonly ApplicationDbContext _dbContext;

		private ApiPerleRare.Models.PropertyContact[] _actuals;

		public uint ContactRef { get; }

		public string ConseillerLogin { get; }

		public bool Alerte { get; }

		public PropertyContact(ApplicationDbContext dbContext, uint contactRef, string conseillerLogin, bool alerte, PropertyFilterSearch search)
			: base(search)
		{
			_dbContext = dbContext;
			ContactRef = contactRef;
			ConseillerLogin = conseillerLogin;
			Alerte = alerte;
		}

		public ApiPerleRare.Models.PropertyContact GetActual(string propertyId)
		{
			if (_actuals == null)
			{
				string tableName = $"annonces_refcontact_{ContactRef}";
				DbConnection c = _dbContext.Database.GetDbConnection();
				if (c.State != ConnectionState.Open)
				{
					c.Open();
				}
				if (!c.DoesTableExist(tableName))
				{
					_actuals = new ApiPerleRare.Models.PropertyContact[0];
				}
				else
				{
					List<PropertyInfos> propInfos = new List<PropertyInfos>();
					using (DbCommand cmd = c.CreateCommand())
					{
						cmd.CommandText = "\r\nSELECT DISTINCT ar.A_Actif, ar.A_Com, ar.A_Rate, ag.AG_IdPropertyYanport, ar.A_Date_Aff FROM " + tableName + " ar\r\nINNER JOIN annonces_globales ag ON ar.A_RefAnnGlob=ag.AG_Ref_AGC\r\nINNER JOIN property p ON ag.AG_IdPropertyYanport=p.P_PropertyId";
						using DbDataReader reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							propInfos.Add(new PropertyInfos
							{
								IsActif = (reader.GetInt32(0) == 1),
								Com = reader.GetString(1),
								Rate = reader.GetFieldValue<uint>(2),
								Id = reader.GetString(3),
								Vu = (reader.GetDateTime(4).Year > 2000)
							});
						}
					}
					_actuals = (from pi in propInfos
						group pi by pi.Id into p
						select new ApiPerleRare.Models.PropertyContact
						{
							PcActif = p.Any((PropertyInfos pi) => pi.IsActif),
							PcCom = string.Join(" ", from pi in p
								where !string.IsNullOrEmpty(pi.Com)
								select pi.Com).Trim(),
							PcPropertyId = p.Key,
							PcRate = p.Max((PropertyInfos pi) => pi.Rate),
							PcRefContact = ContactRef,
							PcVu = p.Any((PropertyInfos pi) => pi.Vu)
						}).ToArray();
				}
			}
			return _actuals.FirstOrDefault((ApiPerleRare.Models.PropertyContact a) => a.PcPropertyId == propertyId);
		}
	}

	private class Field
	{
		public PropertyInfo Prop { get; set; }

		public Func<Hit, object> GetVal { get; set; }
	}

	private readonly MySqlConnection _connection;

	private readonly Action<string> _logError;

	private readonly ApplicationDbContext _dbContext;

	private readonly List<Field> _fields = new List<Field>();

	private List<PropertyContact> _contacts = new List<PropertyContact>();

	private List<string> _contactsLoaded = new List<string>();

	private Dictionary<string, uint> _conseillers = new Dictionary<string, uint>();

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

	public YanportImporter(ApplicationDbContext dbContext, Action<string> logError)
	{
		_logError = logError;
		_dbContext = dbContext;
		AddProp((Expression<Func<Property, string>>)((Property p) => p.PPropertyId), (Func<Hit, string>)((Hit h) => h.Id.ToString()));
		AddProp((Property p) => p.PDateCreation, (Hit h) => DateTime.Now);
		AddProp((Property p) => p.PAds, (Hit h) => Serialize(h.Ads, 2000));
		AddProp((Property p) => p.PImages, (Hit h) => Serialize(h.Features?.Visual?.Images ?? Array.Empty<string>(), 5000));
		AddProp((Property p) => p.PType, (Hit h) => GetType(h));
		AddProp((Property p) => p.PAnnonceurs, (Hit h) => AnnonceursHelper.NormalizePhoneNumbers(Serialize(h.Marketing?.Dealers, 1000)));
		AddProp((Property p) => p.PDescription, (Hit h) => h.Features?.Descriptive?.Description, 3000);
		AddProp((Property p) => p.PQuartier, (Hit h) => GetQuartier(h.Address?.QuarterId ?? 0)?.QTag ?? "", 100);
		AddProp((Property p) => p.PQuartier2, (Hit h) => GetQuartier2(h.Address?.QuarterId), 100);
		AddProp((Property p) => p.PListeTags, (Hit h) => GetListeTags(h));
		AddProp((Property p) => p.PAdresseNum, (Hit h) => h.Address?.StreetNumber ?? "", 10);
		AddProp((Property p) => p.PAdresseRue, (Hit h) => h.Address?.Street ?? "", 100);
		AddProp((Property p) => p.PCp, (Hit h) => h.Address?.ZipCode ?? "", 10);
		AddProp((Property p) => p.PTypeTransaction, (Hit h) => GetTypeTransaction(h), 1);
		AddProp((Property p) => p.PAsc, (Hit h) => h.Features?.AdditionalFeatures?.Any(delegate(AdditionalFeature af)
		{
			Features features = af.Features;
			return features != null && features.Descriptive?.Equipments?.Elevator == true;
		}));
		AddProp((Property p) => p.PPrix, (Hit h) => h.Marketing?.Price);
		AddProp((Property p) => p.PNbChambres, (Hit h) => h.Features?.Geometry?.AreaCount?.Bedroom);
		AddProp((Property p) => p.PNbPieces, (Hit h) => h.Features?.Geometry?.RoomCount);
		AddProp((Property p) => p.PSurface, (Hit h) => h.Features?.Geometry?.Surface);
		AddProp((Property p) => p.PEtage, (Hit h) => h.Features?.Geometry?.Floors?.FirstOrDefault()?.Level);
		AddProp((Property p) => p.PEstExclusif, (Hit h) => h.Marketing?.ExclusiveMandate);
		AddProp((Property p) => p.PEstDernierEtage, (Hit h) => EstDernierEtage(h.Features));
		AddProp((Property p) => p.PAnnee, (Hit h) => h.Features?.Construction?.Year);
		AddProp((Property p) => p.PDateDebut, (Hit h) => h.Marketing?.PublicationStartDate.ToLocalTime());
		AddProp((Property p) => p.PDateFin, (Hit h) => h.Marketing?.PublicationEndDate.ToLocalTime());
		AddProp((Property p) => p.PEstRecent, (Hit h) => h.Features?.Construction?.NewBuild);
		AddProp((Property p) => p.PNbEtages, (Hit h) => h.Features?.AdditionalFeatures?.FirstOrDefault((AdditionalFeature af) => af.Type == "BUILDING")?.Features?.Geometry?.FloorCount);
		AddProp((Property p) => p.PAdresseLat, (Hit h) => h.Address?.Location?.Lat);
		AddProp((Property p) => p.PAdresseLon, (Hit h) => h.Address?.Location?.Lon);
		AddProp((Property p) => p.PEstOccupe, (Hit h) => h.Marketing?.Occupied);
		AddProp((Property p) => p.PIdVille, (Hit h) => h.Address?.CityId);
		AddProp((Property p) => p.PIdQuartier, (Hit h) => h.Address?.QuarterId);
		AddProp((Property p) => p.PConsumptionLetter, (Hit h) => h.Features?.Energy?.ConsumptionLetter, 1);
		AddProp((Property p) => p.PGreenhouseGasConsumptionLetter, (Hit h) => h.Features?.Energy?.GreenhouseGasConsumptionLetter, 1);
		AddProp((Property p) => p.PPrixHistorique, (Hit h) => Serialize(h.Marketing?.PriceEvents, 1000));
		AddProp((Property p) => p.PPrixEvol, (Hit h) => GetPrixEvol(h.Marketing?.PriceEvents));
		AddProp((Property p) => p.PDateLastUpdate, (Hit h) => DateTime.Now);
		AddProp((Property p) => p.PState, delegate(Hit h)
		{
			Marketing marketing = h.Marketing;
			return (marketing == null || !marketing.PublicationEndDate.HasValue) ? 1u : 0u;
		});
		_connection = (MySqlConnection)dbContext.Database.GetDbConnection();
		if (_connection.State != ConnectionState.Open)
		{
			_connection.Open();
		}
	}

	private sbyte GetPrixEvol(PriceEvent[] values)
	{
		if (values == null || values.Length <= 1)
		{
			return 0;
		}
		if (values[^1].Price > values[^2].Price)
		{
			return 1;
		}
		return -1;
	}

	public Property Import(Hit hit, Action<string> logger)
	{
		Property property = _dbContext.Property.Find(hit.Id.ToString());
		if (!hit.IsActive)
		{
			if (property != null)
			{
				_dbContext.Property.Remove(property);
			}
			return null;
		}
		bool hasDateFin = hit.Marketing?.PublicationEndDate.HasValue ?? false;
		bool hasChanged = false;
		double? oldPrice = property?.PPrix;
		if (property != null)
		{
			foreach (Field f in _fields.Skip(2))
			{
				object val = f.GetVal(hit);
				object actual = f.Prop.GetValue(property);
				if (!AreEqual(val, actual))
				{
					f.Prop.SetValue(property, val);
					if (f.Prop.Name != "PDateLastUpdate" && f.Prop.Name != "PState")
					{
						hasChanged = true;
					}
				}
			}
		}
		else
		{
			property = new Property();
			foreach (Field f2 in _fields)
			{
				f2.Prop.SetValue(property, f2.GetVal(hit));
			}
			_dbContext.Property.Add(property);
			property.PDateLastUpdate = DateTime.Now;
			_dbContext.SaveChanges();
			if (!hasDateFin)
			{
				hasChanged = true;
			}
		}
		try
		{
			if (property.PState == 1)
			{
				AnnonceursHelper.Check(_connection, property.PPropertyId, hit.Marketing?.Dealers);
			}
		}
		catch (Exception value)
		{
			logger($"Erreur CheckAnnonceurs : {value}");
		}
		if (hasChanged)
		{
			TryToAddToContacts(property, oldPrice, property?.PPrix, logger);
		}
		else
		{
			_dbContext.Database.ExecuteSqlRaw("UPDATE property SET P_DateLastUpdate=CURTIME() WHERE P_PropertyId='" + property.PPropertyId + "'");
		}
		return property;
	}

	private void TryToAddToContacts(Property property, double? oldPrice, double? newPrice, Action<string> logger)
	{
		string key = $"{property.PTypeTransaction}/{property.PType}/{property.PCp}";
		if (!_contactsLoaded.Contains(key))
		{
			logger("Récupération des contacts " + key);
			foreach (ContactsRecherche contact in _dbContext.ContactsRecherche.Where((ContactsRecherche t) => EF.Functions.Like(t.CTypeBien, $"%{property.PType}%") && t.CTypeTransaction == property.PTypeTransaction && EF.Functions.Like(t.CLocalisation, $"%{property.PCp}%") && (t.CStatut == "CLIENT ACTIF" || t.CStatut == "PROSPECT ACTIF")).AsNoTracking())
			{
				try
				{
					_contacts.Add(new PropertyContact(_dbContext, contact.CRefContact, contact.CNomFamilleConseiller, contact.CAlerteMail == 1, new PropertyFilterSearch
					{
						BudgetC = contact.CBudgetC,
						NbChambres = contact.CNbChambres,
						NbPieces = contact.CNbPieces,
						Anciennete = contact.CAnciennete,
						Budget = contact.CBudget,
						DernEtage = contact.CDernEtage,
						Etage = contact.CEtage,
						EvoPrix = contact.CEvoPrix,
						Exclusivite = contact.CExclusivite,
						Localisation = contact.CLocalisation,
						Surface = contact.CSurface,
						Tags = contact.CTags,
						TypeBien = contact.CTypeBien,
						TypeTransaction = contact.CTypeTransaction
					}));
				}
				catch (Exception value)
				{
					if (_logError != null)
					{
						_logError($"Exception filtre contact '{contact.CRefContact}' : {value}");
					}
				}
			}
			_contactsLoaded.Add(key);
		}
		logger("Liste contacts");
		foreach (PropertyContact c in _contacts)
		{
			if (!c.IsMatch(property))
			{
				continue;
			}
			if (_dbContext.PropertyContact.Any((ApiPerleRare.Models.PropertyContact pc) => pc.PcPropertyId == property.PPropertyId && pc.PcRefContact == c.ContactRef))
			{
				if (c.Alerte && oldPrice.HasValue && newPrice.HasValue && oldPrice != newPrice)
				{
					uint refConseiller = GetConseillerRefFromLogin(c.ConseillerLogin);
					if (refConseiller != 0)
					{
						logger("Ajout notif 'modif prix'");
						_dbContext.Notifications.Add(new Notifications
						{
							NCreatedOn = DateTime.Now,
							NNewPrix = PrixToString(newPrice),
							NOldPrix = PrixToString(oldPrice),
							NPropertyId = property.PPropertyId,
							NRefContact = c.ContactRef,
							NType = "MODIF PRIX",
							NState = -2,
							NRefConseiller = refConseiller
						});
					}
				}
				continue;
			}
			logger("Ajout property contact");
			ApiPerleRare.Models.PropertyContact newPropertyContact = new ApiPerleRare.Models.PropertyContact
			{
				PcActif = true,
				PcCom = "",
				PcPropertyId = property.PPropertyId,
				PcRate = 0u,
				PcRefContact = c.ContactRef,
				PcVu = false,
				PcDateAff = DateTime.Now
			};
			_dbContext.PropertyContact.Add(newPropertyContact);
			if (c.Alerte)
			{
				uint refConseiller2 = GetConseillerRefFromLogin(c.ConseillerLogin);
				if (refConseiller2 != 0)
				{
					logger("Ajout notif 'nouveau bien'");
					_dbContext.Notifications.Add(new Notifications
					{
						NCreatedOn = DateTime.Now,
						NNewPrix = PrixToString(newPrice),
						NOldPrix = PrixToString(oldPrice),
						NPropertyId = property.PPropertyId,
						NRefContact = c.ContactRef,
						NType = "NOUVEAU BIEN",
						NState = -2,
						NRefConseiller = refConseiller2
					});
				}
			}
		}
		logger("Save TryToAddToContacts");
		_dbContext.SaveChanges();
	}

	public string PrixToString(double? prix)
	{
		if (!prix.HasValue)
		{
			return null;
		}
		return prix.Value.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "");
	}

	private uint GetConseillerRefFromLogin(string conseillerLogin)
	{
		if (!_conseillers.TryGetValue(conseillerLogin, out var cr))
		{
			cr = (from c in _dbContext.ConseillersPersonnels
				where c.CpLogin == conseillerLogin && (int?)c.CpActif == (int?)1
				select c.CpRefConseiller).FirstOrDefault();
			_conseillers.Add(conseillerLogin, cr);
		}
		return cr;
	}

	private static bool AreEqual(object x, object y)
	{
		if (x == null && y == null)
		{
			return true;
		}
		if (x == null || y == null)
		{
			return false;
		}
		if (x is DateTime dtx && y is DateTime dty)
		{
			return Math.Abs((dtx - dty).TotalSeconds) < 2.0;
		}
		if (x is IComparable xc)
		{
			return xc.CompareTo(y) == 0;
		}
		throw new Exception();
	}

	private static string GetType(Hit hit)
	{
		switch (hit.Type)
		{
		case "APARTMENT":
		case "APARTEMENT":
			return "Appartement";
		case "PREMISES":
			return "Locaux pro";
		case "HOUSE":
			return "Maison";
		default:
			return hit.Type;
		}
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

	public string GetListeTags(Hit h)
	{
		string res = "";
		if ((h.Features?.Geometry?.AreaCount?.Balcony).GetValueOrDefault() > 0)
		{
			res += "[1]";
		}
		if ((h.Features?.Geometry?.AreaCount?.Terrace).GetValueOrDefault() > 0)
		{
			res += "[3]";
		}
		Features features = h.Features;
		if (features != null && features.Descriptive?.Equipments?.Furniture == true)
		{
			res += "[43]";
		}
		if ((h.Features?.Geometry?.AreaCount?.Parking).GetValueOrDefault() > 0)
		{
			res += "[5]";
		}
		string desc = h.Features?.Descriptive?.Description;
		if (TagsAnnonces != null && !string.IsNullOrWhiteSpace(desc))
		{
			try
			{
				string typeB = GetType(h);
				string typeT = GetTypeTransaction(h);
				TagsAnnonce[] tagsAnnonces = TagsAnnonces;
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
			catch
			{
			}
		}
		return res;
	}

	internal string GetTypeTransaction(Hit hit)
	{
		return (hit.Marketing.Type == "SALE") ? "A" : "L";
	}

	private string GetQuartier2(long? quarterId)
	{
		if (!quarterId.HasValue)
		{
			return "";
		}
		Quartiers q = GetQuartier(quarterId.Value);
		if (q == null)
		{
			return "";
		}
		return $"[{q.QRef}]";
	}

	private static bool? EstDernierEtage(Features features)
	{
		byte? level = features?.Geometry?.Floors?.FirstOrDefault()?.Level;
		if (!level.HasValue)
		{
			return null;
		}
		byte? lastLevel = features?.AdditionalFeatures?.FirstOrDefault((AdditionalFeature af) => af.Type == "BUILDING")?.Features?.Geometry?.Floors?.Max((Floor f) => f.Level);
		if (!lastLevel.HasValue)
		{
			return null;
		}
		return level == lastLevel;
	}

	public string Serialize<T>(T[] array, int maxSize)
	{
		if (array == null)
		{
			return "";
		}
		int nb = array.Length;
		string json;
		while (true)
		{
			json = JsonSerializer.Serialize(array.Take(nb).ToArray(), new JsonSerializerOptions
			{
				DefaultIgnoreCondition = (JsonIgnoreCondition)3
			});
			if (json.Length < maxSize)
			{
				break;
			}
			nb--;
		}
		return json;
	}

	public void AddProp<TType>(Expression<Func<Property, TType>> expression, Func<Hit, TType> getValue)
	{
		MemberExpression ma;
		if (expression.Body is MemberExpression)
		{
			ma = (MemberExpression)expression.Body;
		}
		else
		{
			if (!(expression.Body is UnaryExpression ue))
			{
				throw new Exception(expression.Body.GetType().FullName + " non géré");
			}
			ma = (MemberExpression)ue.Operand;
		}
		PropertyInfo pi = (PropertyInfo)ma.Member;
		_fields.Add(new Field
		{
			Prop = pi,
			GetVal = (Hit h) => getValue(h)
		});
	}

	public void AddProp(Expression<Func<Property, string>> expression, Func<Hit, string> getValue, int maxLength)
	{
		AddProp(expression, (Hit h) => getValue(h).Truncate(maxLength));
	}
}
