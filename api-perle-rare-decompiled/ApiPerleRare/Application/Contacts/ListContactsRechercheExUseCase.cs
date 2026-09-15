using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Application.Abstractions;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiPerleRare.Application.Contacts;

public sealed class ListContactsRechercheExUseCase : IListContactsRechercheExUseCase
{
	private readonly IApplicationDbContext _context;

	private readonly IUserService _userService;

	static ListContactsRechercheExUseCase()
	{
		ContactsRecherchePhpConfig.EnsureRegistered();
	}

	public ListContactsRechercheExUseCase(IApplicationDbContext context, IUserService userService)
	{
		_context = context;
		_userService = userService;
		ContactsRecherchePhpConfig.EnsureRegistered();
	}

	public Task<List<ContactsRechercheEx>> Execute(string select, string where, string orderby, int skip, int take, bool applyFilter, string userLogin)
	{
		return Execute(select, where, orderby, skip, take, applyFilter, userLogin, leanForAccueil: false);
	}

	public async Task<List<ContactsRechercheEx>> Execute(string select, string where, string orderby, int skip, int take, bool applyFilter, string userLogin, bool leanForAccueil)
	{
		ContactsRecherchePhpConfig.EnsureRegistered();
		QueryPaging.Normalize(ref skip, ref take);
		if (skip > 0)
		{
			orderby = QueryPaging.EnsureOrderBy(orderby, "CRefContact");
		}
		IQueryable<ContactsRecherche> query = _context.ContactsRecherche.AsNoTracking();
		query = ApplyDefaultFilter(query, applyFilter, userLogin);
		query = EFHelper<ContactsRecherche>.Apply(query, where, orderby, take, skip, leanForAccueil ? null : select);
		if (leanForAccueil)
		{
			// Home Accueil only needs identity / roles / flags — skip huge TEXT + PHP blobs on the wire.
			query = query.Select((ContactsRecherche c) => new ContactsRecherche
			{
				CRefContact = c.CRefContact,
				CPrenom = c.CPrenom,
				CNomFamille = c.CNomFamille,
				CDateCreation = c.CDateCreation,
				CDate = c.CDate,
				CDateFin = c.CDateFin,
				CApporteur = c.CApporteur,
				C2emeApporteur = c.C2emeApporteur,
				CNegociateur = c.CNegociateur,
				C2emeNegociateur = c.C2emeNegociateur,
				CNomFamilleConseiller = c.CNomFamilleConseiller,
				C2emeConseiller = c.C2emeConseiller,
				CTypeRecherche = c.CTypeRecherche,
				CStatut = c.CStatut,
				CMttHono = c.CMttHono
			});
		}
		List<ContactsRecherche> res = await query.ToListAsync();
		if (leanForAccueil)
		{
			FixAccueilNameEncoding(res);
		}
		else
		{
			res.FixEncoding();
			EFHelper<ContactsRecherche>.ConvertPhpSerializedToJson(res);
		}
		List<ContactsRechercheEx> res2 = res.Select((ContactsRecherche v) => EFHelper<ContactsRecherche>.Copy<ContactsRechercheEx>(v)).ToList();
		WarmConseillers(res2);
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

	private static void FixAccueilNameEncoding(List<ContactsRecherche> rows)
	{
		foreach (ContactsRecherche r in rows)
		{
			r.CPrenom = EncodingHelper.FixEncoding(r.CPrenom);
			r.CNomFamille = EncodingHelper.FixEncoding(r.CNomFamille);
			r.CTypeRecherche = EncodingHelper.FixEncoding(r.CTypeRecherche);
			r.CApporteur = EncodingHelper.FixEncoding(r.CApporteur);
			r.CNegociateur = EncodingHelper.FixEncoding(r.CNegociateur);
			r.CNomFamilleConseiller = EncodingHelper.FixEncoding(r.CNomFamilleConseiller);
		}
	}

	private void WarmConseillers(List<ContactsRechercheEx> rows)
	{
		HashSet<string> logins = new HashSet<string>(StringComparer.Ordinal);
		foreach (ContactsRechercheEx r in rows)
		{
			AddLogin(logins, r.C2emeApporteur);
			AddLogin(logins, r.C2emeConseiller);
			AddLogin(logins, r.C2emeNegociateur);
			AddLogin(logins, r.CApporteur);
			AddLogin(logins, r.CNomFamilleConseiller);
			AddLogin(logins, r.CNegociateur);
		}
		_userService.WarmConseillers(logins);
	}

	private static void AddLogin(HashSet<string> logins, string login)
	{
		if (!string.IsNullOrEmpty(login))
		{
			logins.Add(login);
		}
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

	internal static IQueryable<ContactsRecherche> ApplyDefaultFilter(IQueryable<ContactsRecherche> query, bool applyFilter, string userLogin)
	{
		if (applyFilter)
		{
			string conseillerId = userLogin;
			query = query.Where((ContactsRecherche c) => c.CApporteur == conseillerId || c.C2emeApporteur == conseillerId || c.CNegociateur == conseillerId || c.C2emeNegociateur == conseillerId || c.CNomFamilleConseiller == conseillerId || c.C2emeConseiller == conseillerId);
		}
		return query;
	}
}
