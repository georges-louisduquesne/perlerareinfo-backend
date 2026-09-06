using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ApiPerleRare.Tasks;

public class RetrieveExistingPropertyContacts : AbstractTask
{
	private class PropertyInfos
	{
		public bool IsActif { get; internal set; }

		public string Com { get; internal set; }

		public uint Rate { get; internal set; }

		public string Id { get; internal set; }

		public bool Vu { get; internal set; }

		public ulong AnnonceId { get; internal set; }
	}

	private readonly ApplicationDbContext _dbContext;

	private readonly ILogger<RetrieveExistingPropertyContacts> _logger;

	public override string Description => "Récupération des annonces des contacts";

	public RetrieveExistingPropertyContacts(ApplicationDbContext dbContext, ILogger<RetrieveExistingPropertyContacts> logger)
	{
		_dbContext = dbContext;
		_logger = logger;
	}

	public override void Run()
	{
		_logger.LogInformation("Récupération des contacts actifs...");
		uint[] actifContactRefs = (from contactsRecherche in _dbContext.ContactsRecherche
			where contactsRecherche.CStatut == "CLIENT ACTIF" || contactsRecherche.CStatut == "PROSPECT ACTIF"
			select contactsRecherche.CRefContact).ToArray();
		_logger.LogInformation("Récupération des contacts actifs OK");
		_logger.LogInformation("Récupération des annonces...");
		DbConnection c = _dbContext.Database.GetDbConnection();
		if (c.State != ConnectionState.Open)
		{
			c.Open();
		}
		int added = 0;
		int removed = 0;
		int updated = 0;
		uint[] array = actifContactRefs;
		foreach (uint cref in array)
		{
			_logger.LogInformation($"{cref}...");
			string tableName = $"annonces_refcontact_{cref}";
			if (!c.DoesTableExist(tableName))
			{
				continue;
			}
			List<PropertyInfos> propInfos = new List<PropertyInfos>();
			using (DbCommand cmd = c.CreateCommand())
			{
				cmd.CommandText = "\r\nSELECT DISTINCT ar.A_Actif, ar.A_Com, ar.A_Rate, ag.AG_IdPropertyYanport, ar.A_Date_Aff, ar.A_RefAnn FROM " + tableName + " ar\r\nINNER JOIN annonces_globales ag ON ar.A_RefAnnGlob=ag.AG_Ref\r\nINNER JOIN property p ON ag.AG_IdPropertyYanport=p.P_PropertyId";
				using DbDataReader reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					propInfos.Add(new PropertyInfos
					{
						IsActif = (reader.GetInt32(0) == 1),
						Com = reader.GetString(1),
						Rate = reader.GetFieldValue<uint>(2),
						Id = reader.GetString(3),
						Vu = (reader.GetDateTime(4).Year > 2000),
						AnnonceId = reader.GetFieldValue<ulong>(5)
					});
				}
			}
			foreach (IGrouping<string, PropertyInfos> pis in from p in propInfos
				group p by p.Id)
			{
				PropertyInfos pi = pis.OrderBy((PropertyInfos propertyInfos) => propertyInfos.AnnonceId).First();
				PropertyContact[] pcs = _dbContext.PropertyContact.Where((PropertyContact propertyContact) => propertyContact.PcRefContact == cref && propertyContact.PcPropertyId == pi.Id).ToArray();
				foreach (PropertyContact t in pcs.Skip(1))
				{
					_dbContext.PropertyContact.Remove(t);
					_logger.LogWarning($"Trop de property_contacts pour {cref} et {pi.Id}");
					removed++;
				}
				PropertyContact pc = pcs.FirstOrDefault();
				if (pc == null)
				{
					pc = new PropertyContact
					{
						PcActif = pi.IsActif,
						PcCom = pi.Com,
						PcPropertyId = pi.Id,
						PcRate = pi.Rate,
						PcRefContact = cref,
						PcVu = pi.Vu,
						PcDateAff = DateTime.Now
					};
					_dbContext.PropertyContact.Add(pc);
					added++;
				}
				else if (pc.PcActif != pi.IsActif || pc.PcCom != pi.Com || pc.PcRate != pi.Rate || pc.PcVu != pi.Vu)
				{
					pc.PcActif = pi.IsActif;
					pc.PcCom = pi.Com;
					pc.PcRate = pi.Rate;
					pc.PcVu = pi.Vu;
					updated++;
				}
			}
			_dbContext.SaveChanges();
		}
		_logger.LogInformation("Nb d'enregistrements ajoutés   : {0}", added);
		_logger.LogInformation("Nb d'enregistrements modifiés  : {0}", updated);
		_logger.LogInformation("Nb d'enregistrements supprimés : {0}", removed);
	}
}
