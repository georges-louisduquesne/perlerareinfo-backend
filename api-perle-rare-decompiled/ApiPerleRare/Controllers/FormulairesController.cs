using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FormulairesController : ControllerBase
{
	private abstract class AbstractCorrespondanceManager
	{
		public abstract FormulairesCorrespondance[] FindCorrespondances(ApplicationDbContext dbContext, string[] noms, string[] tels, string[] emails);

		public static bool IsMatch(FormulairesCorrespondance fc, Formulaires form)
		{
			fc.NomIdentique = Eq(fc.NomFamille, form.FNom);
			fc.EmailIdentique = Eq(fc.Mel1, form.FEmail) || Eq(fc.Mel2, form.FEmail);
			fc.TelIdentique = Eq(fc.Telephone1, form.FTel) || Eq(fc.Telephone2, form.FTel) || Eq(fc.Mobile1, form.FTel) || Eq(fc.Mobile2, form.FTel) || Eq(fc.Pro1, form.FTel) || Eq(fc.Pro2, form.FTel);
			return fc.NomIdentique || fc.EmailIdentique || fc.TelIdentique;
		}

		private static bool Eq(string cval, string fval)
		{
			if (string.IsNullOrWhiteSpace(cval) || string.IsNullOrWhiteSpace(fval))
			{
				return false;
			}
			return string.Compare(cval.Trim(), fval.Trim(), ignoreCase: true) == 0;
		}
	}

	private abstract class AbstractCorrespondanceManager<TItem> : AbstractCorrespondanceManager
	{
		public override FormulairesCorrespondance[] FindCorrespondances(ApplicationDbContext dbContext, string[] noms, string[] tels, string[] emails)
		{
			Expression<Func<TItem, bool>> where = null;
			if (noms.Length != 0)
			{
				where = PredicateHelper.Or(where, GetNomFilter(noms));
			}
			if (tels.Length != 0)
			{
				where = PredicateHelper.Or(where, GetTelFilter(tels));
			}
			if (emails.Length != 0)
			{
				where = PredicateHelper.Or(where, GetEmailFilter(emails));
			}
			return GetCorrespondances(dbContext, where);
		}

		protected abstract Expression<Func<TItem, bool>> GetNomFilter(string[] noms);

		protected abstract Expression<Func<TItem, bool>> GetTelFilter(string[] tels);

		protected abstract Expression<Func<TItem, bool>> GetEmailFilter(string[] emails);

		protected abstract FormulairesCorrespondance[] GetCorrespondances(ApplicationDbContext dbContext, Expression<Func<TItem, bool>> where);
	}

	private class IntermediairesDirectsCorrespondanceManager : AbstractCorrespondanceManager<IntermediairesDirects>
	{
		protected override FormulairesCorrespondance[] GetCorrespondances(ApplicationDbContext dbContext, Expression<Func<IntermediairesDirects, bool>> where)
		{
			return (from id in dbContext.IntermediairesDirects.Where(@where)
				select new FormulairesCorrespondance
				{
					IRefIntermediaire = id.IRefIntermediaire,
					Mel1 = id.IMel,
					NomFamille = id.INomIntermediaire,
					Nom = id.INomIntermediaire,
					Telephone1 = id.ITelephone,
					Telephone2 = id.ITelephone2,
					Type = "Intermédiaire direct",
					Actif = (id.IActif != 0)
				}).ToArray();
		}

		protected override Expression<Func<IntermediairesDirects, bool>> GetEmailFilter(string[] emails)
		{
			return (IntermediairesDirects id) => emails.Contains(id.IMel);
		}

		protected override Expression<Func<IntermediairesDirects, bool>> GetNomFilter(string[] noms)
		{
			return (IntermediairesDirects id) => noms.Contains(id.INomIntermediaire);
		}

		protected override Expression<Func<IntermediairesDirects, bool>> GetTelFilter(string[] tels)
		{
			return (IntermediairesDirects id) => tels.Contains(id.ITelephone) || tels.Contains(id.ITelephone2);
		}
	}

	private class ContactIntermediaireCorrespondanceManager : AbstractCorrespondanceManager<ContactIntermediaire>
	{
		protected override FormulairesCorrespondance[] GetCorrespondances(ApplicationDbContext dbContext, Expression<Func<ContactIntermediaire, bool>> where)
		{
			return (from ci in dbContext.ContactIntermediaire.Where(@where)
				select new FormulairesCorrespondance
				{
					CRefCtcInter = ci.CRefCtcInter,
					Mel1 = ci.CMel,
					NomFamille = ci.CNom,
					Nom = string.Concat(ci.CNom + " ", ci.CPrenom),
					Telephone1 = ci.CTel,
					Type = "Contact intermédiaire",
					Actif = (ci.CStatut == "Actif")
				}).ToArray();
		}

		protected override Expression<Func<ContactIntermediaire, bool>> GetEmailFilter(string[] emails)
		{
			return (ContactIntermediaire ci) => emails.Contains(ci.CMel);
		}

		protected override Expression<Func<ContactIntermediaire, bool>> GetNomFilter(string[] noms)
		{
			return (ContactIntermediaire ci) => noms.Contains(ci.CNom);
		}

		protected override Expression<Func<ContactIntermediaire, bool>> GetTelFilter(string[] tels)
		{
			return (ContactIntermediaire ci) => tels.Contains(ci.CTel);
		}
	}

	private class ContactsRechercheCorrespondanceManager : AbstractCorrespondanceManager<ContactsRecherche>
	{
		protected override FormulairesCorrespondance[] GetCorrespondances(ApplicationDbContext dbContext, Expression<Func<ContactsRecherche, bool>> where)
		{
			return (from cr in dbContext.ContactsRecherche.Where(@where)
				select new FormulairesCorrespondance
				{
					CRefContact = cr.CRefContact,
					Mel1 = cr.CMel1,
					Mel2 = cr.CMel2,
					Telephone1 = cr.CTelPersonnel1,
					Telephone2 = cr.CTelPersonnel2,
					Mobile1 = cr.CTelMobile1,
					Mobile2 = cr.CTelMobile2,
					Pro1 = cr.CTelProfessionnel1,
					Pro2 = cr.CTelProfessionnel2,
					NomFamille = cr.CNomFamille,
					Nom = string.Concat(cr.CNomFamille + " ", cr.CPrenom),
					Type = cr.CStatut,
					Actif = (cr.CStatut == "CLIENT ACTIF" || cr.CStatut == "PROSPECT ACTIF")
				}).ToArray();
		}

		protected override Expression<Func<ContactsRecherche, bool>> GetEmailFilter(string[] emails)
		{
			return (ContactsRecherche cr) => emails.Contains(cr.CMel1) || emails.Contains(cr.CMel2);
		}

		protected override Expression<Func<ContactsRecherche, bool>> GetNomFilter(string[] noms)
		{
			return (ContactsRecherche cr) => noms.Contains(cr.CNomFamille);
		}

		protected override Expression<Func<ContactsRecherche, bool>> GetTelFilter(string[] tels)
		{
			return (ContactsRecherche cr) => tels.Contains(cr.CTelMobile1) || tels.Contains(cr.CTelMobile2) || tels.Contains(cr.CTelPersonnel1) || tels.Contains(cr.CTelPersonnel2) || tels.Contains(cr.CTelProfessionnel1) || tels.Contains(cr.CTelProfessionnel2);
		}
	}

	private readonly ApplicationDbContext _context;

	public FormulairesController(ApplicationDbContext context)
	{
		_context = context;
	}

	[Authorize(Roles = "Admin")]
	[HttpGet]
	public async Task<ActionResult<SelectResult<Formulaires>>> GetFormulaires([FromQuery] string select = null, [FromQuery] string where = null, [FromQuery] string orderby = null, [FromQuery] int skip = 0, [FromQuery] int take = 0, [FromQuery] int correspondances = 0)
	{
		try
		{
			SelectResult<Formulaires> forms = await EFHelper<Formulaires>.Select(_context.Formulaires, where, orderby, take, skip, select);
			if (correspondances != 0)
			{
				AddCorrespondances(forms.Items);
			}
			return forms;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			return BadRequest(ex2.ToString());
		}
	}

	private void AddCorrespondances(Formulaires[] forms)
	{
		string[] tels = (from i in forms
			select i.FTel into t
			where !string.IsNullOrWhiteSpace(t)
			select t).Distinct().ToArray();
		string[] emails = (from i in forms
			select i.FEmail into e
			where !string.IsNullOrWhiteSpace(e)
			select e).Distinct().ToArray();
		string[] noms = (from i in forms
			select i.FNom into n
			where !string.IsNullOrWhiteSpace(n)
			select n).Distinct().ToArray();
		if (tels.Length == 0 && emails.Length == 0 && noms.Length == 0)
		{
			foreach (Formulaires f in forms)
			{
				f.Correspondances = new FormulairesCorrespondance[0];
			}
			return;
		}
		AbstractCorrespondanceManager[] corrManagers = new AbstractCorrespondanceManager[3]
		{
			new IntermediairesDirectsCorrespondanceManager(),
			new ContactIntermediaireCorrespondanceManager(),
			new ContactsRechercheCorrespondanceManager()
		};
		FormulairesCorrespondance[] allCorrespondances = corrManagers.SelectMany((AbstractCorrespondanceManager cm) => cm.FindCorrespondances(_context, noms, tels, emails)).ToArray();
		foreach (Formulaires form in forms)
		{
			form.Correspondances = (from c in allCorrespondances
				where AbstractCorrespondanceManager.IsMatch(c, form)
				select c.Clone() into c
				orderby c.Nom
				select c).ToArray();
		}
		FormulairesCorrespondance[] left = allCorrespondances.Except(forms.SelectMany((Formulaires formulaires) => formulaires.Correspondances)).ToArray();
	}

	[Authorize]
	[HttpGet]
	[Route("ActiveCount")]
	public async Task<ActionResult<int>> GetActiveFormulairesCount()
	{
		return await _context.Formulaires.CountAsync((Formulaires f) => f.FStatut == 1);
	}

	[Authorize(Roles = "Admin")]
	[HttpGet("{id}")]
	public async Task<ActionResult<Formulaires>> GetFormulaires(int id)
	{
		Formulaires formulaires = await _context.Formulaires.FindAsync(id);
		if (formulaires == null)
		{
			return NotFound();
		}
		AddCorrespondances(new Formulaires[1] { formulaires });
		return formulaires;
	}

	[Authorize(Roles = "Admin")]
	[HttpPut("{id}")]
	public async Task<IActionResult> PutFormulaires(int id, Formulaires formulaires)
	{
		if (id != formulaires.FRef)
		{
			return BadRequest();
		}
		_context.Entry(formulaires).State = EntityState.Modified;
		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException)
		{
			if (!FormulairesExists(id))
			{
				return NotFound();
			}
			throw;
		}
		catch (Exception ex2)
		{
			return BadRequest(ex2.ToString());
		}
		return NoContent();
	}

	[Authorize(Roles = "Admin")]
	[HttpPost]
	public async Task<ActionResult<Formulaires>> PostFormulaires(Formulaires formulaires)
	{
		_context.Formulaires.Add(formulaires);
		await _context.SaveChangesAsync();
		return CreatedAtAction("GetFormulaires", new
		{
			id = formulaires.FRef
		}, formulaires);
	}

	[Authorize(Roles = "Admin")]
	[HttpDelete("{id}")]
	public async Task<ActionResult<Formulaires>> DeleteFormulaires(int id)
	{
		Formulaires formulaires = await _context.Formulaires.FindAsync(id);
		if (formulaires == null)
		{
			return NotFound();
		}
		_context.Formulaires.Remove(formulaires);
		await _context.SaveChangesAsync();
		return formulaires;
	}

	private bool FormulairesExists(int id)
	{
		return _context.Formulaires.Any((Formulaires e) => e.FRef == id);
	}
}
