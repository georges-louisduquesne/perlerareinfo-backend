using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using ApiPerleRare.Application.Abstractions;

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

	public async Task<List<ContactsRechercheEx>> Execute(string select, string where, string orderby, int skip, int take, bool applyFilter, string userLogin)
	{
		ContactsRecherchePhpConfig.EnsureRegistered();
		IQueryable<ContactsRecherche> query = _context.ContactsRecherche.AsNoTracking();
		query = ApplyDefaultFilter(query, applyFilter, userLogin);
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
