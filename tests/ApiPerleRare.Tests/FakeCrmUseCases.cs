using System.Collections.Generic;
using System.Threading.Tasks;
using ApiPerleRare.Application.Catalog;
using ApiPerleRare.Application.Contacts;
using ApiPerleRare.Application.Tasks;
using ApiPerleRare.Controllers;
using ApiPerleRare.Helpers;
using ApiPerleRare.Models;

namespace ApiPerleRare.Tests;

internal sealed class FakeQueryEntitiesUseCase<TEntity> : IQueryEntitiesUseCase<TEntity> where TEntity : class, new()
{
	public Task<SelectResult<TEntity>> Execute(EntityQuery query)
	{
		TEntity item = new TEntity();
		if (item is TypesTaches tache)
		{
			tache.TtType = "Appel";
			tache.TtContactStatut = "PROSPECT ACTIF";
			tache.TtTypeTransaction = "A";
		}
		if (item is Property property)
		{
			property.PPropertyId = "prop-1";
			property.PCp = "94120";
			property.PTypeTransaction = "A";
			property.PState = 1;
		}
		return Task.FromResult(new SelectResult<TEntity>
		{
			Total = 1,
			Items = new[] { item }
		});
	}
}

internal sealed class FakeListContactsRechercheExUseCase : IListContactsRechercheExUseCase
{
	public Task<List<ContactsRechercheEx>> Execute(string select, string where, string orderby, int skip, int take, bool applyFilter, string userLogin)
	{
		return Execute(select, where, orderby, skip, take, applyFilter, userLogin, leanForAccueil: false);
	}

	public Task<List<ContactsRechercheEx>> Execute(string select, string where, string orderby, int skip, int take, bool applyFilter, string userLogin, bool leanForAccueil)
	{
		return Task.FromResult(new List<ContactsRechercheEx> { SeedContact() });
	}

	internal static ContactsRechercheEx SeedContact() => new()
	{
		CRefContact = 7,
		CPrenom = "Léa",
		CNomFamille = "Martin",
		CStatut = "PROSPECT ACTIF",
		CNomFamilleConseiller = "demo"
	};
}

internal sealed class FakeListContactsRechercheAccueilUseCase : IListContactsRechercheAccueilUseCase
{
	public Task<List<ContactsRechercheAccueil>> Execute(string where, string orderby, int skip, int take, int withAnnonces, int withTaches, bool applyFilter, string userLogin)
	{
		return Task.FromResult(new List<ContactsRechercheAccueil>
		{
			new()
			{
				CRefContact = 7,
				CPrenom = "Léa",
				CNomFamille = "Martin",
				CStatut = "PROSPECT ACTIF"
			}
		});
	}
}

internal sealed class FakeCountContactsRechercheUseCase : ICountContactsRechercheUseCase
{
	public Task<int> Execute(string where, bool applyFilter, string userLogin) => Task.FromResult(1);
}

internal sealed class FakeListTachesExUseCase : IListTachesExUseCase
{
	public Task<List<TachesEx>> Execute(EntityQuery query, string option, string userLogin, bool applyQuiFilter)
	{
		return Task.FromResult(new List<TachesEx>
		{
			new()
			{
				TRef = 3,
				TRefContact = 7,
				TType = "Appel",
				TQui = "demo",
				TEtat = "A faire",
				TCom = "Relance"
			}
		});
	}
}
