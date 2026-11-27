using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Application.Abstractions;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Application.Contacts;

public sealed class ListContactsRechercheAccueilUseCase : IListContactsRechercheAccueilUseCase
{
	private readonly IApplicationDbContext _context;

	private readonly IListContactsRechercheExUseCase _listEx;

	public ListContactsRechercheAccueilUseCase(IApplicationDbContext context, IListContactsRechercheExUseCase listEx)
	{
		_context = context;
		_listEx = listEx;
	}

	public async Task<List<ContactsRechercheAccueil>> Execute(string where, string orderby, int skip, int take, int withAnnonces, int withTaches, bool applyFilter, string userLogin)
	{
		ContactsRechercheEx[] res = (await _listEx.Execute(null, where, orderby, skip, take, applyFilter, userLogin, leanForAccueil: true)).ToArray();
		List<ContactsRechercheAccueil> resa = new List<ContactsRechercheAccueil>(res.Length);
		Dictionary<uint, ContactsRechercheAccueil> byId = new Dictionary<uint, ContactsRechercheAccueil>(res.Length);
		foreach (ContactsRechercheEx cr in res)
		{
			ContactsRechercheAccueil cra = new ContactsRechercheAccueil();
			EFHelper<ContactsRechercheEx>.Copy(cr, cra);
			resa.Add(cra);
			byId[cra.CRefContact] = cra;
		}
		if (resa.Count == 0)
		{
			return resa;
		}
		// Do not dispose: connection is owned by the EF context.
		DbConnection c = _context.Database.GetDbConnection();
		if (c.State != System.Data.ConnectionState.Open)
		{
			await c.OpenAsync();
		}
		await FillDoubleCoche(c, resa);
		if (withAnnonces <= 0 && withTaches <= 0)
		{
			return resa;
		}
		if (withAnnonces > 0)
		{
			await FillAnnoncesStats(c, resa, byId);
		}
		if (withTaches > 0)
		{
			await FillTachesStats(c, resa, byId, applyFilter, userLogin);
		}
		return resa;
	}

	private static async Task FillDoubleCoche(DbConnection c, List<ContactsRechercheAccueil> resa)
	{
		string ids = string.Join(",", resa.Select((ContactsRechercheAccueil x) => x.CRefContact));
		HashSet<uint> closing = new HashSet<uint>();
		await using DbCommand cmd = c.CreateCommand();
		cmd.CommandText = ClientDoubleCoche.BuildSql(ids);
		await using DbDataReader reader = await cmd.ExecuteReaderAsync();
		while (await reader.ReadAsync())
		{
			if (ClientDoubleCoche.TryReadContactId(reader.GetValue(0), out uint contactId))
			{
				closing.Add(contactId);
			}
		}
		ClientDoubleCoche.Apply(resa, closing);
	}

	internal static string BuildAnnoncesStatsSql(string ids)
	{
		return $@"
SELECT pc.PC_RefContact,
  COUNT(DISTINCT CASE WHEN pc.PC_Vu = 0 THEN COALESCE(NULLIF(p.P_DoublonPropertyId, ''), p.P_PropertyId) END) AS nonlus,
  MIN(p.P_DateCreation) AS max_aff,
  COUNT(DISTINCT CASE WHEN pc.PC_Vu = 1 THEN COALESCE(NULLIF(p.P_DoublonPropertyId, ''), p.P_PropertyId) END) AS lus,
  COUNT(DISTINCT CASE WHEN pc.PC_Rate = 1 THEN COALESCE(NULLIF(p.P_DoublonPropertyId, ''), p.P_PropertyId) END) AS demi,
  COUNT(DISTINCT CASE WHEN pc.PC_Rate = 2 THEN COALESCE(NULLIF(p.P_DoublonPropertyId, ''), p.P_PropertyId) END) AS complete,
  COUNT(DISTINCT COALESCE(NULLIF(p.P_DoublonPropertyId, ''), p.P_PropertyId)) AS total
FROM property_contact pc
INNER JOIN property p ON pc.PC_PropertyId=p.P_PropertyId
WHERE pc.PC_RefContact IN ({ids}) AND p.P_DateFin IS NULL AND pc.PC_Actif=1
GROUP BY pc.PC_RefContact";
	}

	private static async Task FillAnnoncesStats(DbConnection c, List<ContactsRechercheAccueil> resa, Dictionary<uint, ContactsRechercheAccueil> byId)
	{
		string ids = string.Join(",", resa.Select((ContactsRechercheAccueil x) => x.CRefContact));
		await using DbCommand cmd = c.CreateCommand();
		cmd.CommandText = BuildAnnoncesStatsSql(ids);
		await using DbDataReader reader = await cmd.ExecuteReaderAsync();
		while (await reader.ReadAsync())
		{
			uint contactId = Convert.ToUInt32(reader.GetValue(0));
			if (!byId.TryGetValue(contactId, out ContactsRechercheAccueil cra))
			{
				continue;
			}
			if (!reader.IsDBNull(1))
			{
				int nbAnnoncesNonLues = (cra._NbBiensNonLues = reader.GetInt32(1));
				cra._NbAnnoncesNonLues = nbAnnoncesNonLues;
			}
			if (!reader.IsDBNull(2))
			{
				DateTime? annoncesNonLuesDate = (cra._BiensNonLuesDate = reader.GetDateTime(2));
				cra._AnnoncesNonLuesDate = annoncesNonLuesDate;
			}
			if (!reader.IsDBNull(3))
			{
				cra._NbBiensLus = reader.GetInt32(3);
			}
			if (!reader.IsDBNull(4))
			{
				cra._NbBiensDemiEtoile = reader.GetInt32(4);
			}
			if (!reader.IsDBNull(5))
			{
				cra._NbBiensEtoile = reader.GetInt32(5);
			}
		}
	}

	private static async Task FillTachesStats(DbConnection c, List<ContactsRechercheAccueil> resa, Dictionary<uint, ContactsRechercheAccueil> byId, bool applyFilter, string userLogin)
	{
		string ids = string.Join(",", resa.Select((ContactsRechercheAccueil x) => x.CRefContact));
		string filter = (applyFilter ? (" AND T_Qui='" + userLogin + "'") : "");
		await using DbCommand cmd2 = c.CreateCommand();
		cmd2.CommandText = $"SELECT T_Ref_Contact, MIN(T_Date_Realisation) AS Date, COUNT(*) AS nbTaches \r\n                        FROM taches  \r\n                        WHERE T_Ref_Contact IN ({ids}) AND (T_Etat <> 'fait') {filter}\r\n                        GROUP BY t_ref_contact";
		await using DbDataReader reader2 = await cmd2.ExecuteReaderAsync();
		while (await reader2.ReadAsync())
		{
			object idObj = reader2.GetValue(0);
			uint id;
			if (idObj is int i2)
			{
				id = (uint)i2;
			}
			else if (idObj is uint ui)
			{
				id = ui;
			}
			else if (idObj is long l)
			{
				id = (uint)l;
			}
			else
			{
				continue;
			}
			if (!byId.TryGetValue(id, out ContactsRechercheAccueil r))
			{
				continue;
			}
			int nbTaches = reader2.GetInt32(2);
			r._AvecTaches = nbTaches > 0;
			if (!reader2.IsDBNull(1) && !(reader2.GetDateTime(1) > DateTime.Now))
			{
				r._NbTaches = nbTaches;
			}
		}
	}
}
