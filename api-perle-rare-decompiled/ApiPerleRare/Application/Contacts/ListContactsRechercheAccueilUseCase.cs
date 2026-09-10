using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using ApiPerleRare.Application.Abstractions;

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
		ContactsRechercheEx[] res = (await _listEx.Execute(null, where, orderby, skip, take, applyFilter, userLogin)).ToArray();
		List<ContactsRechercheAccueil> resa = new List<ContactsRechercheAccueil>();
		using DbConnection c = _context.Database.GetDbConnection();
		c.Open();
		ContactsRechercheEx[] array = res;
		foreach (ContactsRechercheEx cr in array)
		{
			ContactsRechercheAccueil cra = new ContactsRechercheAccueil();
			EFHelper<ContactsRechercheEx>.Copy(cr, cra);
			resa.Add(cra);
			if (withAnnonces <= 0)
			{
				continue;
			}
			using DbCommand cmd = c.CreateCommand();
			cmd.CommandText = $"\r\nSELECT SUM(1-pc.PC_Vu) AS nonlus, MIN(p.P_DateCreation) AS max_aff, SUM(pc.PC_Vu) AS lus, SUM(case when pc.PC_Rate=1 then 1 ELSE 0 END) AS demi, SUM(case when pc.PC_Rate=2 then 1 ELSE 0 END) AS complete, COUNT(*) AS total FROM property_contact pc\r\nINNER JOIN property p ON pc.PC_PropertyId=p.P_PropertyId\r\n WHERE pc.PC_RefContact={cr.CRefContact} AND p.P_DateFin IS NULL AND pc.PC_Actif=1\r\n";
			using DbDataReader reader = await cmd.ExecuteReaderAsync();
			if (reader.Read())
			{
				if (!reader.IsDBNull(0))
				{
					int nbAnnoncesNonLues = (cra._NbBiensNonLues = reader.GetInt32(0));
					cra._NbAnnoncesNonLues = nbAnnoncesNonLues;
				}
				if (!reader.IsDBNull(1))
				{
					DateTime? annoncesNonLuesDate = (cra._BiensNonLuesDate = reader.GetDateTime(1));
					cra._AnnoncesNonLuesDate = annoncesNonLuesDate;
				}
				if (!reader.IsDBNull(2))
				{
					cra._NbBiensLus = reader.GetInt32(2);
				}
				if (!reader.IsDBNull(3))
				{
					cra._NbBiensDemiEtoile = reader.GetInt32(3);
				}
				if (!reader.IsDBNull(4))
				{
					cra._NbBiensEtoile = reader.GetInt32(4);
				}
			}
		}
		if (resa.Count > 0 && withTaches > 0)
		{
			string ids = string.Join(",", resa.Select((ContactsRechercheAccueil contactsRechercheAccueil) => contactsRechercheAccueil.CRefContact));
			string filter = (applyFilter ? (" AND T_Qui='" + userLogin + "'") : "");
			using DbCommand cmd2 = c.CreateCommand();
			cmd2.CommandText = $"SELECT T_Ref_Contact, MIN(T_Date_Realisation) AS Date, COUNT(*) AS nbTaches \r\n                        FROM taches  \r\n                        WHERE T_Ref_Contact IN ({ids}) AND (T_Etat <> 'fait') {filter}\r\n                        GROUP BY t_ref_contact";
			using DbDataReader reader2 = await cmd2.ExecuteReaderAsync();
			int i2 = default(int);
			uint ui = default(uint);
			while (reader2.Read())
			{
				object idObj = reader2.GetValue(0);
				int num;
				if (idObj is int)
				{
					i2 = (int)idObj;
					num = 1;
				}
				else
				{
					num = 0;
				}
				uint id;
				if (num != 0)
				{
					id = (uint)i2;
				}
				else
				{
					int num2;
					if (idObj is uint)
					{
						ui = (uint)idObj;
						num2 = 1;
					}
					else
					{
						num2 = 0;
					}
					if (num2 == 0)
					{
						continue;
					}
					id = ui;
				}
				int nbTaches = reader2.GetInt32(2);
				ContactsRechercheAccueil r = resa.Find((ContactsRechercheAccueil t) => t.CRefContact == id);
				r._AvecTaches = nbTaches > 0;
				if (!reader2.IsDBNull(1) && !(reader2.GetDateTime(1) > DateTime.Now))
				{
					r._NbTaches = nbTaches;
				}
			}
		}
		return resa;
	}
}
