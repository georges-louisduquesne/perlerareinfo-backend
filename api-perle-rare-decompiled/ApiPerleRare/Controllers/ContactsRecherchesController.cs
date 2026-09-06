using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[EnableCors]
[ApiController]
[Authorize]
public class ContactsRecherchesController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	private readonly IUserSessionService _userSessionService;

	private readonly IUserService _userService;

	public ContactsRecherchesController(ApplicationDbContext context, IUserSessionService userSessionService, IUserService userService)
	{
		_context = context;
		_userSessionService = userSessionService;
		_userService = userService;
	}

	static ContactsRecherchesController()
	{
		EFHelper<ContactsRecherche>.SetPhpSerialized((ContactsRecherche c) => c.CAnciennete, (ContactsRecherche c) => c.CBudget, (ContactsRecherche c) => c.CBudgetC, (ContactsRecherche c) => c.CSurface, (ContactsRecherche c) => c.CTags, (ContactsRecherche c) => c.CLocalisation);
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<ContactsRechercheEx>>> GetContactsRecherche([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		try
		{
			return await InternalGetContactsRechercheEx(select, where, orderby, skip, take);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	private async Task<ActionResult<IEnumerable<ContactsRechercheEx>>> InternalGetContactsRechercheEx(string select, string where, string orderby, int skip, int take)
	{
		IQueryable<ContactsRecherche> query = _context.ContactsRecherche.AsNoTracking();
		query = ApplyDefaultFilter(query);
		query = EFHelper<ContactsRecherche>.Apply(query, where, orderby, take, skip, select);
		List<ContactsRecherche> res = await query.AsNoTracking().ToListAsync();
		res.FixEncoding();
		EFHelper<ContactsRecherche>.ConvertPhpSerializedToJson(res);
		List<ContactsRechercheEx> res2 = res.Select((ContactsRecherche v) => EFHelper<ContactsRecherche>.Copy<ContactsRechercheEx>(v)).ToList();
		foreach (ContactsRechercheEx r in res2)
		{
			Complete(r.C2emeApporteur, delegate(string p)
			{
				r.C2emeApporteur_PS = p;
			}, delegate(string i)
			{
				r.C2emeApporteur_I = i;
			});
			Complete(r.C2emeConseiller, delegate(string p)
			{
				r.C2emeConseiller_PS = p;
			}, delegate(string i)
			{
				r.C2emeConseiller_I = i;
			});
			Complete(r.C2emeNegociateur, delegate(string p)
			{
				r.C2emeNegociateur_PS = p;
			}, delegate(string i)
			{
				r.C2emeNegociateur_I = i;
			});
			Complete(r.CApporteur, delegate(string p)
			{
				r.CApporteur_PS = p;
			}, delegate(string i)
			{
				r.CApporteur_I = i;
			});
			Complete(r.CNomFamilleConseiller, delegate(string p)
			{
				r.CNomFamilleConseiller_PS = p;
			}, delegate(string i)
			{
				r.CNomFamilleConseiller_I = i;
			});
			Complete(r.CNegociateur, delegate(string p)
			{
				r.CNegociateur_PS = p;
			}, delegate(string i)
			{
				r.CNegociateur_I = i;
			});
		}
		return res2;
	}

	private void Complete(string login, Action<string> photoSetter, Action<string> initialesSetter)
	{
		if (!string.IsNullOrEmpty(login))
		{
			ConseillersPersonnels conseiller = _userService.GetConseiller(login);
			if (conseiller != null)
			{
				photoSetter(conseiller.CpPhotoSignature);
				initialesSetter(conseiller.CpInitiales);
			}
		}
	}

	[HttpGet]
	[Route("Accueil")]
	public async Task<ActionResult<IEnumerable<ContactsRechercheAccueil>>> GetContactsRechercheAccueil([FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0, [FromQuery] int withAnnonces = 0, [FromQuery] int withTaches = 0)
	{
		try
		{
			ContactsRechercheEx[] res = InternalGetContactsRechercheEx(null, where, orderby, skip, take).Result.Value.ToArray();
			List<ContactsRechercheAccueil> resa = new List<ContactsRechercheAccueil>();
			using (DbConnection c = _context.Database.GetDbConnection())
			{
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
					string filter = (_userSessionService.Filter ? (" AND T_Qui='" + _userSessionService.Login + "'") : "");
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
			}
			return resa;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	private IQueryable<ContactsRecherche> ApplyDefaultFilter(IQueryable<ContactsRecherche> query)
	{
		if (_userSessionService.Filter)
		{
			string conseillerId = this.GetUserLogin();
			query = query.Where((ContactsRecherche c) => c.CApporteur == conseillerId || c.C2emeApporteur == conseillerId || c.CNegociateur == conseillerId || c.C2emeNegociateur == conseillerId || c.CNomFamilleConseiller == conseillerId || c.C2emeConseiller == conseillerId);
		}
		return query;
	}

	[HttpGet]
	[Route("/api/ContactsRecherchesCount")]
	[Route("Count")]
	public async Task<ActionResult<int>> GetContactsRechercheCount([FromQuery] string where = null)
	{
		try
		{
			IQueryable<ContactsRecherche> query = _context.ContactsRecherche;
			query = ApplyDefaultFilter(query);
			query = EFHelper<ContactsRecherche>.Apply(query, where);
			return await query.CountAsync();
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<ContactsRecherche>> GetContactsRecherche(uint id)
	{
		ContactsRecherche contactsRecherche = await _context.ContactsRecherche.FindAsync(id);
		if (contactsRecherche == null)
		{
			return NotFound();
		}
		EncodingHelper.FixEncodingInStringProperties(contactsRecherche);
		EFHelper<ContactsRecherche>.ConvertPhpSerializedToJson(contactsRecherche);
		return contactsRecherche;
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> PutContactsRecherche(uint id, ContactsRecherche contactsRecherche)
	{
		if (id != contactsRecherche.CRefContact)
		{
			return BadRequest();
		}
		EFHelper<ContactsRecherche>.ConvertPhpSerializedToPhp(contactsRecherche);
		_context.Entry(contactsRecherche).State = EntityState.Modified;
		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException)
		{
			if (!ContactsRechercheExists(id))
			{
				return NotFound();
			}
			throw;
		}
		return NoContent();
	}

	[HttpPost]
	public async Task<ActionResult<ContactsRecherche>> PostContactsRecherche(ContactsRecherche ncr)
	{
		if (await _context.ContactsRecherche.FirstOrDefaultAsync((ContactsRecherche cr) => cr.CNomFamille == ncr.CNomFamille && cr.CTelPersonnel1 == ncr.CTelPersonnel1 && cr.CMel1 == ncr.CMel1) != null)
		{
			return BadRequest("Doublon");
		}
		ContactsRecherche contactsRecherche = ncr;
		if (contactsRecherche.CKeyWord1 == null)
		{
			contactsRecherche.CKeyWord1 = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.CKeyWord2 == null)
		{
			contactsRecherche.CKeyWord2 = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.CKeyWord3 == null)
		{
			contactsRecherche.CKeyWord3 = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.CMandat == null)
		{
			contactsRecherche.CMandat = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.C2emeApporteur == null)
		{
			contactsRecherche.C2emeApporteur = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.C2emeNegociateur == null)
		{
			contactsRecherche.C2emeNegociateur = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.C2emeConseiller == null)
		{
			contactsRecherche.C2emeConseiller = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.CQrecherche == null)
		{
			contactsRecherche.CQrecherche = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.COrigine == null)
		{
			contactsRecherche.COrigine = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.CConditionCom == null)
		{
			contactsRecherche.CConditionCom = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.CRechercheCom == null)
		{
			contactsRecherche.CRechercheCom = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.CLocalisation == null)
		{
			contactsRecherche.CLocalisation = "";
		}
		contactsRecherche = ncr;
		if (contactsRecherche.CTags == null)
		{
			contactsRecherche.CTags = "";
		}
		EFHelper<ContactsRecherche>.ConvertPhpSerializedToPhp(ncr);
		_context.ContactsRecherche.Add(ncr);
		await _context.SaveChangesAsync();
		return CreatedAtAction("GetContactsRecherche", new
		{
			id = ncr.CRefContact
		}, ncr);
	}

	[HttpDelete("{id}")]
	public async Task<ActionResult<ContactsRecherche>> DeleteContactsRecherche(uint id)
	{
		ContactsRecherche contactsRecherche = await _context.ContactsRecherche.FindAsync(id);
		if (contactsRecherche == null)
		{
			return NotFound();
		}
		_context.ContactsRecherche.Remove(contactsRecherche);
		await _context.SaveChangesAsync();
		return contactsRecherche;
	}

	private bool ContactsRechercheExists(uint id)
	{
		return _context.ContactsRecherche.Any((ContactsRecherche e) => e.CRefContact == id);
	}
}
