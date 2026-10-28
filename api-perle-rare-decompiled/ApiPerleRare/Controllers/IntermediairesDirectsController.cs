using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Application.Files;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using ApiPerleRare.YanportModels;
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

	private readonly ImportAgencyLogoUseCase _importLogo;

	private readonly AgencyYanportLinkJob _yanportLinks;

	public IntermediairesDirectsController(ApplicationDbContext context, ImportAgencyLogoUseCase importLogo, AgencyYanportLinkJob yanportLinks)
	{
		_context = context;
		_importLogo = importLogo;
		_yanportLinks = yanportLinks;
	}

	[HttpGet]
	public async Task<ActionResult<SelectResult<IntermediairesDirects>>> GetIntermediairesDirects([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		DbSet<IntermediairesDirects> query = _context.IntermediairesDirects;
		return await EFHelper<IntermediairesDirects>.Select(query, where, orderby, take, skip, select);
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<IntermediairesDirects>> GetIntermediairesDirects(uint id, [FromQuery] string Expand = null)
	{
		IQueryable<IntermediairesDirects> query = _context.IntermediairesDirects.AsNoTracking();
		if (!string.IsNullOrWhiteSpace(Expand))
		{
			string[] parts = Expand.Split(',');
			foreach (string part in parts)
			{
				string name = part.Trim();
				if (string.Equals(name, "pProperty", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(name, "PProperty", StringComparison.OrdinalIgnoreCase))
				{
					query = query.Include((IntermediairesDirects row) => row.PProperty);
				}
			}
		}
		IntermediairesDirects intermediairesDirects = await query.FirstOrDefaultAsync((IntermediairesDirects e) => e.IRefIntermediaire == id);
		if (intermediairesDirects == null)
		{
			return NotFound();
		}
		return intermediairesDirects;
	}

	/// <summary>Historique agence (actor_logs + actor_search), dont les changements de réseau.</summary>
	[HttpGet("{id}/ActorHistory")]
	public ActionResult<AgencyActorHistoryItem[]> GetActorHistory(uint id)
	{
		MySqlConnection c = (MySqlConnection)_context.Database.GetDbConnection();
		if (c.State != ConnectionState.Open)
		{
			c.Open();
		}
		var items = new List<AgencyActorHistoryItem>();
		using (MySqlCommand cmd = c.CreateCommand())
		{
			cmd.CommandText = @"SELECT al.AL_Action AS Action, asr.AS_Date AS Date, asr.AS_Actor AS Actor, al.AL_Champs AS Champs
FROM actor_logs al
INNER JOIN actor_search asr ON asr.AS_Id = al.AL_RefAS
WHERE al.AL_RefIntermediaire = @id AND al.AL_RefCtcInter = 0
ORDER BY asr.AS_Date ASC, al.AL_Id ASC";
			cmd.Parameters.AddWithValue("@id", id);
			using MySqlDataReader reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				object champs = reader.IsDBNull(3) ? null : reader.GetValue(3);
				items.Add(new AgencyActorHistoryItem
				{
					Action = reader.IsDBNull(0) ? "" : reader.GetString(0),
					Date = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1),
					Actor = reader.IsDBNull(2) ? "" : reader.GetString(2),
					Champs = champs == null || champs is DBNull ? "" : Convert.ToString(champs)
				});
			}
		}
		return items.ToArray();
	}

	[HttpPost("{id}/logo")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> ImportLogo(uint id, AgencyLogoImportRequest request)
	{
		try
		{
			AgencyLogoImportResult result = await _importLogo.Execute(id, request?.Url, HttpContext.RequestAborted);
			return Ok(result);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { errors = ex.Message });
		}
	}

	[HttpDelete("{id}/logo")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> ClearLogo(uint id)
	{
		try
		{
			await _importLogo.ExecuteClear(id, HttpContext.RequestAborted);
			return NoContent();
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { errors = ex.Message });
		}
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> PutIntermediairesDirects(uint id, IntermediairesDirects intermediairesDirects)
	{
		if (id != intermediairesDirects.IRefIntermediaire)
		{
			return BadRequest();
		}
		var previous = await _context.IntermediairesDirects.AsNoTracking()
			.Where((IntermediairesDirects e) => e.IRefIntermediaire == id)
			.Select((IntermediairesDirects e) => new { e.IIdYanport, e.IRefGroupe, e.IGroupeEventuel })
			.FirstOrDefaultAsync();
		long? previousYanportId = previous == null ? null : previous.IIdYanport;
		_context.Entry(intermediairesDirects).State = EntityState.Modified;
		// Le logo ne passe que par POST/DELETE …/logo. Sinon la fiche ouverte réécrit
		// l’ancien I_Logo et un logo effacé en base ne peut plus être remplacé.
		_context.Entry(intermediairesDirects).Property((IntermediairesDirects row) => row.ILogo).IsModified = false;
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
		if (previousYanportId.HasValue)
		{
			_yanportLinks.Enqueue(id, previousYanportId.Value, intermediairesDirects.IIdYanport);
		}
		if (previous != null && AgencyNetworkHistory.Changed(previous.IRefGroupe, previous.IGroupeEventuel, intermediairesDirects.IRefGroupe, intermediairesDirects.IGroupeEventuel))
		{
			try
			{
				RecordAgencyNetworkHistory(id, previous.IRefGroupe, previous.IGroupeEventuel, intermediairesDirects.IRefGroupe, intermediairesDirects.IGroupeEventuel);
			}
			catch (Exception)
			{
				// Le rattachement est déjà enregistré : un échec d’historique ne doit pas le masquer.
			}
		}
		return NoContent();
	}

	private void RecordAgencyNetworkHistory(uint agencyId, uint? beforeId, string beforeName, uint? afterId, string afterName)
	{
		MySqlConnection c = (MySqlConnection)_context.Database.GetDbConnection();
		if (c.State != ConnectionState.Open)
		{
			c.Open();
		}
		string actor = (this.GetUserLogin() ?? "").Trim();
		if (actor.Length == 0)
		{
			actor = "fiche-agence";
		}
		if (actor.Length > 250)
		{
			actor = actor.Substring(0, 250);
		}
		string champs = AgencyNetworkHistory.ChampsJson(beforeId, beforeName, afterId, afterName);
		long searchId;
		using (MySqlCommand cmd = c.CreateCommand())
		{
			cmd.CommandText = @"INSERT INTO actor_search (AS_Date, AS_Actor, AS_nbAjouts, AS_nbModifs, AS_nbDesactives)
VALUES (NOW(), @actor, 0, 0, 0)";
			cmd.Parameters.AddWithValue("@actor", actor);
			cmd.ExecuteNonQuery();
		}
		using (MySqlCommand cmd = c.CreateCommand())
		{
			cmd.CommandText = "SELECT LAST_INSERT_ID()";
			searchId = Convert.ToInt64(cmd.ExecuteScalar());
		}
		using (MySqlCommand cmd = c.CreateCommand())
		{
			cmd.CommandText = @"INSERT INTO actor_logs (AL_RefAS, AL_Action, AL_RefCtcInter, AL_RefIntermediaire, AL_Nom, AL_Prenom, AL_Champs)
VALUES (@searchId, 'modif', 0, @agencyId, '', '', @champs)";
			cmd.Parameters.AddWithValue("@searchId", searchId);
			cmd.Parameters.AddWithValue("@agencyId", agencyId);
			cmd.Parameters.AddWithValue("@champs", champs);
			cmd.ExecuteNonQuery();
		}
	}

	[HttpPost]
	public async Task<ActionResult<IntermediairesDirects>> PostIntermediairesDirects(IntermediairesDirects intermediairesDirects)
	{
		_context.IntermediairesDirects.Add(intermediairesDirects);
		await _context.SaveChangesAsync();
		_yanportLinks.Enqueue(intermediairesDirects.IRefIntermediaire, 0, intermediairesDirects.IIdYanport);
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
