using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class IntermediairesDirectsController : ControllerBase
{
	public class IntermediairesDirectsWithNb
	{
		public uint IRefIntermediaire { get; set; }

		public string INomIntermediaire { get; set; }

		public string IStanding { get; set; }

		public string IDiffusionSitePropre { get; set; }

		public string IIntercabinet { get; set; }

		public string IGroupeEventuel { get; set; }

		public string IQualiteRelation { get; set; }

		public string IAdhIntercabinet { get; set; }

		public string IAdresse1 { get; set; }

		public int ICodePostal { get; set; }

		public string ITelephone { get; set; }

		public string ITelephone2 { get; set; }

		public int Nb { get; set; }

		public IntermediairesDirectsContactIntermediaire[] Contacts { get; set; }

		public string I2UrlSite { get; set; }
	}

	public class IntermediairesDirectsContactIntermediaire
	{
		public string CNom { get; set; }

		public string CPrenom { get; set; }

		public string CTel { get; set; }

		public string CMel { get; set; }

		public string CQualiteRelation { get; set; }

		public string CStatut { get; set; }

		public uint CRefIntermediaire { get; set; }
	}

	private readonly ApplicationDbContext _context;

	public IntermediairesDirectsController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<SelectResult<IntermediairesDirects>>> GetIntermediairesDirects([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		DbSet<IntermediairesDirects> query = _context.IntermediairesDirects;
		return await EFHelper<IntermediairesDirects>.Select(query, where, orderby, take, skip, select);
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<IntermediairesDirects>> GetIntermediairesDirects(uint id)
	{
		IntermediairesDirects intermediairesDirects = await _context.IntermediairesDirects.FindAsync(id);
		if (intermediairesDirects == null)
		{
			return NotFound();
		}
		return intermediairesDirects;
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> PutIntermediairesDirects(uint id, IntermediairesDirects intermediairesDirects)
	{
		if (id != intermediairesDirects.IRefIntermediaire)
		{
			return BadRequest();
		}
		_context.Entry(intermediairesDirects).State = EntityState.Modified;
		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException)
		{
			if (!IntermediairesDirectsExists(id))
			{
				return NotFound();
			}
			throw;
		}
		return NoContent();
	}

	[HttpPost]
	public async Task<ActionResult<IntermediairesDirects>> PostIntermediairesDirects(IntermediairesDirects intermediairesDirects)
	{
		_context.IntermediairesDirects.Add(intermediairesDirects);
		await _context.SaveChangesAsync();
		return CreatedAtAction("GetIntermediairesDirects", new
		{
			id = intermediairesDirects.IRefIntermediaire
		}, intermediairesDirects);
	}

	[HttpDelete("{id}")]
	public async Task<ActionResult<IntermediairesDirects>> DeleteIntermediairesDirects(uint id)
	{
		IntermediairesDirects intermediairesDirects = await _context.IntermediairesDirects.FindAsync(id);
		if (intermediairesDirects == null)
		{
			return NotFound();
		}
		_context.IntermediairesDirects.Remove(intermediairesDirects);
		await _context.SaveChangesAsync();
		return intermediairesDirects;
	}

	private bool IntermediairesDirectsExists(uint id)
	{
		return _context.IntermediairesDirects.Any((IntermediairesDirects e) => e.IRefIntermediaire == id);
	}

	[HttpGet]
	[Route("Top30Agences/{contactId}")]
	public ActionResult<IntermediairesDirectsWithNb[]> GetTop30Agences(uint contactId)
	{
		MySqlConnection c = (MySqlConnection)_context.Database.GetDbConnection();
		if (c.State != ConnectionState.Open)
		{
			c.Open();
		}
		if (!c.DoesTableExist($"annonces_refcontact_{contactId}"))
		{
			return new IntermediairesDirectsWithNb[0];
		}
		string select = $"select I_RefIntermediaire, I_NomIntermediaire, I_Standing, I_DiffusionSitePropre, I_Intercabinet, I_GroupeEventuel, I_QualiteRelation, I_AdhIntercabinet, I_Adresse1, I_CodePostal, I_Telephone, I_Telephone2, count(*) as Nb from intermediaires_directs id\r\n\tinner join annonces_refcontact_{contactId} ar\r\n\tinner join annonces_globales ag on ar.A_RefAnnGlob=ag.AG_Ref\r\n\twhere id.I_Actif=1 and ag.AG_TelAnnonceur is not null and ag.AG_TelAnnonceur <> '' and\r\n\t(id.I_Telephone = ag.AG_TelAnnonceur or id.I_Telephone2= ag.AG_TelAnnonceur or id.I_RefIntermediaire in (select ci.C_RefIntermediaire from contact_intermediaire ci where ci.C_Tel=ag.AG_TelAnnonceur) )\r\n\tand (ag.AG_DateFin is null or ag.AG_DateFin>date_add(now(),interval -2 year))\r\n\tgroup by I_RefIntermediaire, I_NomIntermediaire, I_Standing, I_DiffusionSitePropre, I_Intercabinet, I_GroupeEventuel, I_QualiteRelation, I_AdhIntercabinet, I_Adresse1, I_CodePostal, I_Telephone, I_Telephone2\r\n\torder by nb desc\r\n\tlimit 0,30";
		IntermediairesDirectsWithNb[] intermediaires;
		using (MySqlCommand cmd = c.CreateCommand())
		{
			cmd.CommandText = select;
			using MySqlDataReader reader = cmd.ExecuteReader();
			intermediaires = reader.ToModels<IntermediairesDirectsWithNb>();
		}
		if (intermediaires.Length != 0)
		{
			IntermediairesDirectsContactIntermediaire[] contacts;
			using (MySqlCommand cmd2 = c.CreateCommand())
			{
				cmd2.CommandText = "SELECT C_Nom,C_Prenom,C_Tel,C_Mel,C_QualiteRelation, C_Statut, C_RefIntermediaire FROM contact_intermediaire where C_RefIntermediaire in (" + string.Join(", ", intermediaires.Select((IntermediairesDirectsWithNb intermediairesDirectsWithNb) => intermediairesDirectsWithNb.IRefIntermediaire)) + ")";
				using MySqlDataReader reader2 = cmd2.ExecuteReader();
				contacts = reader2.ToModels<IntermediairesDirectsContactIntermediaire>();
			}
			IntermediairesDirectsWithNb[] array = intermediaires;
			foreach (IntermediairesDirectsWithNb i in array)
			{
				i.Contacts = (from intermediairesDirectsContactIntermediaire in contacts
					where intermediairesDirectsContactIntermediaire.CRefIntermediaire == i.IRefIntermediaire
					orderby intermediairesDirectsContactIntermediaire.CNom, intermediairesDirectsContactIntermediaire.CPrenom
					select intermediairesDirectsContactIntermediaire).ToArray();
			}
		}
		string[] groups = (from intermediairesDirectsWithNb in intermediaires
			select intermediairesDirectsWithNb.IGroupeEventuel into g
			where !string.IsNullOrWhiteSpace(g)
			select g).Distinct().ToArray();
		if (groups.Length != 0)
		{
			var data = (from intermediairesIndirects in _context.IntermediairesIndirects
				where groups.Contains(intermediairesIndirects.I2NomIntermIndirect)
				select new { intermediairesIndirects.I2NomIntermIndirect, intermediairesIndirects.I2UrlSite }).ToArray();
			IntermediairesDirectsWithNb[] array2 = intermediaires;
			foreach (IntermediairesDirectsWithNb i2 in array2)
			{
				var d = data.FirstOrDefault(t => string.Compare(t.I2NomIntermIndirect, i2.IGroupeEventuel, ignoreCase: true) == 0);
				if (d != null)
				{
					i2.I2UrlSite = d.I2UrlSite;
				}
			}
		}
		return intermediaires;
	}
}
