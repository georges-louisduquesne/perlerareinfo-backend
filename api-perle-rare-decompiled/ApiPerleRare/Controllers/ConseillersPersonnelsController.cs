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
public class ConseillersPersonnelsController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public ConseillersPersonnelsController(ApplicationDbContext context)
	{
		_context = context;
	}

	private static readonly string[] RestrictedFilterTokens = new string[7] { "motdepasse", "autologin", "pieceidentite", "datenaissance", "melperso", "adresse", "commentaire" };

	private bool IsAdmin => User.IsInRole("Admin");

	private static bool UsesRestrictedField(string expression)
	{
		if (string.IsNullOrEmpty(expression))
		{
			return false;
		}
		string folded = expression.ToLowerInvariant().Replace("_", "");
		return RestrictedFilterTokens.Any((string t) => folded.Contains(t));
	}

	// Non-admins (agents, associés) need the team directory for Contact, agenda, tâches and mails,
	// but never credentials nor other people's personal data.
	private static void StripForNonAdmin(ConseillersPersonnels cp, bool isSelf)
	{
		cp.CpMotDePasse = null;
		cp.CpMelMotDePasse = null;
		cp.CpAutoLogin = null;
		if (!isSelf)
		{
			cp.CpPieceIdentite = null;
			cp.CpDateNaissance = null;
			cp.CpMelPerso = null;
			cp.CpAdresse = null;
			cp.CpCp = 0;
			cp.CpVille = null;
			cp.CpCommentaire = null;
			cp.CpCv = null;
		}
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<ConseillersPersonnels>>> GetConseillersPersonnels([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		bool isAdmin = IsAdmin;
		if (!isAdmin && (UsesRestrictedField(where) || UsesRestrictedField(orderby)))
		{
			return Forbid();
		}
		try
		{
			IQueryable<ConseillersPersonnels> query = _context.ConseillersPersonnels.AsNoTracking();
			query = EFHelper<ConseillersPersonnels>.Apply(query, where, orderby, take, skip, select);
			List<ConseillersPersonnels> res = await query.AsNoTracking().ToListAsync();
			int userId = isAdmin ? 0 : this.GetUserId();
			foreach (ConseillersPersonnels cr in res)
			{
				EncodingHelper.FixEncodingInStringProperties(cr);
				if (!isAdmin)
				{
					StripForNonAdmin(cr, cr.CpRefConseiller == userId);
				}
			}
			return res;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[Authorize(Roles = "Admin")]
	[HttpGet]
	[Route("/api/Negociateurs")]
	public async Task<ActionResult<SelectResult<Negociateur>>> GetNegociateurs([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0)
	{
		try
		{
			IQueryable<ConseillersPersonnels> query = _context.ConseillersPersonnels.Where((ConseillersPersonnels cp) => (int?)cp.CpActif == (int?)1 && cp.CpNegociateur && (cp.CpStatut == "Agent commercial" || cp.CpStatut == "Associé")).AsNoTracking();
			SelectResult<ConseillersPersonnels> res = await EFHelper<ConseillersPersonnels>.Select(query, where, orderby, take, skip, select);
			SelectResult<Negociateur> nres = new SelectResult<Negociateur>
			{
				Total = res.Total,
				Items = new Negociateur[res.Items.Length]
			};
			for (int i = 0; i < res.Items.Length; i++)
			{
				Negociateur neg = new Negociateur();
				EFHelper<ConseillersPersonnels>.Copy(res.Items[i], neg);
				nres.Items[i] = neg;
			}
			string ids = string.Join(", ", res.Items.Select((ConseillersPersonnels r) => r.CpRefConseiller));
			if (ids.Length > 0)
			{
				using DbConnection c = _context.Database.GetDbConnection();
				c.Open();
				using DbCommand cmd = c.CreateCommand();
				cmd.CommandText = "\r\nSELECT cp.CP_RefConseiller,\r\n\r\n(SELECT COUNT(*) FROM contacts_recherche cr left JOIN formulaires f  ON cr.C_RefFormulaire=f.F_Ref\r\nWHERE cr.C_Negociateur=cp.CP_Login and ((f.F_RefNegociateur=cp.CP_RefConseiller AND f.F_Statut IN (1,2,3)) OR cr.C_RefFormulaire IS NULL) AND cr.C_Date_Creation>=CURDATE()) NbFormsDuJour,\r\n\r\n(SELECT COUNT(*) FROM contacts_recherche cr left JOIN formulaires f  ON cr.C_RefFormulaire=f.F_Ref\r\nWHERE cr.C_Negociateur=cp.CP_Login and ((f.F_RefNegociateur=cp.CP_RefConseiller AND f.F_Statut IN (1,2,3)) OR cr.C_RefFormulaire IS NULL) AND cr.C_Date_Creation < CURDATE() AND cr.C_Date_Creation>=DATE_ADD(CURDATE(), INTERVAL -30 DAY)) NbFormsEnRetard,\r\n\r\n(SELECT COUNT(*) FROM taches t\r\nINNER JOIN contacts_recherche cr ON cr.C_RefContact=t.T_Ref_Contact\r\nWHERE t.T_Qui=cp.CP_Login AND t.T_Etat='' AND t.T_Date_Realisation=CURDATE() AND cr.C_Statut IN ('PROSPECT ACTIF', 'PROSPECT MORT')) NbTachesDuJour,\r\n\r\n(SELECT COUNT(*) FROM taches t \r\nINNER JOIN contacts_recherche cr ON cr.C_RefContact=t.T_Ref_Contact\r\nWHERE t.T_Qui=cp.CP_Login AND t.T_Etat='' AND t.T_Date_Realisation<CURDATE() AND cr.C_Statut IN ('PROSPECT ACTIF', 'PROSPECT MORT')) NbTachesEnRetard\r\n\r\n FROM conseillers_personnels cp\r\n WHERE cp.CP_RefConseiller IN (" + ids + ")";
				using DbDataReader reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					int refId = reader.GetInt32(0);
					Negociateur neg2 = nres.Items.Single((Negociateur negociateur) => negociateur.CpRefConseiller == refId);
					neg2.NbFormsDuJour = reader.GetInt32(1);
					neg2.NbFormsEnRetard = reader.GetInt32(2);
					neg2.NbTachesDuJour = reader.GetInt32(3);
					neg2.NbTachesEnRetard = reader.GetInt32(4);
				}
			}
			return nres;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<ConseillersPersonnels>> GetConseillersPersonnels(uint id)
	{
		ConseillersPersonnels cp = await _context.ConseillersPersonnels.AsNoTracking().FirstOrDefaultAsync((ConseillersPersonnels e) => e.CpRefConseiller == id);
		if (cp == null)
		{
			return NotFound();
		}
		if (!IsAdmin)
		{
			StripForNonAdmin(cp, cp.CpRefConseiller == this.GetUserId());
		}
		return cp;
	}

	[Authorize(Roles = "Admin")]
	[HttpPut("{id}")]
	public async Task<IActionResult> PutConseillersPersonnels(uint id, ConseillersPersonnels conseillersPersonnels)
	{
		if (id != conseillersPersonnels.CpRefConseiller)
		{
			return BadRequest();
		}
		_context.Entry(conseillersPersonnels).State = EntityState.Modified;
		_context.Entry(conseillersPersonnels).Property(x => x.CpMotDePasse).IsModified = false;
		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException)
		{
			if (!ConseillersPersonnelsExists(id))
			{
				return NotFound();
			}
			throw;
		}
		return NoContent();
	}

	[Authorize(Roles = "Admin")]
	[HttpPost]
	public async Task<ActionResult<ConseillersPersonnels>> PostConseillersPersonnels(ConseillersPersonnels conseillersPersonnels)
	{
		try
		{
			ConseillersPersonnels conseillersPersonnels2 = conseillersPersonnels;
			if (conseillersPersonnels2.CpAdresse == null)
			{
				conseillersPersonnels2.CpAdresse = "";
			}
			conseillersPersonnels2 = conseillersPersonnels;
			if (conseillersPersonnels2.CpMandat == null)
			{
				conseillersPersonnels2.CpMandat = "";
			}
			conseillersPersonnels2 = conseillersPersonnels;
			if (conseillersPersonnels2.CpMelPerso == null)
			{
				conseillersPersonnels2.CpMelPerso = "";
			}
			conseillersPersonnels2 = conseillersPersonnels;
			if (conseillersPersonnels2.CpPhoto == null)
			{
				conseillersPersonnels2.CpPhoto = "";
			}
			conseillersPersonnels2 = conseillersPersonnels;
			if (conseillersPersonnels2.CpPieceIdentite == null)
			{
				conseillersPersonnels2.CpPieceIdentite = "";
			}
			conseillersPersonnels2 = conseillersPersonnels;
			if (conseillersPersonnels2.CpRsac == null)
			{
				conseillersPersonnels2.CpRsac = "";
			}
			conseillersPersonnels2 = conseillersPersonnels;
			if (conseillersPersonnels2.CpVille == null)
			{
				conseillersPersonnels2.CpVille = "";
			}
			_context.ConseillersPersonnels.Add(conseillersPersonnels);
			await _context.SaveChangesAsync();
			return CreatedAtAction("GetConseillersPersonnels", new
			{
				id = conseillersPersonnels.CpRefConseiller
			}, conseillersPersonnels);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	[Authorize(Roles = "Admin")]
	[HttpDelete("{id}")]
	public async Task<ActionResult<ConseillersPersonnels>> DeleteConseillersPersonnels(uint id)
	{
		ConseillersPersonnels conseillersPersonnels = await _context.ConseillersPersonnels.FindAsync(id);
		if (conseillersPersonnels == null)
		{
			return NotFound();
		}
		_context.ConseillersPersonnels.Remove(conseillersPersonnels);
		await _context.SaveChangesAsync();
		return conseillersPersonnels;
	}

	private bool ConseillersPersonnelsExists(uint id)
	{
		return _context.ConseillersPersonnels.Any((ConseillersPersonnels e) => e.CpRefConseiller == id);
	}
}
