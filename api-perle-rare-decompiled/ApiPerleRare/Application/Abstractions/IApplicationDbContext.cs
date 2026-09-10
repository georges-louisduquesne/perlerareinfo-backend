using System.Threading;
using System.Threading.Tasks;
using ApiPerleRare.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ApiPerleRare.Application.Abstractions;

/// <summary>Port over EF Core for Application use cases (DIP).</summary>
public interface IApplicationDbContext
{
	DbSet<AdminVersions> AdminVersions { get; }
	DbSet<AgencesYanport> AgencesYanport { get; }
	DbSet<AnnoncesGlobales> AnnoncesGlobales { get; }
	DbSet<AnnoncesGlobalesVerifs> AnnoncesGlobalesVerifs { get; }
	DbSet<Audit> Audit { get; }
	DbSet<Biens> Biens { get; }
	DbSet<Calendar> Calendar { get; }
	DbSet<CategoriesIntermediaires> CategoriesIntermediaires { get; }
	DbSet<CodesPostaux> CodesPostaux { get; }
	DbSet<ConseillersPersonnels> ConseillersPersonnels { get; }
	DbSet<ConseillersPersonnelsEvenements> ConseillersPersonnelsEvenements { get; }
	DbSet<ConseillersPersonnelsTaches> ConseillersPersonnelsTaches { get; }
	DbSet<ConseillersPersonnelsTypesEvenements> ConseillersPersonnelsTypesEvenements { get; }
	DbSet<ContactAgence> ContactAgence { get; }
	DbSet<ContactAgencePublication> ContactAgencePublication { get; }
	DbSet<ContactEvenements> ContactEvenements { get; }
	DbSet<ContactIntermediaire> ContactIntermediaire { get; }
	DbSet<ContactsRecherche> ContactsRecherche { get; }
	DbSet<Dictionnaire> Dictionnaire { get; }
	DbSet<Erreur> Erreur { get; }
	DbSet<Evenements> Evenements { get; }
	DbSet<Formulaires> Formulaires { get; }
	DbSet<HistoriqueAnnonces> HistoriqueAnnonces { get; }
	DbSet<HistoriqueAspiration> HistoriqueAspiration { get; }
	DbSet<HistoriqueInterDirect> HistoriqueInterDirect { get; }
	DbSet<IntermediairesDirects> IntermediairesDirects { get; }
	DbSet<IntermediairesDirectsYanport> IntermediairesDirectsYanport { get; }
	DbSet<IntermediairesIndirects> IntermediairesIndirects { get; }
	DbSet<Ips> Ips { get; }
	DbSet<IpsHost> IpsHost { get; }
	DbSet<Mails> Mails { get; }
	DbSet<MetaMoteurs> MetaMoteurs { get; }
	DbSet<ModelesCommerciaux> ModelesCommerciaux { get; }
	DbSet<MotsCles> MotsCles { get; }
	DbSet<NiveauxDiffusion> NiveauxDiffusion { get; }
	DbSet<NiveauxHabilitation> NiveauxHabilitation { get; }
	DbSet<Notifications> Notifications { get; }
	DbSet<Organisation> Organisation { get; }
	DbSet<Origines> Origines { get; }
	DbSet<Pap> Pap { get; }
	DbSet<PhotosAnnonces> PhotosAnnonces { get; }
	DbSet<Property> Property { get; }
	DbSet<PropertyContact> PropertyContact { get; }
	DbSet<Proxy> Proxy { get; }
	DbSet<ProxyAnonyme> ProxyAnonyme { get; }
	DbSet<QualiteRelationIntermediaire> QualiteRelationIntermediaire { get; }
	DbSet<Quartiers> Quartiers { get; }
	DbSet<RechAutomatique> RechAutomatique { get; }
	DbSet<RechercheGlobalConfig> RechercheGlobalConfig { get; }
	DbSet<ScheduledTaskLogs> ScheduledTaskLogs { get; }
	DbSet<SqlLogs> SqlLogs { get; }
	DbSet<StandingBien> StandingBien { get; }
	DbSet<StatistiquesGenerales> StatistiquesGenerales { get; }
	DbSet<Stats> Stats { get; }
	DbSet<StatutsContacts> StatutsContacts { get; }
	DbSet<StratalisExpired> StratalisExpired { get; }
	DbSet<Taches> Taches { get; }
	DbSet<TagsAnnonce> TagsAnnonce { get; }
	DbSet<Templates> Templates { get; }
	DbSet<TypesBiens> TypesBiens { get; }
	DbSet<TypesCatInterIndirect> TypesCatInterIndirect { get; }
	DbSet<TypesEvenements> TypesEvenements { get; }
	DbSet<TypesMails> TypesMails { get; }
	DbSet<TypesMissions> TypesMissions { get; }
	DbSet<TypesModeles> TypesModeles { get; }
	DbSet<TypesSupprform> TypesSupprform { get; }
	DbSet<TypesTaches> TypesTaches { get; }
	DbSet<UrlLogs> UrlLogs { get; }
	DbSet<UrlSearch> UrlSearch { get; }
	DbSet<UserAgents> UserAgents { get; }
	DbSet<YanportInterruptions> YanportInterruptions { get; }

	DatabaseFacade Database { get; }

	DbSet<TEntity> Set<TEntity>() where TEntity : class;

	EntityEntry Entry(object entity);

	EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

	int SaveChanges();
}
