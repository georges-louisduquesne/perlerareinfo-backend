using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using ApiPerleRare.ScheduledTasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class FormulairesCandidatsController : ControllerBase
{
	public class AttribuerBody
	{
		public uint RefAdmin { get; set; }
	}

	public class RejeterBody
	{
		public string Cause { get; set; }
	}

	public class RecruteurRow
	{
		public uint CpRefConseiller { get; set; }

		public string CpPrenom { get; set; }

		public string CpNomFamille { get; set; }

		public string CpLogin { get; set; }

		public string CpPhoto { get; set; }
	}

	private readonly ApplicationDbContext _context;

	public FormulairesCandidatsController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<object>> GetFormulairesCandidats()
	{
		List<FormulairesCandidats> items = await _context.FormulairesCandidats.AsNoTracking()
			.Where((FormulairesCandidats row) => row.FcStatut == FormulaireCandidatMail.StatutAAttribuer)
			.OrderByDescending((FormulairesCandidats row) => row.FcDate)
			.ToListAsync();
		await AddCorrespondances(items);
		return new
		{
			items,
			total = items.Count
		};
	}

	[HttpGet("ActiveCount")]
	public async Task<ActionResult<int>> GetActiveCount()
	{
		return await _context.FormulairesCandidats.CountAsync((FormulairesCandidats row) => row.FcStatut == FormulaireCandidatMail.StatutAAttribuer);
	}

	[HttpGet("Recruteurs")]
	public async Task<ActionResult<RecruteurRow[]>> GetRecruteurs()
	{
		List<ConseillersPersonnels> rows = await _context.ConseillersPersonnels.AsNoTracking()
			.Where((ConseillersPersonnels cp) => (int?)cp.CpActif == (int?)1 && cp.CpAdmin)
			.ToListAsync();
		RecruteurRow[] recruteurs = rows
			.Where((ConseillersPersonnels cp) => FormulaireCandidatAttribution.IsRecruteur(cp.CpActif, cp.CpAdmin, cp.CpStatut, cp.CpPrenom, cp.CpNomFamille, cp.CpLogin))
			.OrderBy((ConseillersPersonnels cp) => cp.CpNomFamille)
			.ThenBy((ConseillersPersonnels cp) => cp.CpPrenom)
			.Select((ConseillersPersonnels cp) => new RecruteurRow
			{
				CpRefConseiller = cp.CpRefConseiller,
				CpPrenom = cp.CpPrenom,
				CpNomFamille = cp.CpNomFamille,
				CpLogin = cp.CpLogin,
				CpPhoto = cp.CpPhoto
			})
			.ToArray();
		return recruteurs;
	}

	[HttpPost("{id}/attribuer")]
	public async Task<ActionResult<object>> Attribuer(int id, [FromBody] AttribuerBody body)
	{
		if (body == null)
		{
			return BadRequest("Admin manquant");
		}
		FormulairesCandidats form = await _context.FormulairesCandidats.FirstOrDefaultAsync((FormulairesCandidats row) => row.FcRef == id);
		if (form == null)
		{
			return NotFound();
		}
		if (form.FcStatut != FormulaireCandidatMail.StatutAAttribuer)
		{
			return Conflict("Ce formulaire n’est plus à attribuer");
		}
		ConseillersPersonnels admin = await _context.ConseillersPersonnels.FirstOrDefaultAsync((ConseillersPersonnels cp) => cp.CpRefConseiller == body.RefAdmin);
		if (admin == null || !FormulaireCandidatAttribution.IsRecruteur(admin.CpActif, admin.CpAdmin, admin.CpStatut, admin.CpPrenom, admin.CpNomFamille, admin.CpLogin))
		{
			return BadRequest("Admin inconnu");
		}
		DateTime now = DateTime.Now;
		ConseillersPersonnels created = FormulaireCandidatAttribution.CreateConseiller(form, DateOnly.FromDateTime(now));
		await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx = await _context.Database.BeginTransactionAsync();
		_context.ConseillersPersonnels.Add(created);
		await _context.SaveChangesAsync();
		await _context.Database.ExecuteSqlRawAsync(FormulaireCandidatAttribution.PersistOffFlagsSql, created.CpRefConseiller);
		_context.ConseillersPersonnelsEvenements.Add(FormulaireCandidatAttribution.CreateEvent(form, created.CpRefConseiller));
		_context.ConseillersPersonnelsTaches.Add(FormulaireCandidatAttribution.CreateTask(created.CpRefConseiller, admin.CpLogin, now));
		form.FcStatut = FormulaireCandidatMail.StatutAttribue;
		form.FcRefAdmin = admin.CpRefConseiller;
		form.FcRefConseiller = created.CpRefConseiller;
		form.FcCauseRejet = null;
		form.FcDateTraitement = now;
		await _context.SaveChangesAsync();
		await tx.CommitAsync();
		return new
		{
			conseillerRef = created.CpRefConseiller
		};
	}

	[HttpPost("{id}/rejeter")]
	public async Task<IActionResult> Rejeter(int id, [FromBody] RejeterBody body)
	{
		string cause = FormulaireCandidatMail.CanonicalCause(body?.Cause);
		if (cause == null)
		{
			return BadRequest("Cause inconnue");
		}
		FormulairesCandidats form = await _context.FormulairesCandidats.FirstOrDefaultAsync((FormulairesCandidats row) => row.FcRef == id);
		if (form == null)
		{
			return NotFound();
		}
		if (form.FcStatut != FormulaireCandidatMail.StatutAAttribuer)
		{
			return Conflict("Ce formulaire n’est plus à attribuer");
		}
		form.FcStatut = FormulaireCandidatMail.StatutRejete;
		form.FcCauseRejet = cause;
		form.FcDateTraitement = DateTime.Now;
		await _context.SaveChangesAsync();
		return NoContent();
	}

	private async Task AddCorrespondances(List<FormulairesCandidats> forms)
	{
		if (forms.Count == 0)
		{
			return;
		}
		string[] noms = forms.Select((FormulairesCandidats form) => form.FcNom).Where((string value) => !string.IsNullOrWhiteSpace(value)).Distinct().ToArray();
		string[] emails = forms.Select((FormulairesCandidats form) => form.FcEmail).Where((string value) => !string.IsNullOrWhiteSpace(value)).Distinct().ToArray();
		string[] tels = forms.Select((FormulairesCandidats form) => TypeformPartialLeadMapper.NormalizePhone(form.FcTel)).Where((string value) => !string.IsNullOrWhiteSpace(value)).Distinct().ToArray();
		List<PersonSnapshot> people = new List<PersonSnapshot>();
		if (noms.Length != 0 || emails.Length != 0 || tels.Length != 0)
		{
			List<ConseillersPersonnels> conseillers = new List<ConseillersPersonnels>();
			if (noms.Length != 0)
			{
				conseillers.AddRange(await _context.ConseillersPersonnels.AsNoTracking().Where((ConseillersPersonnels cp) => noms.Contains(cp.CpNomFamille)).ToListAsync());
			}
			if (emails.Length != 0)
			{
				conseillers.AddRange(await _context.ConseillersPersonnels.AsNoTracking().Where((ConseillersPersonnels cp) => emails.Contains(cp.CpMelPerso)).ToListAsync());
			}
			if (tels.Length != 0)
			{
				conseillers.AddRange(await _context.ConseillersPersonnels.AsNoTracking().Where((ConseillersPersonnels cp) => tels.Contains(cp.CpTelPersonnel)).ToListAsync());
			}
			conseillers = conseillers.GroupBy((ConseillersPersonnels cp) => cp.CpRefConseiller).Select((IGrouping<uint, ConseillersPersonnels> group) => group.First()).ToList();
			people.AddRange(conseillers.Select((ConseillersPersonnels cp) => new PersonSnapshot
			{
				Ref = cp.CpRefConseiller,
				Kind = "conseiller",
				Prenom = cp.CpPrenom,
				Nom = cp.CpNomFamille,
				Statut = string.IsNullOrWhiteSpace(cp.CpStatut) ? "Candidat" : cp.CpStatut,
				Created = cp.CpDateCreation.HasValue ? cp.CpDateCreation.Value.ToDateTime(TimeOnly.MinValue) : null,
				Emails = new string[2] { cp.CpMelPerso, cp.CpMel },
				Tels = new string[1] { cp.CpTelPersonnel }
			}));
			List<ContactsRecherche> contacts = new List<ContactsRecherche>();
			if (noms.Length != 0)
			{
				contacts.AddRange(await _context.ContactsRecherche.AsNoTracking().Where((ContactsRecherche cr) => noms.Contains(cr.CNomFamille)).ToListAsync());
			}
			if (emails.Length != 0)
			{
				contacts.AddRange(await _context.ContactsRecherche.AsNoTracking().Where((ContactsRecherche cr) => emails.Contains(cr.CMel1) || emails.Contains(cr.CMel2)).ToListAsync());
			}
			if (tels.Length != 0)
			{
				contacts.AddRange(await _context.ContactsRecherche.AsNoTracking().Where((ContactsRecherche cr) => tels.Contains(cr.CTelPersonnel1) || tels.Contains(cr.CTelMobile1)).ToListAsync());
			}
			contacts = contacts.GroupBy((ContactsRecherche cr) => cr.CRefContact).Select((IGrouping<uint, ContactsRecherche> group) => group.First()).ToList();
			people.AddRange(contacts.Select((ContactsRecherche cr) => new PersonSnapshot
			{
				Ref = cr.CRefContact,
				Kind = "contact",
				Prenom = cr.CPrenom,
				Nom = cr.CNomFamille,
				Statut = cr.CStatut,
				Created = cr.CDateCreation.ToDateTime(TimeOnly.MinValue),
				Emails = new string[2] { cr.CMel1, cr.CMel2 },
				Tels = new string[4] { cr.CTelPersonnel1, cr.CTelPersonnel2, cr.CTelMobile1, cr.CTelMobile2 }
			}));
		}
		foreach (FormulairesCandidats form in forms)
		{
			form.Correspondances = FormulaireCandidatMatches.Match(form.FcNom, form.FcEmail, form.FcTel, people);
		}
	}
}
