using System.Collections.Generic;
using ApiPerleRare.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiPerleRare.Models;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
	public virtual DbSet<AdminVersions> AdminVersions { get; set; }

	public virtual DbSet<AgencesYanport> AgencesYanport { get; set; }

	public virtual DbSet<AnnoncesGlobales> AnnoncesGlobales { get; set; }

	public virtual DbSet<AnnoncesGlobalesVerifs> AnnoncesGlobalesVerifs { get; set; }

	public virtual DbSet<Audit> Audit { get; set; }

	public virtual DbSet<Biens> Biens { get; set; }

	public virtual DbSet<Calendar> Calendar { get; set; }

	public virtual DbSet<CategoriesIntermediaires> CategoriesIntermediaires { get; set; }

	public virtual DbSet<CodesPostaux> CodesPostaux { get; set; }

	public virtual DbSet<ConseillersPersonnels> ConseillersPersonnels { get; set; }

	public virtual DbSet<ConseillersPersonnelsEvenements> ConseillersPersonnelsEvenements { get; set; }

	public virtual DbSet<ConseillersPersonnelsTaches> ConseillersPersonnelsTaches { get; set; }

	public virtual DbSet<ConseillersPersonnelsTypesEvenements> ConseillersPersonnelsTypesEvenements { get; set; }

	public virtual DbSet<ContactAgence> ContactAgence { get; set; }

	public virtual DbSet<ContactAgencePublication> ContactAgencePublication { get; set; }

	public virtual DbSet<ContactEvenements> ContactEvenements { get; set; }

	public virtual DbSet<ContactIntermediaire> ContactIntermediaire { get; set; }

	public virtual DbSet<ContactsRecherche> ContactsRecherche { get; set; }

	public virtual DbSet<Dictionnaire> Dictionnaire { get; set; }

	public virtual DbSet<Erreur> Erreur { get; set; }

	public virtual DbSet<Evenements> Evenements { get; set; }

	public virtual DbSet<Formulaires> Formulaires { get; set; }

	public virtual DbSet<HistoriqueAnnonces> HistoriqueAnnonces { get; set; }

	public virtual DbSet<HistoriqueAspiration> HistoriqueAspiration { get; set; }

	public virtual DbSet<HistoriqueInterDirect> HistoriqueInterDirect { get; set; }

	public virtual DbSet<IntermediairesDirects> IntermediairesDirects { get; set; }

	public virtual DbSet<IntermediairesDirectsYanport> IntermediairesDirectsYanport { get; set; }

	public virtual DbSet<IntermediairesIndirects> IntermediairesIndirects { get; set; }

	public virtual DbSet<Ips> Ips { get; set; }

	public virtual DbSet<IpsHost> IpsHost { get; set; }

	public virtual DbSet<Mails> Mails { get; set; }

	public virtual DbSet<MetaMoteurs> MetaMoteurs { get; set; }

	public virtual DbSet<ModelesCommerciaux> ModelesCommerciaux { get; set; }

	public virtual DbSet<MotsCles> MotsCles { get; set; }

	public virtual DbSet<NiveauxDiffusion> NiveauxDiffusion { get; set; }

	public virtual DbSet<NiveauxHabilitation> NiveauxHabilitation { get; set; }

	public virtual DbSet<Notifications> Notifications { get; set; }

	public virtual DbSet<Organisation> Organisation { get; set; }

	public virtual DbSet<Origines> Origines { get; set; }

	public virtual DbSet<Pap> Pap { get; set; }

	public virtual DbSet<PhotosAnnonces> PhotosAnnonces { get; set; }

	public virtual DbSet<Property> Property { get; set; }

	public virtual DbSet<PropertyContact> PropertyContact { get; set; }

	public virtual DbSet<Proxy> Proxy { get; set; }

	public virtual DbSet<ProxyAnonyme> ProxyAnonyme { get; set; }

	public virtual DbSet<QualiteRelationIntermediaire> QualiteRelationIntermediaire { get; set; }

	public virtual DbSet<Quartiers> Quartiers { get; set; }

	public virtual DbSet<RechAutomatique> RechAutomatique { get; set; }

	public virtual DbSet<RechercheGlobalConfig> RechercheGlobalConfig { get; set; }

	public virtual DbSet<ScheduledTaskLogs> ScheduledTaskLogs { get; set; }

	public virtual DbSet<SqlLogs> SqlLogs { get; set; }

	public virtual DbSet<StandingBien> StandingBien { get; set; }

	public virtual DbSet<StatistiquesGenerales> StatistiquesGenerales { get; set; }

	public virtual DbSet<Stats> Stats { get; set; }

	public virtual DbSet<StatutsContacts> StatutsContacts { get; set; }

	public virtual DbSet<StratalisExpired> StratalisExpired { get; set; }

	public virtual DbSet<Taches> Taches { get; set; }

	public virtual DbSet<TagsAnnonce> TagsAnnonce { get; set; }

	public virtual DbSet<Templates> Templates { get; set; }

	public virtual DbSet<TypesBiens> TypesBiens { get; set; }

	public virtual DbSet<TypesCatInterIndirect> TypesCatInterIndirect { get; set; }

	public virtual DbSet<TypesEvenements> TypesEvenements { get; set; }

	public virtual DbSet<TypesMails> TypesMails { get; set; }

	public virtual DbSet<TypesMissions> TypesMissions { get; set; }

	public virtual DbSet<TypesModeles> TypesModeles { get; set; }

	public virtual DbSet<TypesSupprform> TypesSupprform { get; set; }

	public virtual DbSet<TypesTaches> TypesTaches { get; set; }

	public virtual DbSet<UrlLogs> UrlLogs { get; set; }

	public virtual DbSet<UrlSearch> UrlSearch { get; set; }

	public virtual DbSet<UserAgents> UserAgents { get; set; }

	public virtual DbSet<YanportInterruptions> YanportInterruptions { get; set; }

	public ApplicationDbContext()
	{
	}

	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
		: base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.UseCollation("utf8mb4_general_ci").HasCharSet("utf8mb4");
		modelBuilder.Entity(delegate(EntityTypeBuilder<AdminVersions> entity)
		{
			entity.HasKey((AdminVersions e) => e.AvParamName).HasName("PRIMARY");
			entity.ToTable("admin_versions").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.Property((AdminVersions e) => e.AvParamName).HasMaxLength(50).HasColumnName("AV_ParamName");
			entity.Property((AdminVersions e) => e.AvVersion).HasColumnName("AV_Version");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<AgencesYanport> entity)
		{
			entity.HasKey((AgencesYanport e) => e.A).HasName("PRIMARY");
			entity.ToTable("agences_yanport").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.Property((AgencesYanport e) => e.A).ValueGeneratedNever();
			entity.Property((AgencesYanport e) => e.AyTel).IsRequired().HasMaxLength(15)
				.HasColumnName("AY_Tel");
			entity.Property((AgencesYanport e) => e.C).HasMaxLength(111);
			entity.Property((AgencesYanport e) => e.D).HasMaxLength(16);
			entity.Property((AgencesYanport e) => e.E).HasMaxLength(35);
			entity.Property((AgencesYanport e) => e.I).HasMaxLength(6);
			entity.Property((AgencesYanport e) => e.J).HasMaxLength(45);
			entity.Property((AgencesYanport e) => e.L).HasMaxLength(10);
			entity.Property((AgencesYanport e) => e.M).HasMaxLength(9);
			entity.Property((AgencesYanport e) => e.N).HasMaxLength(78);
			entity.Property((AgencesYanport e) => e.O).HasMaxLength(15);
			entity.Property((AgencesYanport e) => e.P).HasMaxLength(23);
			entity.Property((AgencesYanport e) => e.Q).HasMaxLength(23);
			entity.Property((AgencesYanport e) => e.T).HasColumnType("mediumtext");
			entity.Property((AgencesYanport e) => e.U).HasMaxLength(4);
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<AnnoncesGlobales> entity)
		{
			entity.HasKey((AnnoncesGlobales e) => e.AgRef).HasName("PRIMARY");
			entity.ToTable("annonces_globales", delegate(TableBuilder<AnnoncesGlobales> tb)
			{
				tb.HasComment("Regroupe TOUTES les annonces brutes.");
			}).HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.HasIndex((AnnoncesGlobales e) => e.AgCp, "AG_CP");
			entity.HasIndex((AnnoncesGlobales e) => e.AgDateFin, "AG_DateFin");
			entity.HasIndex((AnnoncesGlobales e) => e.AgEtage, "AG_Etage");
			entity.HasIndex((AnnoncesGlobales e) => e.AgIdAnnonceYanport, "AG_IdAnnonceYanport");
			entity.HasIndex((AnnoncesGlobales e) => e.AgIdPropertyYanport, "AG_IdPropertyYanport");
			entity.HasIndex((AnnoncesGlobales e) => e.AgLienId, "AG_LienID").IsUnique();
			entity.HasIndex((AnnoncesGlobales e) => e.AgListeTags, "AG_ListeTags");
			entity.HasIndex((AnnoncesGlobales e) => e.AgNbChambres, "AG_NbChambres");
			entity.HasIndex((AnnoncesGlobales e) => e.AgNbPieces, "AG_NbPieces");
			entity.HasIndex((AnnoncesGlobales e) => e.AgPrix, "AG_Prix");
			entity.HasIndex((AnnoncesGlobales e) => e.AgQuartier2, "AG_Quartier2");
			entity.HasIndex((AnnoncesGlobales e) => e.AgRefMetaMoteur, "AG_RefMetaMoteur");
			entity.HasIndex((AnnoncesGlobales e) => e.AgRefAgc, "AG_Ref_AGC");
			entity.HasIndex((AnnoncesGlobales e) => e.AgSurface, "AG_Surface");
			entity.HasIndex((AnnoncesGlobales e) => e.AgType, "AG_Type");
			entity.HasIndex((AnnoncesGlobales e) => e.AgTypeTransaction, "AG_TypeTransaction");
			entity.HasIndex((AnnoncesGlobales e) => e.AgLien, "ASL_Lien").IsUnique();
			entity.Property((AnnoncesGlobales e) => e.AgRef).HasColumnName("AG_Ref");
			entity.Property((AnnoncesGlobales e) => e.AgAdresseLat).HasComment("yanport: lat").HasColumnName("AG_AdresseLat");
			entity.Property((AnnoncesGlobales e) => e.AgAdresseLon).HasComment("yanport: lon").HasColumnName("AG_AdresseLon");
			entity.Property((AnnoncesGlobales e) => e.AgAdresseNum).HasMaxLength(10).HasComment("yanport: StreetNumber")
				.HasColumnName("AG_AdresseNum");
			entity.Property((AnnoncesGlobales e) => e.AgAdresseRue).IsRequired().HasMaxLength(100)
				.HasComment("yanport: Street")
				.HasColumnName("AG_AdresseRue");
			entity.Property((AnnoncesGlobales e) => e.AgAnnee).HasComment("yanport: FloorCount").HasColumnName("AG_Annee");
			entity.Property((AnnoncesGlobales e) => e.AgAnnonceur).HasMaxLength(50).HasColumnName("AG_Annonceur");
			entity.Property((AnnoncesGlobales e) => e.AgAsc).HasComment("yanport: ELEVATOR").HasColumnName("AG_Asc");
			entity.Property((AnnoncesGlobales e) => e.AgChampsVerrouilles).HasComment("Permet de stocker les champs qui sont verrouilles car modifies a la main").HasColumnType("set('AG_Ref_AGC','AG_Lien','AG_LienID','AG_DateDebut','AG_DateFin','AG_TypeTransaction','AG_Type','AG_Image','AG_Prix','AG_NbPieces','AG_Surface','AG_CP','AG_Etage','AG_NbChambres','AG_Desc','AG_EstExclusif','AG_EstDernierEtage','AG_Annonceur','AG_TelAnnonceur','AG_Quartier2','AG_ListeTags')")
				.HasColumnName("AG_ChampsVerrouilles")
				.UseCollation("ascii_bin")
				.HasCharSet("ascii");
			entity.Property((AnnoncesGlobales e) => e.AgConsumptionLetter).HasMaxLength(3).HasColumnName("AG_ConsumptionLetter");
			entity.Property((AnnoncesGlobales e) => e.AgCp).IsRequired().HasMaxLength(10)
				.HasColumnName("AG_CP")
				.UseCollation("ascii_bin")
				.HasCharSet("ascii");
			entity.Property((AnnoncesGlobales e) => e.AgDateDebut).HasColumnName("AG_DateDebut");
			entity.Property((AnnoncesGlobales e) => e.AgDateFin).HasColumnType("datetime").HasColumnName("AG_DateFin");
			entity.Property((AnnoncesGlobales e) => e.AgDesc).HasMaxLength(3000).HasColumnName("AG_Desc");
			entity.Property((AnnoncesGlobales e) => e.AgEmailAnnonceur).HasMaxLength(100).HasColumnName("AG_EmailAnnonceur");
			entity.Property((AnnoncesGlobales e) => e.AgEstDernierEtage).HasColumnName("AG_EstDernierEtage");
			entity.Property((AnnoncesGlobales e) => e.AgEstExclusif).HasColumnName("AG_EstExclusif");
			entity.Property((AnnoncesGlobales e) => e.AgEstOccupe).HasColumnName("AG_EstOccupe");
			entity.Property((AnnoncesGlobales e) => e.AgEstRecent).HasComment("yanport: newbuild").HasColumnName("AG_EstRecent");
			entity.Property((AnnoncesGlobales e) => e.AgEtage).HasColumnName("AG_Etage");
			entity.Property((AnnoncesGlobales e) => e.AgGreenhouseGasConsumptionLetter).HasMaxLength(3).HasColumnName("AG_GreenhouseGasConsumptionLetter");
			entity.Property((AnnoncesGlobales e) => e.AgIdAgenceYanport).HasColumnName("AG_IdAgenceYanport");
			entity.Property((AnnoncesGlobales e) => e.AgIdAnnonceYanport).HasMaxLength(40).HasColumnName("AG_IdAnnonceYanport");
			entity.Property((AnnoncesGlobales e) => e.AgIdPropertyYanport).HasMaxLength(40).HasColumnName("AG_IdPropertyYanport");
			entity.Property((AnnoncesGlobales e) => e.AgIdQuartier).HasColumnName("AG_IdQuartier");
			entity.Property((AnnoncesGlobales e) => e.AgIdVille).HasColumnName("AG_IdVille");
			entity.Property((AnnoncesGlobales e) => e.AgImage).HasMaxLength(255).HasColumnName("AG_Image")
				.UseCollation("latin1_general_ci")
				.HasCharSet("latin1");
			entity.Property((AnnoncesGlobales e) => e.AgLien).IsRequired().HasColumnName("AG_Lien")
				.UseCollation("latin1_bin")
				.HasCharSet("latin1");
			entity.Property((AnnoncesGlobales e) => e.AgLienId).HasColumnName("AG_LienID").UseCollation("latin1_bin")
				.HasCharSet("latin1");
			entity.Property((AnnoncesGlobales e) => e.AgListeTags).HasMaxLength(100).HasComment("Liste de references de la table tags_annonce ")
				.HasColumnName("AG_ListeTags")
				.UseCollation("latin1_general_ci")
				.HasCharSet("latin1");
			entity.Property((AnnoncesGlobales e) => e.AgNbChambres).HasColumnName("AG_NbChambres");
			entity.Property((AnnoncesGlobales e) => e.AgNbEtages).HasComment("yanport: FloorCount").HasColumnName("AG_NbEtages");
			entity.Property((AnnoncesGlobales e) => e.AgNbPieces).HasColumnName("AG_NbPieces");
			entity.Property((AnnoncesGlobales e) => e.AgPrix).HasColumnName("AG_Prix");
			entity.Property((AnnoncesGlobales e) => e.AgQuartier).IsRequired().HasMaxLength(100)
				.HasColumnName("AG_Quartier")
				.UseCollation("latin1_general_ci")
				.HasCharSet("latin1");
			entity.Property((AnnoncesGlobales e) => e.AgQuartier2).HasMaxLength(100).HasComment("Liste de references de la table quartiers")
				.HasColumnName("AG_Quartier2")
				.UseCollation("latin1_general_ci")
				.HasCharSet("latin1");
			entity.Property((AnnoncesGlobales e) => e.AgRefAgc).HasColumnName("AG_Ref_AGC");
			entity.Property((AnnoncesGlobales e) => e.AgRefMetaMoteur).HasColumnName("AG_RefMetaMoteur");
			entity.Property((AnnoncesGlobales e) => e.AgSsTypeAnnonceur).HasMaxLength(20).HasColumnName("AG_SsTypeAnnonceur");
			entity.Property((AnnoncesGlobales e) => e.AgSurface).HasColumnName("AG_Surface");
			entity.Property((AnnoncesGlobales e) => e.AgTelAnnonceur).HasMaxLength(14).HasColumnName("AG_TelAnnonceur")
				.UseCollation("latin1_general_ci")
				.HasCharSet("latin1");
			entity.Property((AnnoncesGlobales e) => e.AgTime).HasColumnType("datetime").HasColumnName("AG_Time");
			entity.Property((AnnoncesGlobales e) => e.AgTimeEndY).HasComment("yanport: publicationEndDate").HasColumnType("datetime")
				.HasColumnName("AG_TimeEndY");
			entity.Property((AnnoncesGlobales e) => e.AgTimeY).HasComment("yanport: publicationStartDate").HasColumnType("datetime")
				.HasColumnName("AG_TimeY");
			entity.Property((AnnoncesGlobales e) => e.AgType).IsRequired().HasMaxLength(50)
				.HasDefaultValueSql("'Appartement'")
				.HasColumnName("AG_Type")
				.UseCollation("latin1_general_ci")
				.HasCharSet("latin1");
			entity.Property((AnnoncesGlobales e) => e.AgTypeAnnonceur).HasMaxLength(20).HasColumnName("AG_TypeAnnonceur");
			entity.Property((AnnoncesGlobales e) => e.AgTypeTransaction).IsRequired().HasColumnType("enum('A','L')")
				.HasColumnName("AG_TypeTransaction")
				.UseCollation("ascii_bin")
				.HasCharSet("ascii");
			entity.HasOne((AnnoncesGlobales d) => d.AgRefMetaMoteurNavigation).WithMany((MetaMoteurs p) => p.AnnoncesGlobales).HasForeignKey((AnnoncesGlobales d) => d.AgRefMetaMoteur)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("annonces_globales_ibfk_5");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<AnnoncesGlobalesVerifs> entity)
		{
			entity.HasKey((AnnoncesGlobalesVerifs e) => e.AgvRef).HasName("PRIMARY");
			entity.ToTable("annonces_globales_verifs").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.HasIndex((AnnoncesGlobalesVerifs e) => e.AgvDate, "AGV_Date");
			entity.HasIndex((AnnoncesGlobalesVerifs e) => e.AgvStatut, "AGV_Statut");
			entity.Property((AnnoncesGlobalesVerifs e) => e.AgvRef).ValueGeneratedNever().HasColumnName("AGV_Ref");
			entity.Property((AnnoncesGlobalesVerifs e) => e.AgvDate).HasColumnName("AGV_Date");
			entity.Property((AnnoncesGlobalesVerifs e) => e.AgvStatut).IsRequired().HasColumnType("enum('Verif_Perim','Verif_Perim_OK','Verif_Perim_KO')")
				.HasColumnName("AGV_Statut");
			entity.HasOne((AnnoncesGlobalesVerifs d) => d.AgvRefNavigation).WithOne((AnnoncesGlobales p) => p.AnnoncesGlobalesVerifs).HasForeignKey((AnnoncesGlobalesVerifs d) => d.AgvRef)
				.HasConstraintName("annonces_globales_verifs_ibfk_1");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Audit> entity)
		{
			entity.HasKey((Audit e) => e.AId).HasName("PRIMARY");
			entity.ToTable("audit").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.Property((Audit e) => e.AId).HasColumnName("A_Id");
			entity.Property((Audit e) => e.ADate).HasColumnType("datetime").HasColumnName("A_Date");
			entity.Property((Audit e) => e.AIpsource).IsRequired().HasMaxLength(16)
				.HasColumnName("A_IPsource");
			entity.Property((Audit e) => e.ALogin).IsRequired().HasMaxLength(20)
				.HasColumnName("A_Login");
			entity.Property((Audit e) => e.ATable).IsRequired().HasMaxLength(30)
				.HasColumnName("A_Table");
			entity.Property((Audit e) => e.AType).IsRequired().HasMaxLength(20)
				.HasColumnName("A_Type");
			entity.Property((Audit e) => e.AValue).IsRequired().HasMaxLength(1000)
				.HasColumnName("A_Value");
			entity.Property((Audit e) => e.AWhere).IsRequired().HasMaxLength(500)
				.HasColumnName("A_Where");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Biens> entity)
		{
			entity.HasKey((Biens e) => e.BRef).HasName("PRIMARY");
			entity.ToTable("biens").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.HasIndex((Biens e) => e.BRefCtcInter, "FK_biens_contact_intermediaire");
			entity.HasIndex((Biens e) => e.BRefInterD, "FK_biens_intermediaires_directs");
			entity.HasIndex((Biens e) => e.BRefInterI, "FK_biens_intermediaires_indirects");
			entity.Property((Biens e) => e.BRef).HasColumnName("B_Ref");
			entity.Property((Biens e) => e.BAdresse).IsRequired().HasMaxLength(50)
				.HasColumnName("B_Adresse");
			entity.Property((Biens e) => e.BCp).IsRequired().HasMaxLength(5)
				.HasColumnName("B_CP");
			entity.Property((Biens e) => e.BRefAnnAgc).HasColumnName("B_RefAnnAGC");
			entity.Property((Biens e) => e.BRefCtcInter).HasColumnName("B_RefCtcInter");
			entity.Property((Biens e) => e.BRefInterD).HasColumnName("B_RefInterD");
			entity.Property((Biens e) => e.BRefInterI).HasColumnName("B_RefInterI");
			entity.HasOne((Biens d) => d.BRefCtcInterNavigation).WithMany((ContactIntermediaire p) => p.Biens).HasForeignKey((Biens d) => d.BRefCtcInter)
				.HasConstraintName("FK_biens_contact_intermediaire");
			entity.HasOne((Biens d) => d.BRefInterDNavigation).WithMany((IntermediairesDirects p) => p.Biens).HasForeignKey((Biens d) => d.BRefInterD)
				.HasConstraintName("FK_biens_intermediaires_directs");
			entity.HasOne((Biens d) => d.BRefInterINavigation).WithMany((IntermediairesIndirects p) => p.Biens).HasForeignKey((Biens d) => d.BRefInterI)
				.HasConstraintName("FK_biens_intermediaires_indirects");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Calendar> entity)
		{
			entity.HasKey((Calendar e) => e.Id).HasName("PRIMARY");
			entity.ToTable("calendar").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.HasIndex((Calendar e) => e.DateV, "days").IsUnique();
			entity.Property((Calendar e) => e.Id).HasColumnName("id");
			entity.Property((Calendar e) => e.DateV).HasColumnName("dateV");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<CategoriesIntermediaires> entity)
		{
			entity.HasKey((CategoriesIntermediaires e) => e.Num).HasName("PRIMARY");
			entity.ToTable("categories_intermediaires").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.Property((CategoriesIntermediaires e) => e.Categories).HasMaxLength(50);
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<CodesPostaux> entity)
		{
			entity.HasKey((CodesPostaux e) => e.CpId).HasName("PRIMARY");
			entity.ToTable("codes_postaux").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.Property((CodesPostaux e) => e.CpId).ValueGeneratedNever().HasColumnType("mediumint unsigned")
				.HasColumnName("CP_Id");
			entity.Property((CodesPostaux e) => e.CpActif).IsRequired().HasDefaultValueSql("'1'")
				.HasColumnName("CP_Actif");
			entity.Property((CodesPostaux e) => e.CpAnnoncesjaunes).IsRequired().HasMaxLength(255)
				.HasColumnName("CP_annoncesjaunes");
			entity.Property((CodesPostaux e) => e.CpAvendrealouer).IsRequired().HasMaxLength(255)
				.HasColumnName("CP_avendrealouer");
			entity.Property((CodesPostaux e) => e.CpExplorimmo).IsRequired().HasMaxLength(255)
				.HasColumnName("CP_explorimmo");
			entity.Property((CodesPostaux e) => e.CpIdYanport).HasColumnName("CP_IdYanport");
			entity.Property((CodesPostaux e) => e.CpInsee).HasColumnType("mediumint").HasColumnName("CP_INSEE");
			entity.Property((CodesPostaux e) => e.CpLeboncoin).IsRequired().HasMaxLength(255)
				.HasColumnName("CP_leboncoin");
			entity.Property((CodesPostaux e) => e.CpLogicimmo).IsRequired().HasMaxLength(255)
				.HasColumnName("CP_logicimmo");
			entity.Property((CodesPostaux e) => e.CpNom).IsRequired().HasMaxLength(50)
				.HasDefaultValueSql("''")
				.HasColumnName("CP_Nom")
				.UseCollation("latin1_general_ci")
				.HasCharSet("latin1");
			entity.Property((CodesPostaux e) => e.CpPap).IsRequired().HasMaxLength(255)
				.HasColumnName("CP_pap");
			entity.Property((CodesPostaux e) => e.CpRefleximmo).IsRequired().HasMaxLength(255)
				.HasColumnName("CP_refleximmo");
			entity.Property((CodesPostaux e) => e.CpSeloger).IsRequired().HasMaxLength(255)
				.HasColumnName("CP_seloger");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<ConseillersPersonnels> entity)
		{
			entity.HasKey((ConseillersPersonnels e) => e.CpRefConseiller).HasName("PRIMARY");
			entity.ToTable("conseillers_personnels").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.HasIndex((ConseillersPersonnels e) => e.CpMel, "CP_Mel");
			entity.HasIndex((ConseillersPersonnels e) => e.CpNomFamille, "CP_NomFamille");
			entity.Property((ConseillersPersonnels e) => e.CpRefConseiller).HasColumnName("CP_RefConseiller");
			entity.Property((ConseillersPersonnels e) => e.CpAccueilPos).HasMaxLength(350).HasColumnName("CP_Accueil_Pos");
			entity.Property((ConseillersPersonnels e) => e.CpActif).HasDefaultValueSql("'0'").HasColumnName("CP_Actif");
			entity.Property((ConseillersPersonnels e) => e.CpAdmin).HasColumnName("CP_Admin");
			entity.Property((ConseillersPersonnels e) => e.CpAdresse).IsRequired().HasMaxLength(100)
				.HasColumnName("CP_Adresse");
			entity.Property((ConseillersPersonnels e) => e.CpAutoLogin).HasMaxLength(15).HasColumnName("CP_AutoLogin");
			entity.Property((ConseillersPersonnels e) => e.CpCommentaire).HasMaxLength(2000).HasColumnName("CP_Commentaire");
			entity.Property((ConseillersPersonnels e) => e.CpCp).HasColumnName("CP_CP");
			entity.Property((ConseillersPersonnels e) => e.CpCv).HasMaxLength(100).HasColumnName("CP_CV");
			entity.Property((ConseillersPersonnels e) => e.CpDateContrat).HasColumnName("CP_DateContrat");
			entity.Property((ConseillersPersonnels e) => e.CpDateCreation).HasColumnName("CP_DateCreation");
			entity.Property((ConseillersPersonnels e) => e.CpDateDebut).HasColumnName("CP_DateDebut");
			entity.Property((ConseillersPersonnels e) => e.CpDateFin).HasColumnName("CP_DateFin");
			entity.Property((ConseillersPersonnels e) => e.CpDateNaissance).HasColumnName("CP_DateNaissance");
			entity.Property((ConseillersPersonnels e) => e.CpDateSignatureContrat).HasColumnName("CP_DateSignatureContrat");
			entity.Property((ConseillersPersonnels e) => e.CpDispo).HasDefaultValueSql("'1'").HasColumnName("CP_Dispo");
			entity.Property((ConseillersPersonnels e) => e.CpEnSociete).HasColumnName("CP_EnSociete");
			entity.Property((ConseillersPersonnels e) => e.CpFonction).HasMaxLength(50).HasColumnName("CP_Fonction");
			entity.Property((ConseillersPersonnels e) => e.CpInitiales).HasMaxLength(3).HasColumnName("CP_Initiales");
			entity.Property((ConseillersPersonnels e) => e.CpLibelle).HasMaxLength(50).HasColumnName("CP_Libelle");
			entity.Property((ConseillersPersonnels e) => e.CpLogin).HasMaxLength(15).HasColumnName("CP_Login");
			entity.Property((ConseillersPersonnels e) => e.CpMandat).IsRequired().HasMaxLength(100)
				.HasColumnName("CP_Mandat");
			entity.Property((ConseillersPersonnels e) => e.CpMel).HasMaxLength(50).HasColumnName("CP_Mel");
			entity.Property((ConseillersPersonnels e) => e.CpMelMotDePasse).HasMaxLength(15).HasColumnName("CP_MelMotDePasse");
			entity.Property((ConseillersPersonnels e) => e.CpMelPerso).IsRequired().HasMaxLength(100)
				.HasColumnName("CP_MelPerso");
			entity.Property((ConseillersPersonnels e) => e.CpMelSignature).HasMaxLength(10000).HasColumnName("CP_MelSignature");
			entity.Property((ConseillersPersonnels e) => e.CpMotDePasse).HasMaxLength(15).HasColumnName("CP_MotDePasse");
			entity.Property((ConseillersPersonnels e) => e.CpNegociateur).HasColumnName("CP_Negociateur");
			entity.Property((ConseillersPersonnels e) => e.CpNomFamille).HasMaxLength(50).HasColumnName("CP_NomFamille");
			entity.Property((ConseillersPersonnels e) => e.CpNumrsac).HasMaxLength(20).HasColumnName("CP_Numrsac");
			entity.Property((ConseillersPersonnels e) => e.CpPhoto).IsRequired().HasMaxLength(100)
				.HasColumnName("CP_Photo");
			entity.Property((ConseillersPersonnels e) => e.CpPhotoSignature).HasMaxLength(100).HasColumnName("CP_PhotoSignature");
			entity.Property((ConseillersPersonnels e) => e.CpPhotographie).HasColumnName("CP_Photographie");
			entity.Property((ConseillersPersonnels e) => e.CpPieceIdentite).IsRequired().HasMaxLength(100)
				.HasColumnName("CP_PieceIdentite");
			entity.Property((ConseillersPersonnels e) => e.CpPortage).HasDefaultValueSql("'0'").HasColumnName("CP_Portage");
			entity.Property((ConseillersPersonnels e) => e.CpPrenom).HasMaxLength(50).HasColumnName("CP_Prenom");
			entity.Property((ConseillersPersonnels e) => e.CpRsac).IsRequired().HasMaxLength(100)
				.HasColumnName("CP_RSAC");
			entity.Property((ConseillersPersonnels e) => e.CpSiret).HasColumnName("CP_SIRET");
			entity.Property((ConseillersPersonnels e) => e.CpStatut).HasMaxLength(30).HasColumnName("CP_Statut");
			entity.Property((ConseillersPersonnels e) => e.CpTelPersonnel).HasMaxLength(30).HasColumnName("CP_TelPersonnel");
			entity.Property((ConseillersPersonnels e) => e.CpTitre).HasMaxLength(20).HasColumnName("CP_Titre");
			entity.Property((ConseillersPersonnels e) => e.CpTva).HasColumnName("CP_TVA");
			entity.Property((ConseillersPersonnels e) => e.CpVille).IsRequired().HasMaxLength(30)
				.HasColumnName("CP_Ville");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<ConseillersPersonnelsEvenements> entity)
		{
			entity.HasKey((ConseillersPersonnelsEvenements e) => e.CpeRefEvenement).HasName("PRIMARY");
			entity.ToTable("conseillers_personnels_evenements").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.HasIndex((ConseillersPersonnelsEvenements e) => e.CpeRefConseiller, "FK_conseillers_personnels_evenements_conseillers_personnels");
			entity.Property((ConseillersPersonnelsEvenements e) => e.CpeRefEvenement).HasColumnName("CPE_RefEvenement");
			entity.Property((ConseillersPersonnelsEvenements e) => e.CpeCommentaire).IsRequired().HasMaxLength(100)
				.HasColumnName("CPE_Commentaire");
			entity.Property((ConseillersPersonnelsEvenements e) => e.CpeDateCreation).HasColumnName("CPE_DateCreation");
			entity.Property((ConseillersPersonnelsEvenements e) => e.CpeDateRealisation).HasColumnName("CPE_DateRealisation");
			entity.Property((ConseillersPersonnelsEvenements e) => e.CpeRefConseiller).HasColumnName("CPE_RefConseiller");
			entity.Property((ConseillersPersonnelsEvenements e) => e.CpeStatut).HasDefaultValueSql("'1'").HasColumnName("CPE_Statut");
			entity.Property((ConseillersPersonnelsEvenements e) => e.CpeTypeEvenement).HasMaxLength(50).HasColumnName("CPE_TypeEvenement");
			entity.HasOne((ConseillersPersonnelsEvenements d) => d.CpeRefConseillerNavigation).WithMany((ConseillersPersonnels p) => p.ConseillersPersonnelsEvenements).HasForeignKey((ConseillersPersonnelsEvenements d) => d.CpeRefConseiller)
				.OnDelete(DeleteBehavior.Cascade)
				.HasConstraintName("FK_conseillers_personnels_evenements_conseillers_personnels");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<ConseillersPersonnelsTaches> entity)
		{
			entity.HasKey((ConseillersPersonnelsTaches e) => e.CptRef).HasName("PRIMARY");
			entity.ToTable("conseillers_personnels_taches").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.HasIndex((ConseillersPersonnelsTaches e) => e.CptRefConseiller, "FK_conseillers_personnels_taches_conseillers_personnels");
			entity.Property((ConseillersPersonnelsTaches e) => e.CptRef).HasColumnName("CPT_Ref");
			entity.Property((ConseillersPersonnelsTaches e) => e.CptCom).IsRequired().HasMaxLength(300)
				.HasColumnName("CPT_Com");
			entity.Property((ConseillersPersonnelsTaches e) => e.CptDateCreation).HasColumnName("CPT_Date_Creation");
			entity.Property((ConseillersPersonnelsTaches e) => e.CptDateRealisation).HasColumnName("CPT_Date_Realisation");
			entity.Property((ConseillersPersonnelsTaches e) => e.CptEtat).IsRequired().HasMaxLength(20)
				.HasColumnName("CPT_Etat");
			entity.Property((ConseillersPersonnelsTaches e) => e.CptLien).IsRequired().HasMaxLength(300)
				.HasColumnName("CPT_Lien");
			entity.Property((ConseillersPersonnelsTaches e) => e.CptQui).IsRequired().HasMaxLength(100)
				.HasColumnName("CPT_Qui");
			entity.Property((ConseillersPersonnelsTaches e) => e.CptRefConseiller).HasColumnName("CPT_RefConseiller");
			entity.Property((ConseillersPersonnelsTaches e) => e.CptType).IsRequired().HasMaxLength(20)
				.HasColumnName("CPT_Type");
			entity.HasOne((ConseillersPersonnelsTaches d) => d.CptRefConseillerNavigation).WithMany((ConseillersPersonnels p) => p.ConseillersPersonnelsTaches).HasForeignKey((ConseillersPersonnelsTaches d) => d.CptRefConseiller)
				.HasConstraintName("FK_conseillers_personnels_taches_conseillers_personnels");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<ConseillersPersonnelsTypesEvenements> entity)
		{
			entity.HasKey((ConseillersPersonnelsTypesEvenements e) => e.CpteRefTypeEvenement).HasName("PRIMARY");
			entity.ToTable("conseillers_personnels_types_evenements").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.Property((ConseillersPersonnelsTypesEvenements e) => e.CpteRefTypeEvenement).HasDefaultValueSql("'0'").HasColumnType("mediumint")
				.HasColumnName("CPTE_RefTypeEvenement");
			entity.Property((ConseillersPersonnelsTypesEvenements e) => e.CpteCategorieEvenement).HasMaxLength(50).HasColumnName("CPTE_CategorieEvenement");
			entity.Property((ConseillersPersonnelsTypesEvenements e) => e.CpteTypeEvenement).HasMaxLength(50).HasColumnName("CPTE_TypeEvenement");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<ContactAgence> entity)
		{
			entity.HasKey((ContactAgence e) => new { e.CaNic, e.CaSiren }).HasName("PRIMARY").HasAnnotation("MySql:IndexPrefixLength", new int[2]);
			entity.ToTable("contact_agence").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.Property((ContactAgence e) => e.CaNic).HasMaxLength(50).HasColumnName("ca_nic");
			entity.Property((ContactAgence e) => e.CaSiren).HasMaxLength(50).HasColumnName("ca_siren");
			entity.Property((ContactAgence e) => e.CaAdresse).IsRequired().HasMaxLength(100)
				.HasColumnName("ca_adresse");
			entity.Property((ContactAgence e) => e.CaDesc).IsRequired().HasColumnType("text")
				.HasColumnName("ca_desc");
			entity.Property((ContactAgence e) => e.CaFax).IsRequired().HasMaxLength(50)
				.HasColumnName("ca_fax");
			entity.Property((ContactAgence e) => e.CaNom).IsRequired().HasMaxLength(50)
				.HasColumnName("ca_nom");
			entity.Property((ContactAgence e) => e.CaTel).IsRequired().HasMaxLength(15)
				.HasColumnName("ca_tel");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<ContactAgencePublication> entity)
		{
			entity.HasKey((ContactAgencePublication e) => e.CapIdpublication).HasName("PRIMARY");
			entity.ToTable("contact_agence_publication").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.Property((ContactAgencePublication e) => e.CapIdpublication).ValueGeneratedNever().HasColumnName("cap_idpublication");
			entity.Property((ContactAgencePublication e) => e.CapNic).IsRequired().HasMaxLength(50)
				.HasColumnName("cap_nic");
			entity.Property((ContactAgencePublication e) => e.CapSiren).IsRequired().HasMaxLength(50)
				.HasColumnName("cap_siren");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<ContactEvenements> entity)
		{
			entity.HasNoKey().ToView("contact_evenements");
			entity.Property((ContactEvenements e) => e.BAdresse).HasMaxLength(50).HasColumnName("B_Adresse")
				.UseCollation("utf8mb3_general_ci")
				.HasCharSet("utf8mb3");
			entity.Property((ContactEvenements e) => e.BCp).HasMaxLength(5).HasColumnName("B_CP")
				.UseCollation("utf8mb3_general_ci")
				.HasCharSet("utf8mb3");
			entity.Property((ContactEvenements e) => e.BRef).HasDefaultValueSql("'0'").HasColumnName("B_Ref");
			entity.Property((ContactEvenements e) => e.CNom).HasMaxLength(30).HasColumnName("C_Nom")
				.UseCollation("utf8mb3_general_ci")
				.HasCharSet("utf8mb3");
			entity.Property((ContactEvenements e) => e.CNomIntermediaire).HasMaxLength(50).HasColumnName("C_NomIntermediaire")
				.UseCollation("utf8mb3_general_ci")
				.HasCharSet("utf8mb3");
			entity.Property((ContactEvenements e) => e.CPrenom).HasMaxLength(30).HasColumnName("C_Prenom")
				.UseCollation("utf8mb3_general_ci")
				.HasCharSet("utf8mb3");
			entity.Property((ContactEvenements e) => e.CRefCtcInter).HasDefaultValueSql("'0'").HasColumnName("C_RefCtcInter");
			entity.Property((ContactEvenements e) => e.ECr).IsRequired().HasMaxLength(255)
				.HasColumnName("E_CR")
				.UseCollation("utf8mb3_general_ci")
				.HasCharSet("utf8mb3");
			entity.Property((ContactEvenements e) => e.EDate).HasColumnType("datetime").HasColumnName("E_Date");
			entity.Property((ContactEvenements e) => e.EMail).HasMaxLength(255).HasComment("N'est utilisé que par les types 'MAIL OUT/IN' (ridicule car la colonne E_CR aurait très bien joué le rôle)")
				.HasColumnName("E_Mail")
				.UseCollation("utf8mb3_general_ci")
				.HasCharSet("utf8mb3");
			entity.Property((ContactEvenements e) => e.ERefBien).HasColumnName("E_RefBien");
			entity.Property((ContactEvenements e) => e.ERefContact).HasColumnName("E_RefContact");
			entity.Property((ContactEvenements e) => e.ERefEvenement).HasColumnName("E_RefEvenement");
			entity.Property((ContactEvenements e) => e.ERefInterD).HasColumnName("E_RefInterD");
			entity.Property((ContactEvenements e) => e.ERefInterI).HasColumnName("E_RefInterI");
			entity.Property((ContactEvenements e) => e.ETexte).IsRequired().HasMaxLength(100)
				.HasColumnName("E_Texte")
				.UseCollation("utf8mb3_general_ci")
				.HasCharSet("utf8mb3");
			entity.Property((ContactEvenements e) => e.ETypeEvenement).HasMaxLength(50).HasColumnName("E_TypeEvenement")
				.UseCollation("latin1_swedish_ci")
				.HasCharSet("latin1");
			entity.Property((ContactEvenements e) => e.I2Icon).HasMaxLength(255).HasColumnName("I2_Icon")
				.UseCollation("latin1_swedish_ci")
				.HasCharSet("latin1");
			entity.Property((ContactEvenements e) => e.I2NomIntermIndirect).HasMaxLength(50).HasDefaultValueSql("''")
				.HasColumnName("I2_NomInterm_indirect")
				.UseCollation("latin1_swedish_ci")
				.HasCharSet("latin1");
			entity.Property((ContactEvenements e) => e.I2RefIntermIndirect).HasDefaultValueSql("'0'").HasColumnName("I2_RefInterm_indirect");
			entity.Property((ContactEvenements e) => e.INomIntermediaire).HasMaxLength(50).HasDefaultValueSql("''")
				.HasColumnName("I_NomIntermediaire")
				.UseCollation("utf8mb3_general_ci")
				.HasCharSet("utf8mb3");
			entity.Property((ContactEvenements e) => e.IRefIntermediaire).HasDefaultValueSql("'0'").HasColumnName("I_RefIntermediaire");
			entity.Property((ContactEvenements e) => e.QSigneCtcInter).HasColumnType("text").HasColumnName("Q_signe_CtcInter")
				.UseCollation("latin1_swedish_ci")
				.HasCharSet("latin1");
			entity.Property((ContactEvenements e) => e.QSigneInterD).HasColumnType("text").HasColumnName("Q_signe_InterD")
				.UseCollation("latin1_swedish_ci")
				.HasCharSet("latin1");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<ContactIntermediaire> entity)
		{
			entity.HasKey((ContactIntermediaire e) => e.CRefCtcInter).HasName("PRIMARY");
			entity.ToTable("contact_intermediaire").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.HasIndex((ContactIntermediaire e) => e.CRefIntermediaire, "C_RefIntermediaire");
			entity.HasIndex((ContactIntermediaire e) => e.CStatut, "C_Statut");
			entity.HasIndex((ContactIntermediaire e) => e.CTel, "C_Tel");
			entity.Property((ContactIntermediaire e) => e.CRefCtcInter).HasColumnName("C_RefCtcInter");
			entity.Property((ContactIntermediaire e) => e.CCom).IsRequired().HasMaxLength(100)
				.HasColumnName("C_Com");
			entity.Property((ContactIntermediaire e) => e.CDirecteur).HasColumnName("C_Directeur");
			entity.Property((ContactIntermediaire e) => e.CMel).IsRequired().HasMaxLength(50)
				.HasColumnName("C_Mel");
			entity.Property((ContactIntermediaire e) => e.CNom).IsRequired().HasMaxLength(30)
				.HasColumnName("C_Nom");
			entity.Property((ContactIntermediaire e) => e.CNomIntermediaire).IsRequired().HasMaxLength(50)
				.HasColumnName("C_NomIntermediaire");
			entity.Property((ContactIntermediaire e) => e.CPasDeMails).HasColumnName("C_PasDeMails");
			entity.Property((ContactIntermediaire e) => e.CPhoto).HasMaxLength(300).HasColumnName("C_Photo");
			entity.Property((ContactIntermediaire e) => e.CPrenom).IsRequired().HasMaxLength(30)
				.HasColumnName("C_Prenom");
			entity.Property((ContactIntermediaire e) => e.CQualiteRelation).IsRequired().HasMaxLength(50)
				.HasColumnName("C_QualiteRelation");
			entity.Property((ContactIntermediaire e) => e.CRefIntermediaire).HasColumnName("C_RefIntermediaire");
			entity.Property((ContactIntermediaire e) => e.CRepondMailRecherche).HasColumnName("C_RepondMailRecherche");
			entity.Property((ContactIntermediaire e) => e.CStatut).IsRequired().HasMaxLength(10)
				.HasColumnName("C_Statut");
			entity.Property((ContactIntermediaire e) => e.CTel).IsRequired().HasMaxLength(15)
				.HasColumnName("C_Tel");
			entity.Property((ContactIntermediaire e) => e.CUrl).HasMaxLength(300).HasColumnName("C_Url");
			entity.HasOne((ContactIntermediaire d) => d.CRefIntermediaireNavigation).WithMany((IntermediairesDirects p) => p.ContactIntermediaire).HasForeignKey((ContactIntermediaire d) => d.CRefIntermediaire)
				.OnDelete(DeleteBehavior.SetNull)
				.HasConstraintName("contact_intermediaire_ibfk_1");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<ContactsRecherche> entity)
		{
			entity.HasKey((ContactsRecherche e) => e.CRefContact).HasName("PRIMARY");
			entity.ToTable("contacts_recherche").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.HasIndex((ContactsRecherche e) => e.CRefFormulaire, "FK_Formulaire");
			entity.HasIndex((ContactsRecherche e) => new { e.CApporteur, e.C2emeApporteur, e.CNegociateur, e.C2emeNegociateur, e.CNomFamilleConseiller, e.C2emeConseiller }, "Groupe");
			entity.Property((ContactsRecherche e) => e.CRefContact).HasColumnName("C_RefContact");
			entity.Property((ContactsRecherche e) => e.C2emeApporteur).IsRequired().HasMaxLength(15)
				.HasDefaultValueSql("''")
				.HasColumnName("C_2emeApporteur");
			entity.Property((ContactsRecherche e) => e.C2emeConseiller).IsRequired().HasMaxLength(15)
				.HasDefaultValueSql("''")
				.HasColumnName("C_2emeConseiller");
			entity.Property((ContactsRecherche e) => e.C2emeNegociateur).IsRequired().HasMaxLength(15)
				.HasDefaultValueSql("''")
				.HasColumnName("C_2emeNegociateur");
			entity.Property((ContactsRecherche e) => e.CAdresse).HasMaxLength(255).HasColumnName("C_Adresse");
			entity.Property((ContactsRecherche e) => e.CAdresseBienFacture).HasMaxLength(150).HasColumnName("C_AdresseBienFacture");
			entity.Property((ContactsRecherche e) => e.CAlerteMail).HasColumnName("C_AlerteMail");
			entity.Property((ContactsRecherche e) => e.CAnciennete).HasMaxLength(100).HasColumnName("C_Anciennete")
				.UseCollation("latin1_bin");
			entity.Property((ContactsRecherche e) => e.CApporteur).IsRequired().HasMaxLength(50)
				.HasDefaultValueSql("''")
				.HasColumnName("C_Apporteur");
			entity.Property((ContactsRecherche e) => e.CBudget).HasMaxLength(100).HasColumnName("C_Budget")
				.UseCollation("latin1_bin");
			entity.Property((ContactsRecherche e) => e.CBudgetC).HasMaxLength(100).HasColumnName("C_BudgetC")
				.UseCollation("latin1_bin");
			entity.Property((ContactsRecherche e) => e.CCodePostal).HasMaxLength(50).HasColumnName("C_CodePostal");
			entity.Property((ContactsRecherche e) => e.CCodeSupp1).HasMaxLength(50).HasColumnName("C_CodeSupp1");
			entity.Property((ContactsRecherche e) => e.CCodeSupp2).HasMaxLength(50).HasColumnName("C_CodeSupp2");
			entity.Property((ContactsRecherche e) => e.CCodeSupp3).HasMaxLength(50).HasColumnName("C_CodeSupp3");
			entity.Property((ContactsRecherche e) => e.CConditionCom).IsRequired().HasMaxLength(200)
				.HasDefaultValueSql("''")
				.HasColumnName("C_Condition_Com");
			entity.Property((ContactsRecherche e) => e.CContratsSignes).HasMaxLength(100).HasColumnName("C_ContratsSignes");
			entity.Property((ContactsRecherche e) => e.CDate).HasColumnName("C_Date");
			entity.Property((ContactsRecherche e) => e.CDateCreation).HasColumnName("C_Date_Creation");
			entity.Property((ContactsRecherche e) => e.CDateFin).HasColumnName("C_DateFin");
			entity.Property((ContactsRecherche e) => e.CDernEtage).IsRequired().HasDefaultValueSql("'2'")
				.HasColumnType("enum('0','1','2')")
				.HasColumnName("C_DernEtage")
				.UseCollation("latin1_bin");
			entity.Property((ContactsRecherche e) => e.CEtage).HasMaxLength(100).HasColumnName("C_Etage")
				.UseCollation("latin1_bin");
			entity.Property((ContactsRecherche e) => e.CEvoPrix).IsRequired().HasDefaultValueSql("'2'")
				.HasColumnType("enum('0','1','2')")
				.HasColumnName("C_EvoPrix")
				.UseCollation("latin1_bin");
			entity.Property((ContactsRecherche e) => e.CExclusivite).IsRequired().HasDefaultValueSql("'2'")
				.HasColumnType("enum('0','1','2')")
				.HasColumnName("C_Exclusivite")
				.UseCollation("latin1_bin");
			entity.Property((ContactsRecherche e) => e.CFactureHon).HasColumnName("C_FactureHon");
			entity.Property((ContactsRecherche e) => e.CFacturePs).HasColumnName("C_FacturePS");
			entity.Property((ContactsRecherche e) => e.CFraisPercus).HasColumnName("C_FraisPercus");
			entity.Property((ContactsRecherche e) => e.CIdTypeMission).HasColumnName("C_Id_TypeMission");
			entity.Property((ContactsRecherche e) => e.CKeyWord1).IsRequired().HasMaxLength(255)
				.HasDefaultValueSql("''")
				.HasColumnName("C_KeyWord1");
			entity.Property((ContactsRecherche e) => e.CKeyWord2).IsRequired().HasMaxLength(255)
				.HasDefaultValueSql("''")
				.HasColumnName("C_KeyWord2");
			entity.Property((ContactsRecherche e) => e.CKeyWord3).IsRequired().HasMaxLength(255)
				.HasDefaultValueSql("''")
				.HasColumnName("C_KeyWord3");
			entity.Property((ContactsRecherche e) => e.CLibelleClient).HasMaxLength(200).HasColumnName("C_LibelleClient");
			entity.Property((ContactsRecherche e) => e.CLocalisation).IsRequired().HasColumnType("text")
				.HasColumnName("C_Localisation")
				.UseCollation("utf8mb3_bin")
				.HasCharSet("utf8mb3");
			entity.Property((ContactsRecherche e) => e.CMandat).IsRequired().HasMaxLength(75)
				.HasDefaultValueSql("''")
				.HasColumnName("C_Mandat");
			entity.Property((ContactsRecherche e) => e.CMel1).HasMaxLength(50).HasColumnName("C_Mel1");
			entity.Property((ContactsRecherche e) => e.CMel2).HasMaxLength(50).HasColumnName("C_Mel2");
			entity.Property((ContactsRecherche e) => e.CMttHono).HasPrecision(10, 2).HasColumnName("C_MttHono");
			entity.Property((ContactsRecherche e) => e.CNbChambres).HasMaxLength(120).HasColumnName("C_NbChambres")
				.UseCollation("latin1_bin");
			entity.Property((ContactsRecherche e) => e.CNbPieces).HasMaxLength(100).HasColumnName("C_NbPieces")
				.UseCollation("latin1_bin");
			entity.Property((ContactsRecherche e) => e.CNegociateur).IsRequired().HasMaxLength(50)
				.HasDefaultValueSql("''")
				.HasColumnName("C_Negociateur");
			entity.Property((ContactsRecherche e) => e.CNomFamille).HasMaxLength(50).HasColumnName("C_NomFamille");
			entity.Property((ContactsRecherche e) => e.CNomFamilleConseiller).IsRequired().HasMaxLength(50)
				.HasDefaultValueSql("''")
				.HasColumnName("C_NomFamilleConseiller");
			entity.Property((ContactsRecherche e) => e.CNumeroMandat).HasColumnName("C_NumeroMandat");
			entity.Property((ContactsRecherche e) => e.COrigine).IsRequired().HasMaxLength(20)
				.HasColumnName("C_Origine");
			entity.Property((ContactsRecherche e) => e.CPaysRegion).HasMaxLength(50).HasDefaultValueSql("'France'")
				.HasColumnName("C_PaysRegion");
			entity.Property((ContactsRecherche e) => e.CPourcentageApp).IsRequired().HasMaxLength(3)
				.HasDefaultValueSql("'100'")
				.HasColumnName("C_PourcentageApp");
			entity.Property((ContactsRecherche e) => e.CPourcentageCons).IsRequired().HasMaxLength(3)
				.HasDefaultValueSql("'100'")
				.HasColumnName("C_PourcentageCons");
			entity.Property((ContactsRecherche e) => e.CPourcentageNeg).IsRequired().HasMaxLength(3)
				.HasDefaultValueSql("'100'")
				.HasColumnName("C_PourcentageNeg");
			entity.Property((ContactsRecherche e) => e.CPrenom).HasMaxLength(50).HasColumnName("C_Prenom");
			entity.Property((ContactsRecherche e) => e.CQrecherche).IsRequired().HasMaxLength(75)
				.HasDefaultValueSql("''")
				.HasColumnName("C_QRecherche");
			entity.Property((ContactsRecherche e) => e.CRecherche).HasMaxLength(400).HasColumnName("C_Recherche");
			entity.Property((ContactsRecherche e) => e.CRechercheCom).IsRequired().HasMaxLength(2000)
				.HasColumnName("C_Recherche_Com");
			entity.Property((ContactsRecherche e) => e.CRefFormulaire).HasColumnName("C_RefFormulaire");
			entity.Property((ContactsRecherche e) => e.CRemarques).HasMaxLength(200).HasColumnName("C_Remarques");
			entity.Property((ContactsRecherche e) => e.CRemise).HasColumnName("C_Remise");
			entity.Property((ContactsRecherche e) => e.CStatut).HasMaxLength(50).HasDefaultValueSql("'PROSPECT ACTIF'")
				.HasColumnName("C_Statut");
			entity.Property((ContactsRecherche e) => e.CSurface).HasMaxLength(100).HasColumnName("C_Surface")
				.UseCollation("latin1_bin");
			entity.Property((ContactsRecherche e) => e.CTags).IsRequired().HasColumnType("text")
				.HasColumnName("C_Tags")
				.UseCollation("latin1_bin");
			entity.Property((ContactsRecherche e) => e.CTelMobile1).HasMaxLength(30).HasColumnName("C_TelMobile1");
			entity.Property((ContactsRecherche e) => e.CTelMobile2).HasMaxLength(50).HasColumnName("C_TelMobile2");
			entity.Property((ContactsRecherche e) => e.CTelPersonnel1).HasMaxLength(30).HasColumnName("C_TelPersonnel1");
			entity.Property((ContactsRecherche e) => e.CTelPersonnel2).HasMaxLength(50).HasColumnName("C_TelPersonnel2");
			entity.Property((ContactsRecherche e) => e.CTelProfessionnel1).HasMaxLength(30).HasColumnName("C_TelProfessionnel1");
			entity.Property((ContactsRecherche e) => e.CTelProfessionnel2).HasMaxLength(50).HasColumnName("C_TelProfessionnel2");
			entity.Property((ContactsRecherche e) => e.CTitre).HasMaxLength(50).HasColumnName("C_Titre");
			entity.Property((ContactsRecherche e) => e.CTypeBien).HasColumnType("set('Appartement','Maison','Loft','Péniche','Hôtel part.','Locaux Pro')").HasColumnName("C_TypeBien");
			entity.Property((ContactsRecherche e) => e.CTypeRecherche).IsRequired().HasDefaultValueSql("'0'")
				.HasComment("0 : Active // 1 : En pause // 2 : Rech Terminee")
				.HasColumnType("enum('0','1','2')")
				.HasColumnName("C_TypeRecherche");
			entity.Property((ContactsRecherche e) => e.CTypeTransaction).HasColumnType("enum('A','L')").HasColumnName("C_TypeTransaction")
				.UseCollation("latin1_bin");
			entity.Property((ContactsRecherche e) => e.CVille).HasMaxLength(50).HasColumnName("C_Ville");
			entity.HasOne((ContactsRecherche d) => d.CRefFormulaireNavigation).WithMany((Formulaires p) => p.ContactsRecherche).HasForeignKey((ContactsRecherche d) => d.CRefFormulaire)
				.OnDelete(DeleteBehavior.SetNull)
				.HasConstraintName("FK_Formulaire");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Dictionnaire> entity)
		{
			entity.HasKey((Dictionnaire e) => e.DRef).HasName("PRIMARY");
			entity.ToTable("dictionnaire").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.HasIndex((Dictionnaire e) => e.DLabel, "D_Label").IsUnique();
			entity.HasIndex((Dictionnaire e) => e.DCorrespondances, "index_name").HasAnnotation("MySql:FullTextIndex", true);
			entity.Property((Dictionnaire e) => e.DRef).HasColumnName("D_Ref");
			entity.Property((Dictionnaire e) => e.DCorrespondances).IsRequired().HasMaxLength(75)
				.HasComment("Ajouter des mots ici pour ajouter des correspondances")
				.HasColumnName("D_Correspondances");
			entity.Property((Dictionnaire e) => e.DDomId).IsRequired().HasMaxLength(20)
				.HasColumnName("D_DOM_Id");
			entity.Property((Dictionnaire e) => e.DDomName).IsRequired().HasMaxLength(20)
				.HasColumnName("D_DOM_Name");
			entity.Property((Dictionnaire e) => e.DLabel).IsRequired().HasMaxLength(30)
				.HasColumnName("D_Label");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Erreur> entity)
		{
			entity.HasKey((Erreur e) => e.ECle).HasName("PRIMARY");
			entity.ToTable("erreur").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.Property((Erreur e) => e.ECle).HasColumnName("E_cle");
			entity.Property((Erreur e) => e.EDatetime).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnType("timestamp")
				.HasColumnName("E_Datetime");
			entity.Property((Erreur e) => e.EDetail).IsRequired().HasMaxLength(90)
				.HasColumnName("E_Detail");
			entity.Property((Erreur e) => e.EFrom).HasMaxLength(30).HasColumnName("E_From");
			entity.Property((Erreur e) => e.EImportance).IsRequired().HasMaxLength(30)
				.HasColumnName("E_Importance");
			entity.Property((Erreur e) => e.EOcurrences).HasDefaultValueSql("'1'").HasColumnName("E_Ocurrences");
			entity.Property((Erreur e) => e.EType).IsRequired().HasMaxLength(11)
				.HasColumnName("E_Type");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Evenements> entity)
		{
			entity.HasKey((Evenements e) => e.ERefEvenement).HasName("PRIMARY");
			entity.ToTable("evenements").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.HasIndex((Evenements e) => e.ERefAnnAgc, "E_RefAnnAGC");
			entity.HasIndex((Evenements e) => e.ERefBien, "E_RefBien");
			entity.HasIndex((Evenements e) => e.ERefConseiller, "E_RefConseiller");
			entity.HasIndex((Evenements e) => new { e.ERefContact, e.EStatut, e.EDate }, "E_RefContact");
			entity.HasIndex((Evenements e) => e.ERefCtcInter, "E_RefCtcInter");
			entity.HasIndex((Evenements e) => e.ERefInterD, "E_RefInterD");
			entity.HasIndex((Evenements e) => e.ERefInterI, "E_RefInterI");
			entity.HasIndex((Evenements e) => e.ETypeEvenement, "E_TypeEvenement");
			entity.Property((Evenements e) => e.ERefEvenement).HasColumnName("E_RefEvenement");
			entity.Property((Evenements e) => e.ECr).IsRequired().HasMaxLength(255)
				.HasColumnName("E_CR");
			entity.Property((Evenements e) => e.EDate).HasColumnType("datetime").HasColumnName("E_Date");
			entity.Property((Evenements e) => e.EMail).HasMaxLength(255).HasComment("N'est utilisé que par les types 'MAIL OUT/IN' (ridicule car la colonne E_CR aurait très bien joué le rôle)")
				.HasColumnName("E_Mail");
			entity.Property((Evenements e) => e.ENomContact).IsRequired().HasMaxLength(30)
				.HasColumnName("E_NomContact");
			entity.Property((Evenements e) => e.EPropertyId).HasMaxLength(40).HasColumnName("E_PropertyId");
			entity.Property((Evenements e) => e.ERefAnnAgc).HasColumnName("E_RefAnnAGC");
			entity.Property((Evenements e) => e.ERefBien).HasColumnName("E_RefBien");
			entity.Property((Evenements e) => e.ERefConseiller).HasColumnName("E_RefConseiller");
			entity.Property((Evenements e) => e.ERefContact).HasColumnName("E_RefContact");
			entity.Property((Evenements e) => e.ERefCtcInter).HasColumnName("E_RefCtcInter");
			entity.Property((Evenements e) => e.ERefInterD).HasColumnName("E_RefInterD");
			entity.Property((Evenements e) => e.ERefInterI).HasColumnName("E_RefInterI");
			entity.Property((Evenements e) => e.EStatut).HasDefaultValueSql("'1'").HasColumnName("E_Statut");
			entity.Property((Evenements e) => e.ETexte).IsRequired().HasMaxLength(100)
				.HasColumnName("E_Texte");
			entity.Property((Evenements e) => e.ETypeEvenement).HasMaxLength(50).HasColumnName("E_TypeEvenement")
				.UseCollation("latin1_swedish_ci")
				.HasCharSet("latin1");
			entity.HasOne((Evenements d) => d.ERefBienNavigation).WithMany((Biens p) => p.Evenements).HasForeignKey((Evenements d) => d.ERefBien)
				.OnDelete(DeleteBehavior.SetNull)
				.HasConstraintName("evenements_ibfk_6");
			entity.HasOne((Evenements d) => d.ERefConseillerNavigation).WithMany((ConseillersPersonnels p) => p.Evenements).HasForeignKey((Evenements d) => d.ERefConseiller)
				.OnDelete(DeleteBehavior.SetNull)
				.HasConstraintName("evenements_ibfk_4");
			entity.HasOne((Evenements d) => d.ERefContactNavigation).WithMany((ContactsRecherche p) => p.Evenements).HasForeignKey((Evenements d) => d.ERefContact)
				.OnDelete(DeleteBehavior.SetNull)
				.HasConstraintName("evenements_ibfk_7");
			entity.HasOne((Evenements d) => d.ERefCtcInterNavigation).WithMany((ContactIntermediaire p) => p.Evenements).HasForeignKey((Evenements d) => d.ERefCtcInter)
				.OnDelete(DeleteBehavior.SetNull)
				.HasConstraintName("evenements_ibfk_3");
			entity.HasOne((Evenements d) => d.ERefInterDNavigation).WithMany((IntermediairesDirects p) => p.Evenements).HasForeignKey((Evenements d) => d.ERefInterD)
				.OnDelete(DeleteBehavior.SetNull)
				.HasConstraintName("evenements_ibfk_2");
			entity.HasOne((Evenements d) => d.ERefInterINavigation).WithMany((IntermediairesIndirects p) => p.Evenements).HasForeignKey((Evenements d) => d.ERefInterI)
				.OnDelete(DeleteBehavior.SetNull)
				.HasConstraintName("evenements_ibfk_1");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Formulaires> entity)
		{
			entity.HasKey((Formulaires e) => e.FRef).HasName("PRIMARY");
			entity.ToTable("formulaires").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.HasIndex((Formulaires e) => e.FDate, "F_Date");
			entity.HasIndex((Formulaires e) => e.FRefNegociateur, "F_RefNegociateur");
			entity.HasIndex((Formulaires e) => e.FStatut, "F_Statut");
			entity.Property((Formulaires e) => e.FRef).HasColumnName("F_Ref");
			entity.Property((Formulaires e) => e.FAdresse).HasMaxLength(100).HasColumnName("F_Adresse");
			entity.Property((Formulaires e) => e.FCp).HasMaxLength(100).HasColumnName("F_CP");
			entity.Property((Formulaires e) => e.FDate).HasColumnType("datetime").HasColumnName("F_Date");
			entity.Property((Formulaires e) => e.FDateRdv).HasMaxLength(50).HasColumnName("F_DateRdv");
			entity.Property((Formulaires e) => e.FEmail).HasMaxLength(100).HasColumnName("F_Email");
			entity.Property((Formulaires e) => e.FFichierContenu).HasMaxLength(255).HasColumnName("F_FichierContenu");
			entity.Property((Formulaires e) => e.FIpAddress).HasMaxLength(30).HasColumnName("F_IpAddress");
			entity.Property((Formulaires e) => e.FNom).HasMaxLength(100).HasColumnName("F_Nom");
			entity.Property((Formulaires e) => e.FPrenom).HasMaxLength(100).HasColumnName("F_Prenom");
			entity.Property((Formulaires e) => e.FRefNegociateur).HasColumnName("F_RefNegociateur");
			entity.Property((Formulaires e) => e.FSociete).HasMaxLength(100).HasColumnName("F_Societe");
			entity.Property((Formulaires e) => e.FSouhaits).HasMaxLength(200).HasColumnName("F_Souhaits");
			entity.Property((Formulaires e) => e.FStatut).HasDefaultValueSql("'1'").HasColumnName("F_Statut");
			entity.Property((Formulaires e) => e.FTel).HasMaxLength(100).HasColumnName("F_Tel");
			entity.Property((Formulaires e) => e.FTexte).HasColumnType("text").HasColumnName("F_Texte");
			entity.Property((Formulaires e) => e.FType).HasColumnName("F_Type");
			entity.Property((Formulaires e) => e.FUtmCampaign).HasMaxLength(50).HasColumnName("F_UtmCampaign");
			entity.Property((Formulaires e) => e.FUtmContent).HasMaxLength(100).HasColumnName("F_UtmContent");
			entity.Property((Formulaires e) => e.FUtmMedium).HasMaxLength(50).HasColumnName("F_UtmMedium");
			entity.Property((Formulaires e) => e.FUtmSource).HasMaxLength(50).HasColumnName("F_UtmSource");
			entity.Property((Formulaires e) => e.FUtmTerm).HasMaxLength(100).HasColumnName("F_UtmTerm");
			entity.Property((Formulaires e) => e.FVariant).HasMaxLength(5).HasColumnName("F_Variant");
			entity.Property((Formulaires e) => e.FVille).HasMaxLength(100).HasColumnName("F_Ville");
			entity.HasOne((Formulaires d) => d.FRefNegociateurNavigation).WithMany((ConseillersPersonnels p) => p.Formulaires).HasForeignKey((Formulaires d) => d.FRefNegociateur)
				.OnDelete(DeleteBehavior.SetNull)
				.HasConstraintName("formulaires_ibfk_1");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<HistoriqueAnnonces> entity)
		{
			entity.HasKey((HistoriqueAnnonces e) => e.HRef).HasName("PRIMARY");
			entity.ToTable("historique_annonces").HasCharSet("latin1").UseCollation("latin1_german1_ci");
			entity.HasIndex((HistoriqueAnnonces e) => e.HRefAnnonce, "H_RefAnnonce");
			entity.HasIndex((HistoriqueAnnonces e) => new { e.HRefAnnonce, e.HDate, e.HTypeVariation }, "H_RefAnnonce_2").IsUnique();
			entity.Property((HistoriqueAnnonces e) => e.HRef).HasColumnName("H_Ref");
			entity.Property((HistoriqueAnnonces e) => e.HAncienneValeur).HasColumnName("H_AncienneValeur");
			entity.Property((HistoriqueAnnonces e) => e.HDate).HasColumnName("H_Date");
			entity.Property((HistoriqueAnnonces e) => e.HNouvelleValeur).HasColumnName("H_NouvelleValeur");
			entity.Property((HistoriqueAnnonces e) => e.HRefAnnonce).HasColumnName("H_RefAnnonce");
			entity.Property((HistoriqueAnnonces e) => e.HTypeVariation).IsRequired().HasMaxLength(10)
				.HasColumnName("H_TypeVariation");
			entity.HasOne((HistoriqueAnnonces d) => d.HRefAnnonceNavigation).WithMany((AnnoncesGlobales p) => p.HistoriqueAnnonces).HasForeignKey((HistoriqueAnnonces d) => d.HRefAnnonce)
				.HasConstraintName("historique_annonces_ibfk_1");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<HistoriqueAspiration> entity)
		{
			entity.HasNoKey().ToTable("historique_aspiration").HasCharSet("utf8mb3")
				.UseCollation("utf8mb3_general_ci");
			entity.Property((HistoriqueAspiration e) => e.HaBiensIn).HasColumnName("HA_BiensIn");
			entity.Property((HistoriqueAspiration e) => e.HaBiensOut).HasColumnName("HA_BiensOut");
			entity.Property((HistoriqueAspiration e) => e.HaBiensStock).HasColumnName("HA_BiensStock");
			entity.Property((HistoriqueAspiration e) => e.HaCp).HasColumnName("HA_CP");
			entity.Property((HistoriqueAspiration e) => e.HaDate).HasColumnName("HA_Date");
			entity.Property((HistoriqueAspiration e) => e.HaEtat).IsRequired().HasMaxLength(15)
				.HasColumnName("HA_Etat");
			entity.Property((HistoriqueAspiration e) => e.HaFluxInDuJour).HasColumnName("HA_FluxInDuJour");
			entity.Property((HistoriqueAspiration e) => e.HaFluxOutDuJour).HasColumnName("HA_FluxOutDuJour");
			entity.Property((HistoriqueAspiration e) => e.HaMoteur).IsRequired().HasMaxLength(20)
				.HasColumnName("HA_Moteur");
			entity.Property((HistoriqueAspiration e) => e.HaMoyenneFluxIn).HasColumnName("HA_MoyenneFluxIn");
			entity.Property((HistoriqueAspiration e) => e.HaMoyenneFluxOut).HasColumnName("HA_MoyenneFluxOut");
			entity.Property((HistoriqueAspiration e) => e.HaMoyenneStock).HasColumnName("HA_MoyenneStock");
			entity.Property((HistoriqueAspiration e) => e.HaNbAnnoncesStock).HasColumnName("HA_NbAnnoncesStock");
			entity.Property((HistoriqueAspiration e) => e.HaUpdateAnnoncesGlobales).HasColumnType("datetime").HasColumnName("HA_UpdateAnnoncesGlobales");
			entity.Property((HistoriqueAspiration e) => e.HaUpdateAspiration).HasColumnType("datetime").HasColumnName("HA_UpdateAspiration");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<HistoriqueInterDirect> entity)
		{
			entity.HasKey((HistoriqueInterDirect e) => e.HidId).HasName("PRIMARY");
			entity.ToTable("historique_inter_direct").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.Property((HistoriqueInterDirect e) => e.HidId).HasColumnName("HID_Id");
			entity.Property((HistoriqueInterDirect e) => e.HidMois).IsRequired().HasMaxLength(10)
				.HasColumnName("HID_Mois");
			entity.Property((HistoriqueInterDirect e) => e.HidNbAchat).HasColumnName("HID_NbAchat");
			entity.Property((HistoriqueInterDirect e) => e.HidNbAppart).HasColumnName("HID_NbAppart");
			entity.Property((HistoriqueInterDirect e) => e.HidNbBiensTotal).HasColumnName("HID_NbBiensTotal");
			entity.Property((HistoriqueInterDirect e) => e.HidNbLoc).HasColumnName("HID_NbLoc");
			entity.Property((HistoriqueInterDirect e) => e.HidNbLocaux).HasColumnName("HID_NbLocaux");
			entity.Property((HistoriqueInterDirect e) => e.HidNbMaison).HasColumnName("HID_NbMaison");
			entity.Property((HistoriqueInterDirect e) => e.HidRef).IsRequired().HasMaxLength(14)
				.HasColumnName("HID_Ref");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<IntermediairesDirects> entity)
		{
			entity.HasKey((IntermediairesDirects e) => e.IRefIntermediaire).HasName("PRIMARY");
			entity.ToTable("intermediaires_directs").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.HasIndex((IntermediairesDirects e) => e.IRefGroupe, "FK_intermediaires_directs_intermediaires_indirects");
			entity.HasIndex((IntermediairesDirects e) => e.IActif, "I_Actif");
			entity.HasIndex((IntermediairesDirects e) => e.INomIntermediaire, "I_NomIntermediaire");
			entity.HasIndex((IntermediairesDirects e) => e.ITelephone, "I_Telephone");
			entity.HasIndex((IntermediairesDirects e) => e.ITelephone2, "I_Telephone2");
			entity.Property((IntermediairesDirects e) => e.IRefIntermediaire).HasColumnName("I_RefIntermediaire");
			entity.Property((IntermediairesDirects e) => e.IActif).HasColumnName("I_Actif");
			entity.Property((IntermediairesDirects e) => e.IAdhIntercabinet).IsRequired().HasMaxLength(255)
				.HasDefaultValueSql("'Non'")
				.HasColumnName("I_AdhIntercabinet");
			entity.Property((IntermediairesDirects e) => e.IAdresse1).HasMaxLength(50).HasColumnName("I_Adresse1");
			entity.Property((IntermediairesDirects e) => e.IBoulogne).HasDefaultValueSql("'0'").HasColumnName("I_Boulogne");
			entity.Property((IntermediairesDirects e) => e.ICategorie).HasMaxLength(50).HasColumnName("I_Categorie");
			entity.Property((IntermediairesDirects e) => e.ICharenton).HasDefaultValueSql("'0'").HasColumnName("I_Charenton");
			entity.Property((IntermediairesDirects e) => e.ICodePostal).HasDefaultValueSql("'0'").HasColumnName("I_CodePostal");
			entity.Property((IntermediairesDirects e) => e.ICommentaires).HasMaxLength(255).HasColumnName("I_Commentaires");
			entity.Property((IntermediairesDirects e) => e.ICourbevoie).HasDefaultValueSql("'0'").HasColumnName("I_Courbevoie");
			entity.Property((IntermediairesDirects e) => e.IDiffusionSitePropre).HasColumnType("mediumtext").HasColumnName("I_DiffusionSitePropre");
			entity.Property((IntermediairesDirects e) => e.IGroupeEventuel).HasMaxLength(50).HasColumnName("I_GroupeEventuel");
			entity.Property((IntermediairesDirects e) => e.IIdYanport).HasColumnName("I_IdYanport");
			entity.Property((IntermediairesDirects e) => e.IIntercabinet).IsRequired().HasMaxLength(255)
				.HasDefaultValueSql("'Non'")
				.HasColumnName("I_Intercabinet");
			entity.Property((IntermediairesDirects e) => e.IIssyLesMoulineaux).HasDefaultValueSql("'0'").HasColumnName("I_IssyLesMoulineaux");
			entity.Property((IntermediairesDirects e) => e.ILevallois).HasDefaultValueSql("'0'").HasColumnName("I_Levallois");
			entity.Property((IntermediairesDirects e) => e.ILogo).HasMaxLength(300).HasColumnName("I_Logo");
			entity.Property((IntermediairesDirects e) => e.IMel).HasMaxLength(50).HasColumnName("I_Mel");
			entity.Property((IntermediairesDirects e) => e.INeuilly).HasDefaultValueSql("'0'").HasColumnName("I_Neuilly");
			entity.Property((IntermediairesDirects e) => e.INomIntermediaire).IsRequired().HasMaxLength(50)
				.HasDefaultValueSql("''")
				.HasColumnName("I_NomIntermediaire");
			entity.Property((IntermediairesDirects e) => e.IParis1).HasDefaultValueSql("'0'").HasColumnName("I_Paris1");
			entity.Property((IntermediairesDirects e) => e.IParis10).HasDefaultValueSql("'0'").HasColumnName("I_Paris10");
			entity.Property((IntermediairesDirects e) => e.IParis11).HasDefaultValueSql("'0'").HasColumnName("I_Paris11");
			entity.Property((IntermediairesDirects e) => e.IParis12).HasDefaultValueSql("'0'").HasColumnName("I_Paris12");
			entity.Property((IntermediairesDirects e) => e.IParis13).HasDefaultValueSql("'0'").HasColumnName("I_Paris13");
			entity.Property((IntermediairesDirects e) => e.IParis14).HasDefaultValueSql("'0'").HasColumnName("I_Paris14");
			entity.Property((IntermediairesDirects e) => e.IParis15).HasDefaultValueSql("'0'").HasColumnName("I_Paris15");
			entity.Property((IntermediairesDirects e) => e.IParis16).HasDefaultValueSql("'0'").HasColumnName("I_Paris16");
			entity.Property((IntermediairesDirects e) => e.IParis17).HasDefaultValueSql("'0'").HasColumnName("I_Paris17");
			entity.Property((IntermediairesDirects e) => e.IParis18).HasDefaultValueSql("'0'").HasColumnName("I_Paris18");
			entity.Property((IntermediairesDirects e) => e.IParis19).HasDefaultValueSql("'0'").HasColumnName("I_Paris19");
			entity.Property((IntermediairesDirects e) => e.IParis2).HasDefaultValueSql("'0'").HasColumnName("I_Paris2");
			entity.Property((IntermediairesDirects e) => e.IParis20).HasDefaultValueSql("'0'").HasColumnName("I_Paris20");
			entity.Property((IntermediairesDirects e) => e.IParis3).HasDefaultValueSql("'0'").HasColumnName("I_Paris3");
			entity.Property((IntermediairesDirects e) => e.IParis4).HasDefaultValueSql("'0'").HasColumnName("I_Paris4");
			entity.Property((IntermediairesDirects e) => e.IParis5).HasDefaultValueSql("'0'").HasColumnName("I_Paris5");
			entity.Property((IntermediairesDirects e) => e.IParis6).HasDefaultValueSql("'0'").HasColumnName("I_Paris6");
			entity.Property((IntermediairesDirects e) => e.IParis7).HasDefaultValueSql("'0'").HasColumnName("I_Paris7");
			entity.Property((IntermediairesDirects e) => e.IParis8).HasDefaultValueSql("'0'").HasColumnName("I_Paris8");
			entity.Property((IntermediairesDirects e) => e.IParis9).HasDefaultValueSql("'0'").HasColumnName("I_Paris9");
			entity.Property((IntermediairesDirects e) => e.IPuteaux).HasDefaultValueSql("'0'").HasColumnName("I_Puteaux");
			entity.Property((IntermediairesDirects e) => e.IQualiteRelation).HasMaxLength(50).HasDefaultValueSql("'Neutre ou inconnu'")
				.HasColumnName("I_QualiteRelation");
			entity.Property((IntermediairesDirects e) => e.IRefGroupe).HasColumnName("I_RefGroupe");
			entity.Property((IntermediairesDirects e) => e.ISaintMande).HasDefaultValueSql("'0'").HasColumnName("I_SaintMande");
			entity.Property((IntermediairesDirects e) => e.ISirenYanport).HasMaxLength(9).HasColumnName("I_SirenYanport");
			entity.Property((IntermediairesDirects e) => e.IStanding).IsRequired().HasMaxLength(200)
				.HasColumnName("I_Standing");
			entity.Property((IntermediairesDirects e) => e.ITelephone).HasMaxLength(15).HasColumnName("I_Telephone");
			entity.Property((IntermediairesDirects e) => e.ITelephone2).HasMaxLength(15).HasColumnName("I_Telephone2");
			entity.Property((IntermediairesDirects e) => e.ITypeBien).IsRequired().HasMaxLength(200)
				.HasColumnName("I_Type_bien");
			entity.Property((IntermediairesDirects e) => e.IVanves).HasDefaultValueSql("'0'").HasColumnName("I_Vanves");
			entity.Property((IntermediairesDirects e) => e.IVille).HasMaxLength(50).HasColumnName("I_Ville");
			entity.Property((IntermediairesDirects e) => e.IVincennes).HasDefaultValueSql("'0'").HasColumnName("I_Vincennes");
			entity.Property((IntermediairesDirects e) => e.IVolumetrieAnnonces).HasColumnName("I_VolumetrieAnnonces");
			entity.HasOne((IntermediairesDirects d) => d.IRefGroupeNavigation).WithMany((IntermediairesIndirects p) => p.IntermediairesDirects).HasForeignKey((IntermediairesDirects d) => d.IRefGroupe)
				.HasConstraintName("FK_intermediaires_directs_intermediaires_indirects");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<IntermediairesDirectsYanport> entity)
		{
			entity.HasKey((IntermediairesDirectsYanport e) => new { e.IRefIntermediaire, e.IYanportId }).HasName("PRIMARY").HasAnnotation("MySql:IndexPrefixLength", new int[2]);
			entity.ToTable("intermediaires_directs_yanport").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.Property((IntermediairesDirectsYanport e) => e.IRefIntermediaire).HasColumnName("I_RefIntermediaire");
			entity.Property((IntermediairesDirectsYanport e) => e.IYanportId).HasColumnName("I_YanportId");
			entity.Property((IntermediairesDirectsYanport e) => e.ISource).HasMaxLength(5).HasColumnName("I_Source");
			entity.HasOne((IntermediairesDirectsYanport d) => d.IRefIntermediaireNavigation).WithMany((IntermediairesDirects p) => p.IntermediairesDirectsYanport).HasForeignKey((IntermediairesDirectsYanport d) => d.IRefIntermediaire)
				.HasConstraintName("FK__intermediaires_directs_yanport");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<IntermediairesIndirects> entity)
		{
			entity.HasKey((IntermediairesIndirects e) => e.I2RefIntermIndirect).HasName("PRIMARY");
			entity.ToTable("intermediaires_indirects").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.HasIndex((IntermediairesIndirects e) => e.I2NomIntermIndirect, "I2_NomInterm_indirect");
			entity.HasIndex((IntermediairesIndirects e) => e.I2Standing, "I2_Standing");
			entity.HasIndex((IntermediairesIndirects e) => e.I2TypeBien, "I2_Type_bien");
			entity.HasIndex((IntermediairesIndirects e) => e.I2VolumetrieAnnonces, "I2_Volumétrie_annonces");
			entity.Property((IntermediairesIndirects e) => e.I2RefIntermIndirect).HasColumnName("I2_RefInterm_indirect");
			entity.Property((IntermediairesIndirects e) => e.I2Actif).HasColumnName("I2_Actif");
			entity.Property((IntermediairesIndirects e) => e.I2Boulogne).HasDefaultValueSql("'0'").HasColumnName("I2_Boulogne");
			entity.Property((IntermediairesIndirects e) => e.I2Categorie).HasMaxLength(50).HasColumnName("I2_Categorie")
				.UseCollation("utf8mb3_general_ci")
				.HasCharSet("utf8mb3");
			entity.Property((IntermediairesIndirects e) => e.I2Charenton).HasDefaultValueSql("'0'").HasColumnName("I2_Charenton");
			entity.Property((IntermediairesIndirects e) => e.I2Commentaires).HasMaxLength(255).HasColumnName("I2_Commentaires");
			entity.Property((IntermediairesIndirects e) => e.I2Courbevoie).HasDefaultValueSql("'0'").HasColumnName("I2_Courbevoie");
			entity.Property((IntermediairesIndirects e) => e.I2Icon).IsRequired().HasMaxLength(255)
				.HasColumnName("I2_Icon");
			entity.Property((IntermediairesIndirects e) => e.I2IssyLesMoulineaux).HasDefaultValueSql("'0'").HasColumnName("I2_IssyLesMoulineaux");
			entity.Property((IntermediairesIndirects e) => e.I2Levallois).HasDefaultValueSql("'0'").HasColumnName("I2_Levallois");
			entity.Property((IntermediairesIndirects e) => e.I2NbDagencesSurZone).HasDefaultValueSql("'0'").HasColumnName("I2_Nb_dagences_sur_zone");
			entity.Property((IntermediairesIndirects e) => e.I2NbTotalDagences).HasDefaultValueSql("'0'").HasColumnName("I2_Nb_total_dagences");
			entity.Property((IntermediairesIndirects e) => e.I2Neuilly).HasDefaultValueSql("'0'").HasColumnName("I2_Neuilly");
			entity.Property((IntermediairesIndirects e) => e.I2NomIntermIndirect).IsRequired().HasMaxLength(50)
				.HasDefaultValueSql("''")
				.HasColumnName("I2_NomInterm_indirect");
			entity.Property((IntermediairesIndirects e) => e.I2Paris1).HasDefaultValueSql("'0'").HasColumnName("I2_Paris1");
			entity.Property((IntermediairesIndirects e) => e.I2Paris10).HasDefaultValueSql("'0'").HasColumnName("I2_Paris10");
			entity.Property((IntermediairesIndirects e) => e.I2Paris11).HasDefaultValueSql("'0'").HasColumnName("I2_Paris11");
			entity.Property((IntermediairesIndirects e) => e.I2Paris12).HasDefaultValueSql("'0'").HasColumnName("I2_Paris12");
			entity.Property((IntermediairesIndirects e) => e.I2Paris13).HasDefaultValueSql("'0'").HasColumnName("I2_Paris13");
			entity.Property((IntermediairesIndirects e) => e.I2Paris14).HasDefaultValueSql("'0'").HasColumnName("I2_Paris14");
			entity.Property((IntermediairesIndirects e) => e.I2Paris15).HasDefaultValueSql("'0'").HasColumnName("I2_Paris15");
			entity.Property((IntermediairesIndirects e) => e.I2Paris16).HasDefaultValueSql("'0'").HasColumnName("I2_Paris16");
			entity.Property((IntermediairesIndirects e) => e.I2Paris17).HasDefaultValueSql("'0'").HasColumnName("I2_Paris17");
			entity.Property((IntermediairesIndirects e) => e.I2Paris18).HasDefaultValueSql("'0'").HasColumnName("I2_Paris18");
			entity.Property((IntermediairesIndirects e) => e.I2Paris19).HasDefaultValueSql("'0'").HasColumnName("I2_Paris19");
			entity.Property((IntermediairesIndirects e) => e.I2Paris2).HasDefaultValueSql("'0'").HasColumnName("I2_Paris2");
			entity.Property((IntermediairesIndirects e) => e.I2Paris20).HasDefaultValueSql("'0'").HasColumnName("I2_Paris20");
			entity.Property((IntermediairesIndirects e) => e.I2Paris3).HasDefaultValueSql("'0'").HasColumnName("I2_Paris3");
			entity.Property((IntermediairesIndirects e) => e.I2Paris4).HasDefaultValueSql("'0'").HasColumnName("I2_Paris4");
			entity.Property((IntermediairesIndirects e) => e.I2Paris5).HasDefaultValueSql("'0'").HasColumnName("I2_Paris5");
			entity.Property((IntermediairesIndirects e) => e.I2Paris6).HasDefaultValueSql("'0'").HasColumnName("I2_Paris6");
			entity.Property((IntermediairesIndirects e) => e.I2Paris7).HasDefaultValueSql("'0'").HasColumnName("I2_Paris7");
			entity.Property((IntermediairesIndirects e) => e.I2Paris8).HasDefaultValueSql("'0'").HasColumnName("I2_Paris8");
			entity.Property((IntermediairesIndirects e) => e.I2Paris9).HasDefaultValueSql("'0'").HasColumnName("I2_Paris9");
			entity.Property((IntermediairesIndirects e) => e.I2Puteaux).HasDefaultValueSql("'0'").HasColumnName("I2_Puteaux");
			entity.Property((IntermediairesIndirects e) => e.I2SaintMande).HasDefaultValueSql("'0'").HasColumnName("I2_SaintMande");
			entity.Property((IntermediairesIndirects e) => e.I2Standing).IsRequired().HasMaxLength(50)
				.HasColumnName("I2_Standing");
			entity.Property((IntermediairesIndirects e) => e.I2SystemeAlertes).HasDefaultValueSql("'0'").HasColumnName("I2_Systeme_alertes");
			entity.Property((IntermediairesIndirects e) => e.I2TypeBien).IsRequired().HasMaxLength(20)
				.HasColumnName("I2_Type_bien");
			entity.Property((IntermediairesIndirects e) => e.I2UrlSite).HasColumnType("text").HasColumnName("I2_URL_site");
			entity.Property((IntermediairesIndirects e) => e.I2Vanves).HasDefaultValueSql("'0'").HasColumnName("I2_Vanves");
			entity.Property((IntermediairesIndirects e) => e.I2Vincennes).HasDefaultValueSql("'0'").HasColumnName("I2_Vincennes");
			entity.Property((IntermediairesIndirects e) => e.I2VolumetrieAnnonces).HasDefaultValueSql("'0'").HasColumnName("I2_Volumetrie_annonces");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Ips> entity)
		{
			entity.HasKey((Ips e) => e.IIp).HasName("PRIMARY");
			entity.ToTable("ips").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.Property((Ips e) => e.IIp).HasColumnName("i_ip").UseCollation("utf8mb3_general_ci")
				.HasCharSet("utf8mb3");
			entity.Property((Ips e) => e.IStart).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnType("timestamp")
				.HasColumnName("i_start");
			entity.Property((Ips e) => e.IStop).HasDefaultValueSql("'0000-00-00 00:00:00'").HasColumnType("timestamp")
				.HasColumnName("i_stop");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<IpsHost> entity)
		{
			entity.HasKey((IpsHost e) => new { e.IhIp, e.IhHost }).HasName("PRIMARY").HasAnnotation("MySql:IndexPrefixLength", new int[2]);
			entity.ToTable("ips_host").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.Property((IpsHost e) => e.IhIp).HasColumnName("ih_ip").UseCollation("utf8mb3_general_ci")
				.HasCharSet("utf8mb3");
			entity.Property((IpsHost e) => e.IhHost).HasColumnName("ih_host").UseCollation("utf8mb3_general_ci")
				.HasCharSet("utf8mb3");
			entity.Property((IpsHost e) => e.IhDenied).HasColumnName("ih_denied");
			entity.HasOne((IpsHost d) => d.IhIpNavigation).WithMany((Ips p) => p.IpsHost).HasForeignKey((IpsHost d) => d.IhIp)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("ips_host_ibfk_1");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Mails> entity)
		{
			entity.HasKey((Mails e) => e.Id).HasName("PRIMARY");
			entity.ToTable("mails").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.Property((Mails e) => e.Cc).HasMaxLength(500);
			entity.Property((Mails e) => e.Cci).HasMaxLength(500);
			entity.Property((Mails e) => e.Contact).HasMaxLength(50);
			entity.Property((Mails e) => e.Contents).HasMaxLength(10000);
			entity.Property((Mails e) => e.Createdon).HasColumnType("datetime");
			entity.Property((Mails e) => e.Error).HasMaxLength(1000);
			entity.Property((Mails e) => e.Modele).HasMaxLength(100);
			entity.Property((Mails e) => e.Name).IsRequired().HasMaxLength(100);
			entity.Property((Mails e) => e.Pjmanuel1).HasMaxLength(200);
			entity.Property((Mails e) => e.Pjmanuel2).HasMaxLength(200);
			entity.Property((Mails e) => e.Pjmanuel3).HasMaxLength(200);
			entity.Property((Mails e) => e.Senton).HasColumnType("datetime");
			entity.Property((Mails e) => e.Subject).IsRequired().HasMaxLength(200);
			entity.Property((Mails e) => e.Template3).HasColumnName("Template_3");
			entity.Property((Mails e) => e.Template31).HasColumnName("Template3");
			entity.Property((Mails e) => e.To).HasMaxLength(500);
			entity.Property((Mails e) => e.Updatedon).HasColumnType("datetime");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<MetaMoteurs> entity)
		{
			entity.HasKey((MetaMoteurs e) => e.MInterindirect).HasName("PRIMARY");
			entity.ToTable("meta_moteurs").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.HasIndex((MetaMoteurs e) => e.MStatut, "M_Statut");
			entity.Property((MetaMoteurs e) => e.MInterindirect).ValueGeneratedNever().HasColumnName("M_interindirect");
			entity.Property((MetaMoteurs e) => e.Etape).HasColumnName("etape");
			entity.Property((MetaMoteurs e) => e.MNom).IsRequired().HasMaxLength(20)
				.HasColumnName("M_Nom");
			entity.Property((MetaMoteurs e) => e.MOrdre).HasColumnName("M_Ordre");
			entity.Property((MetaMoteurs e) => e.MProxyActif).HasColumnName("M_ProxyActif");
			entity.Property((MetaMoteurs e) => e.MStatut).HasColumnName("M_Statut");
			entity.Property((MetaMoteurs e) => e.MUrlCpOrInsee).HasDefaultValueSql("'1'").HasColumnName("M_URL_CP_OR_INSEE");
			entity.Property((MetaMoteurs e) => e.MUrlSearch).IsRequired().HasColumnType("text")
				.HasColumnName("M_URL_Search");
			entity.Property((MetaMoteurs e) => e.MUrlTagBien).IsRequired().HasMaxLength(255)
				.HasColumnName("M_URL_TAG_Bien");
			entity.Property((MetaMoteurs e) => e.MUrlTagLocalisation).IsRequired().HasMaxLength(255)
				.HasColumnName("M_URL_TAG_Localisation");
			entity.Property((MetaMoteurs e) => e.MUrlTagPrixMax).IsRequired().HasMaxLength(255)
				.HasColumnName("M_URL_TAG_PrixMax");
			entity.Property((MetaMoteurs e) => e.MUrlTagPrixMin).IsRequired().HasMaxLength(255)
				.HasColumnName("M_URL_TAG_PrixMin");
			entity.Property((MetaMoteurs e) => e.MUrlTagSurfaceMax).IsRequired().HasMaxLength(255)
				.HasColumnName("M_URL_TAG_SurfaceMax");
			entity.Property((MetaMoteurs e) => e.MUrlTagSurfaceMin).IsRequired().HasMaxLength(255)
				.HasColumnName("M_URL_TAG_SurfaceMin");
			entity.Property((MetaMoteurs e) => e.MUrlTagTransaction).IsRequired().HasMaxLength(255)
				.HasColumnName("M_URL_TAG_Transaction");
			entity.Property((MetaMoteurs e) => e.MUrlTypeBienAppart).IsRequired().HasMaxLength(255)
				.HasColumnName("M_URL_TYPE_BIEN_Appart");
			entity.Property((MetaMoteurs e) => e.MUrlTypeBienLocal).IsRequired().HasMaxLength(255)
				.HasColumnName("M_URL_TYPE_BIEN_Local");
			entity.Property((MetaMoteurs e) => e.MUrlTypeBienMaison).IsRequired().HasMaxLength(255)
				.HasColumnName("M_URL_TYPE_BIEN_Maison");
			entity.Property((MetaMoteurs e) => e.MUrlTypeTransacAchat).IsRequired().HasMaxLength(255)
				.HasColumnName("M_URL_TYPE_TRANSAC_Achat");
			entity.Property((MetaMoteurs e) => e.MUrlTypeTransacLoc).IsRequired().HasMaxLength(255)
				.HasColumnName("M_URL_TYPE_TRANSAC_Loc");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<ModelesCommerciaux> entity)
		{
			entity.HasKey((ModelesCommerciaux e) => e.McRef).HasName("PRIMARY");
			entity.ToTable("modeles_commerciaux").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.HasIndex((ModelesCommerciaux e) => e.McCategorie, "MC_categorie");
			entity.Property((ModelesCommerciaux e) => e.McRef).HasColumnName("MC_Ref");
			entity.Property((ModelesCommerciaux e) => e.McCategorie).HasColumnName("MC_Categorie");
			entity.Property((ModelesCommerciaux e) => e.McFichierDoc).HasColumnName("MC_Fichier_doc");
			entity.Property((ModelesCommerciaux e) => e.McFichierDocMaj).HasColumnName("MC_Fichier_doc_maj");
			entity.Property((ModelesCommerciaux e) => e.McFichierPdf).HasColumnName("MC_Fichier_pdf");
			entity.Property((ModelesCommerciaux e) => e.McFichierPdfMaj).HasColumnName("MC_Fichier_pdf_maj");
			entity.Property((ModelesCommerciaux e) => e.McTitre).IsRequired().HasMaxLength(255)
				.HasColumnName("MC_Titre");
			entity.Property((ModelesCommerciaux e) => e.McTitreAbr).IsRequired().HasMaxLength(100)
				.HasColumnName("MC_Titre_abr");
			entity.HasOne((ModelesCommerciaux d) => d.McCategorieNavigation).WithMany((TypesModeles p) => p.ModelesCommerciaux).HasForeignKey((ModelesCommerciaux d) => d.McCategorie)
				.OnDelete(DeleteBehavior.SetNull)
				.HasConstraintName("modeles_commerciaux_ibfk_1");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<MotsCles> entity)
		{
			entity.HasKey((MotsCles e) => e.Id).HasName("PRIMARY");
			entity.ToTable("mots_cles").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.HasIndex((MotsCles e) => e.Nom, "Uniq_nom").IsUnique();
			entity.Property((MotsCles e) => e.Id).HasColumnName("id");
			entity.Property((MotsCles e) => e.Nom).IsRequired().HasColumnName("nom");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<NiveauxDiffusion> entity)
		{
			entity.HasKey((NiveauxDiffusion e) => e.N).HasName("PRIMARY");
			entity.ToTable("niveaux_diffusion").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.Property((NiveauxDiffusion e) => e.N).HasColumnName("N°");
			entity.Property((NiveauxDiffusion e) => e.NiveauDeDiffusion).HasMaxLength(50).HasColumnName("Niveau de diffusion");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<NiveauxHabilitation> entity)
		{
			entity.HasKey((NiveauxHabilitation e) => e.NhRefNiveauDHabilitation).HasName("PRIMARY");
			entity.ToTable("niveaux_habilitation").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.Property((NiveauxHabilitation e) => e.NhRefNiveauDHabilitation).HasColumnName("NH-RefNiveau d''habilitation");
			entity.Property((NiveauxHabilitation e) => e.NhNiveauDHabilitation).HasMaxLength(50).HasColumnName("NH-Niveau d''habilitation");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Notifications> entity)
		{
			entity.HasKey((Notifications e) => e.NId).HasName("PRIMARY");
			entity.ToTable("notifications").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.Property((Notifications e) => e.NId).HasColumnName("N_Id");
			entity.Property((Notifications e) => e.NCreatedOn).HasColumnType("datetime").HasColumnName("N_CreatedOn");
			entity.Property((Notifications e) => e.NNewPrix).HasMaxLength(12).HasColumnName("N_NewPrix");
			entity.Property((Notifications e) => e.NOldPrix).HasMaxLength(12).HasColumnName("N_OldPrix");
			entity.Property((Notifications e) => e.NPropertyId).HasMaxLength(40).HasColumnName("N_PropertyId");
			entity.Property((Notifications e) => e.NRefAnn).HasColumnName("N_RefAnn");
			entity.Property((Notifications e) => e.NRefConseiller).HasColumnName("N_RefConseiller");
			entity.Property((Notifications e) => e.NRefContact).HasColumnName("N_RefContact");
			entity.Property((Notifications e) => e.NState).HasColumnName("N_State");
			entity.Property((Notifications e) => e.NType).IsRequired().HasMaxLength(30)
				.HasColumnName("N_Type");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Organisation> entity)
		{
			entity.HasNoKey().ToTable("organisation").HasCharSet("latin1")
				.UseCollation("latin1_swedish_ci");
			entity.HasIndex((Organisation e) => new { e.ORefContact, e.OIntermediaire }, "i_inter_contact");
			entity.Property((Organisation e) => e.OAlerteDate).HasColumnName("O_Alerte_date");
			entity.Property((Organisation e) => e.ODateContactTel).HasColumnName("O_DateContactTel");
			entity.Property((Organisation e) => e.ODateVisite).HasColumnName("O_DateVisite");
			entity.Property((Organisation e) => e.OIntermediaire).IsRequired().HasMaxLength(50)
				.HasColumnName("O_Intermediaire");
			entity.Property((Organisation e) => e.ORefContact).IsRequired().HasMaxLength(5)
				.HasColumnName("O_RefContact");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Origines> entity)
		{
			entity.HasKey((Origines e) => e.ONom).HasName("PRIMARY");
			entity.ToTable("origines").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.Property((Origines e) => e.ONom).HasMaxLength(50).HasColumnName("O_Nom");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Pap> entity)
		{
			entity.HasNoKey().ToTable("pap").HasCharSet("latin1")
				.UseCollation("latin1_swedish_ci");
			entity.HasIndex((Pap e) => e.ADesc, "A_Desc").IsUnique();
			entity.HasIndex((Pap e) => e.ARefAnn, "A_RefAnn");
			entity.Property((Pap e) => e.AActif).HasDefaultValueSql("'1'").HasColumnName("A_Actif");
			entity.Property((Pap e) => e.ADam).HasColumnName("A_Dam");
			entity.Property((Pap e) => e.ADateAff).HasColumnName("A_Date_Aff");
			entity.Property((Pap e) => e.ADateAspi).HasColumnName("A_Date_Aspi");
			entity.Property((Pap e) => e.ADesc).HasMaxLength(600).HasColumnName("A_Desc");
			entity.Property((Pap e) => e.AImage).HasMaxLength(100).HasColumnName("A_Image");
			entity.Property((Pap e) => e.ALien).IsRequired().HasMaxLength(255)
				.HasColumnName("A_Lien");
			entity.Property((Pap e) => e.ANbPieces).HasDefaultValueSql("'0'").HasColumnName("A_NbPieces");
			entity.Property((Pap e) => e.APrix).HasMaxLength(12).HasColumnName("A_Prix");
			entity.Property((Pap e) => e.ARefAnn).ValueGeneratedOnAdd().HasColumnName("A_RefAnn");
			entity.Property((Pap e) => e.ASource).IsRequired().HasMaxLength(50)
				.HasColumnName("A_Source");
			entity.Property((Pap e) => e.ASurf).HasColumnName("A_Surf");
			entity.Property((Pap e) => e.ATelAnnonceur).IsRequired().HasMaxLength(14)
				.HasColumnName("A_TelAnnonceur");
			entity.Property((Pap e) => e.AType).HasMaxLength(50).HasColumnName("A_Type");
			entity.Property((Pap e) => e.ATypeannonce).HasMaxLength(50).HasColumnName("A_Typeannonce");
			entity.Property((Pap e) => e.AVille).HasMaxLength(50).HasColumnName("A_Ville");
			entity.Property((Pap e) => e.Lalalalla).HasMaxLength(50).HasColumnName("lalalalla");
			entity.Property((Pap e) => e.Lololo).HasMaxLength(50).HasColumnName("lololo");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<PhotosAnnonces> entity)
		{
			entity.HasKey((PhotosAnnonces e) => e.PaRef).HasName("PRIMARY");
			entity.ToTable("photos_annonces").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.Property((PhotosAnnonces e) => e.PaRef).HasColumnName("PA_Ref");
			entity.Property((PhotosAnnonces e) => e.PaIdPropertyYanport).IsRequired().HasMaxLength(40)
				.HasColumnName("PA_IdPropertyYanport");
			entity.Property((PhotosAnnonces e) => e.PaUrl).IsRequired().HasMaxLength(300)
				.HasColumnName("PA_Url");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Property> entity)
		{
			entity.HasKey((Property e) => e.PPropertyId).HasName("PRIMARY");
			entity.ToTable("property").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.Property((Property e) => e.PPropertyId).HasMaxLength(40).HasColumnName("P_PropertyId");
			entity.Property((Property e) => e.PAdresseLat).HasColumnName("P_AdresseLat");
			entity.Property((Property e) => e.PAdresseLon).HasColumnName("P_AdresseLon");
			entity.Property((Property e) => e.PAdresseNum).HasMaxLength(10).HasColumnName("P_AdresseNum");
			entity.Property((Property e) => e.PAdresseRue).HasMaxLength(100).HasColumnName("P_AdresseRue");
			entity.Property((Property e) => e.PAds).HasMaxLength(2000).HasColumnName("P_Ads");
			entity.Property((Property e) => e.PAnnee).HasColumnName("P_Annee");
			entity.Property((Property e) => e.PAnnonceurs).HasMaxLength(1000).HasColumnName("P_Annonceurs");
			entity.Property((Property e) => e.PAsc).HasColumnName("P_Asc");
			entity.Property((Property e) => e.PConsumptionLetter).HasMaxLength(1).HasColumnName("P_ConsumptionLetter");
			entity.Property((Property e) => e.PCp).HasMaxLength(10).HasColumnName("P_CP");
			entity.Property((Property e) => e.PDateCreation).HasColumnType("datetime").HasColumnName("P_DateCreation");
			entity.Property((Property e) => e.PDateDebut).HasColumnType("datetime").HasColumnName("P_DateDebut");
			entity.Property((Property e) => e.PDateFin).HasColumnType("datetime").HasColumnName("P_DateFin");
			entity.Property((Property e) => e.PDateLastUpdate).HasColumnType("datetime").HasColumnName("P_DateLastUpdate");
			entity.Property((Property e) => e.PDescription).HasMaxLength(3000).HasColumnName("P_Description");
			entity.Property((Property e) => e.PDoublonPropertyId).HasMaxLength(40).HasColumnName("P_DoublonPropertyId");
			entity.Property((Property e) => e.PEstDernierEtage).HasColumnName("P_EstDernierEtage");
			entity.Property((Property e) => e.PEstExclusif).HasColumnName("P_EstExclusif");
			entity.Property((Property e) => e.PEstOccupe).HasColumnName("P_EstOccupe");
			entity.Property((Property e) => e.PEstRecent).HasColumnName("P_EstRecent");
			entity.Property((Property e) => e.PEtage).HasColumnName("P_Etage");
			entity.Property((Property e) => e.PGreenhouseGasConsumptionLetter).HasMaxLength(1).HasColumnName("P_GreenhouseGasConsumptionLetter");
			entity.Property((Property e) => e.PIdQuartier).HasColumnName("P_IdQuartier");
			entity.Property((Property e) => e.PIdVille).HasColumnName("P_IdVille");
			entity.Property((Property e) => e.PImages).HasMaxLength(5000).HasColumnName("P_Images");
			entity.Property((Property e) => e.PListeTags).HasMaxLength(100).HasColumnName("P_ListeTags");
			entity.Property((Property e) => e.PNbChambres).HasColumnName("P_NbChambres");
			entity.Property((Property e) => e.PNbEtages).HasColumnName("P_NbEtages");
			entity.Property((Property e) => e.PNbPieces).HasColumnName("P_NbPieces");
			entity.Property((Property e) => e.PPrix).HasColumnType("double unsigned").HasColumnName("P_Prix");
			entity.Property((Property e) => e.PPrixEvol).HasColumnName("P_PrixEvol");
			entity.Property((Property e) => e.PPrixHistorique).HasMaxLength(1000).HasColumnName("P_PrixHistorique");
			entity.Property((Property e) => e.PQuartier).HasMaxLength(100).HasColumnName("P_Quartier");
			entity.Property((Property e) => e.PQuartier2).HasMaxLength(100).HasColumnName("P_Quartier2");
			entity.Property((Property e) => e.PState).HasColumnName("P_State");
			entity.Property((Property e) => e.PSurface).HasColumnType("double unsigned").HasColumnName("P_Surface");
			entity.Property((Property e) => e.PType).HasMaxLength(50).HasColumnName("P_Type");
			entity.Property((Property e) => e.PTypeTransaction).HasMaxLength(1).HasColumnName("P_TypeTransaction");
			entity.HasMany((Property d) => d.CRefCtcInter).WithMany((ContactIntermediaire p) => p.PProperty).UsingEntity("RelPropertyContactInter", (EntityTypeBuilder<Dictionary<string, object>> r) => r.HasOne<ContactIntermediaire>().WithMany().HasForeignKey("CRefCtcInter")
				.HasConstraintName("FK__contact_intermediaire"), (EntityTypeBuilder<Dictionary<string, object>> l) => l.HasOne<Property>().WithMany().HasForeignKey("PPropertyId")
				.HasConstraintName("FK__property2"), delegate(EntityTypeBuilder<Dictionary<string, object>> j)
			{
				j.HasKey("PPropertyId", "CRefCtcInter").HasName("PRIMARY").HasAnnotation("MySql:IndexPrefixLength", new int[2]);
				j.ToTable("rel_property_contact_inter").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
				j.HasIndex(new string[1] { "CRefCtcInter" }, "FK__contact_intermediaire");
				j.IndexerProperty<string>("PPropertyId").HasMaxLength(40).HasColumnName("P_PropertyId");
				j.IndexerProperty<uint>("CRefCtcInter").HasColumnName("C_RefCtcInter");
			});
			entity.HasMany((Property d) => d.IRefIntermediaire).WithMany((IntermediairesDirects p) => p.PProperty).UsingEntity("RelPropertyInterDirect", (EntityTypeBuilder<Dictionary<string, object>> r) => r.HasOne<IntermediairesDirects>().WithMany().HasForeignKey("IRefIntermediaire")
				.HasConstraintName("FK__intermediaires_directs"), (EntityTypeBuilder<Dictionary<string, object>> l) => l.HasOne<Property>().WithMany().HasForeignKey("PPropertyId")
				.HasConstraintName("FK__property"), delegate(EntityTypeBuilder<Dictionary<string, object>> j)
			{
				j.HasKey("PPropertyId", "IRefIntermediaire").HasName("PRIMARY").HasAnnotation("MySql:IndexPrefixLength", new int[2]);
				j.ToTable("rel_property_inter_direct").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
				j.HasIndex(new string[1] { "IRefIntermediaire" }, "FK__intermediaires_directs");
				j.IndexerProperty<string>("PPropertyId").HasMaxLength(40).HasColumnName("P_PropertyId");
				j.IndexerProperty<uint>("IRefIntermediaire").HasColumnName("I_RefIntermediaire");
			});
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<PropertyContact> entity)
		{
			entity.HasKey((PropertyContact e) => e.PcId).HasName("PRIMARY");
			entity.ToTable("property_contact").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.HasIndex((PropertyContact e) => e.PcRefContact, "FK_property_contact_contacts_recherche");
			entity.HasIndex((PropertyContact e) => e.PcPropertyId, "FK_property_contact_property");
			entity.Property((PropertyContact e) => e.PcId).HasColumnName("PC_Id");
			entity.Property((PropertyContact e) => e.PcActif).HasColumnName("PC_Actif");
			entity.Property((PropertyContact e) => e.PcCom).HasMaxLength(200).HasColumnName("PC_Com");
			entity.Property((PropertyContact e) => e.PcDateAff).HasDefaultValueSql("curtime()").HasColumnType("datetime")
				.HasColumnName("PC_DateAff");
			entity.Property((PropertyContact e) => e.PcPropertyId).IsRequired().HasMaxLength(40)
				.HasColumnName("PC_PropertyId");
			entity.Property((PropertyContact e) => e.PcRate).HasColumnName("PC_Rate");
			entity.Property((PropertyContact e) => e.PcRefContact).HasColumnName("PC_RefContact");
			entity.Property((PropertyContact e) => e.PcVu).HasColumnName("PC_Vu");
			entity.HasOne((PropertyContact d) => d.PcProperty).WithMany((Property p) => p.PropertyContact).HasForeignKey((PropertyContact d) => d.PcPropertyId)
				.HasConstraintName("FK_property_contact_property");
			entity.HasOne((PropertyContact d) => d.PcRefContactNavigation).WithMany((ContactsRecherche p) => p.PropertyContact).HasForeignKey((PropertyContact d) => d.PcRefContact)
				.HasConstraintName("FK_property_contact_contacts_recherche");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Proxy> entity)
		{
			entity.HasKey((Proxy e) => e.Id).HasName("PRIMARY");
			entity.ToTable("proxy").HasCharSet("latin1").UseCollation("latin1_german1_ci");
			entity.Property((Proxy e) => e.Id).HasColumnName("id");
			entity.Property((Proxy e) => e.Age).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnType("timestamp")
				.HasColumnName("age");
			entity.Property((Proxy e) => e.CrawlingTime).HasDefaultValueSql("'3'").HasColumnName("crawling_time");
			entity.Property((Proxy e) => e.Echecs).HasColumnName("echecs");
			entity.Property((Proxy e) => e.Parserror).HasColumnName("parserror");
			entity.Property((Proxy e) => e.Port).HasColumnType("mediumint").HasColumnName("port");
			entity.Property((Proxy e) => e.Reussites).HasColumnName("reussites");
			entity.Property((Proxy e) => e.Tentatives).HasColumnName("tentatives");
			entity.Property((Proxy e) => e.Url).IsRequired().HasMaxLength(255)
				.HasColumnName("url");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<ProxyAnonyme> entity)
		{
			entity.HasNoKey().ToTable("proxy_anonyme").HasCharSet("utf8mb3")
				.UseCollation("utf8mb3_general_ci");
			entity.Property((ProxyAnonyme e) => e.Age).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnType("timestamp")
				.HasColumnName("age");
			entity.Property((ProxyAnonyme e) => e.CrawlingTime).HasColumnName("crawling_time");
			entity.Property((ProxyAnonyme e) => e.Echecs).HasColumnName("echecs");
			entity.Property((ProxyAnonyme e) => e.Parserror).HasColumnName("parserror");
			entity.Property((ProxyAnonyme e) => e.Port).HasColumnType("mediumint").HasColumnName("port");
			entity.Property((ProxyAnonyme e) => e.Reussites).HasColumnName("reussites");
			entity.Property((ProxyAnonyme e) => e.Tentatives).HasColumnName("tentatives");
			entity.Property((ProxyAnonyme e) => e.Url).IsRequired().HasMaxLength(255)
				.HasColumnName("url");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<QualiteRelationIntermediaire> entity)
		{
			entity.HasKey((QualiteRelationIntermediaire e) => e.QRefQualite).HasName("PRIMARY");
			entity.ToTable("qualite_relation_intermediaire").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.Property((QualiteRelationIntermediaire e) => e.QRefQualite).HasColumnName("Q_RefQualite");
			entity.Property((QualiteRelationIntermediaire e) => e.QNiveaudeQualite).HasMaxLength(50).HasColumnName("Q_NiveaudeQualite");
			entity.Property((QualiteRelationIntermediaire e) => e.QSigne).IsRequired().HasColumnType("text")
				.HasColumnName("Q_signe");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Quartiers> entity)
		{
			entity.HasKey((Quartiers e) => e.QRef).HasName("PRIMARY");
			entity.ToTable("quartiers").HasCharSet("utf8mb3").UseCollation("utf8mb3_bin");
			entity.HasIndex((Quartiers e) => e.QCp, "Q_CP");
			entity.Property((Quartiers e) => e.QRef).HasColumnName("Q_Ref");
			entity.Property((Quartiers e) => e.QActif).IsRequired().HasDefaultValueSql("'1'")
				.HasColumnName("Q_Actif");
			entity.Property((Quartiers e) => e.QCp).HasColumnType("mediumint unsigned").HasColumnName("Q_CP");
			entity.Property((Quartiers e) => e.QNomQuartier).IsRequired().HasMaxLength(100)
				.HasColumnName("Q_NomQuartier");
			entity.Property((Quartiers e) => e.QQuarterIdYanport).HasColumnName("Q_quarterIdYanport");
			entity.Property((Quartiers e) => e.QTag).IsRequired().HasMaxLength(100)
				.HasComment("c'est ce champs qui sera utilisé pour la recherche")
				.HasColumnName("Q_Tag")
				.UseCollation("utf8mb3_general_ci");
			entity.HasOne((Quartiers d) => d.QCpNavigation).WithMany((CodesPostaux p) => p.Quartiers).HasForeignKey((Quartiers d) => d.QCp)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("quartiers_ibfk_1");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<RechAutomatique> entity)
		{
			entity.HasKey((RechAutomatique e) => e.Id).HasName("PRIMARY");
			entity.ToTable("rech_automatique").HasCharSet("latin1").UseCollation("latin1_german1_ci");
			entity.Property((RechAutomatique e) => e.Id).HasColumnName("id");
			entity.Property((RechAutomatique e) => e.DateLancement).HasColumnName("date_lancement");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<RechercheGlobalConfig> entity)
		{
			entity.HasKey((RechercheGlobalConfig e) => e.RgcCategorie).HasName("PRIMARY");
			entity.ToTable("recherche_global_config").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.Property((RechercheGlobalConfig e) => e.RgcCategorie).HasMaxLength(30).HasColumnName("RGC_Categorie");
			entity.Property((RechercheGlobalConfig e) => e.RgcCouleur).HasMaxLength(10).HasColumnName("RGC_Couleur");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<ScheduledTaskLogs> entity)
		{
			entity.HasKey((ScheduledTaskLogs e) => e.StlId).HasName("PRIMARY");
			entity.ToTable("scheduled_task_logs");
			entity.Property((ScheduledTaskLogs e) => e.StlId).HasColumnName("STL_Id");
			entity.Property((ScheduledTaskLogs e) => e.StlDateDebut).HasColumnType("datetime").HasColumnName("STL_DateDebut");
			entity.Property((ScheduledTaskLogs e) => e.StlDateFin).HasColumnType("datetime").HasColumnName("STL_DateFin");
			entity.Property((ScheduledTaskLogs e) => e.StlNom).IsRequired().HasMaxLength(50)
				.HasColumnName("STL_Nom");
			entity.Property((ScheduledTaskLogs e) => e.StlResults).HasMaxLength(1000).HasColumnName("STL_Results");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<SqlLogs> entity)
		{
			entity.HasKey((SqlLogs e) => e.Logid).HasName("PRIMARY");
			entity.ToTable("sql_logs").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.Property((SqlLogs e) => e.Logid).HasColumnName("logid");
			entity.Property((SqlLogs e) => e.Contactref).HasColumnName("contactref");
			entity.Property((SqlLogs e) => e.Duration).HasColumnName("duration");
			entity.Property((SqlLogs e) => e.Sql).HasMaxLength(5000).HasColumnName("sql");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<StandingBien> entity)
		{
			entity.HasKey((StandingBien e) => e.N).HasName("PRIMARY");
			entity.ToTable("standing_bien").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.Property((StandingBien e) => e.N).HasColumnName("N°");
			entity.Property((StandingBien e) => e.Champ2).HasMaxLength(50);
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<StatistiquesGenerales> entity)
		{
			entity.HasNoKey().ToTable("statistiques_generales").HasCharSet("utf8mb3")
				.UseCollation("utf8mb3_general_ci");
			entity.Property((StatistiquesGenerales e) => e.SgBiensIn).HasColumnName("SG_BiensIn");
			entity.Property((StatistiquesGenerales e) => e.SgBiensOut).HasColumnName("SG_BiensOut");
			entity.Property((StatistiquesGenerales e) => e.SgBiensStock).HasColumnName("SG_BiensStock");
			entity.Property((StatistiquesGenerales e) => e.SgDate).HasColumnName("SG_Date");
			entity.Property((StatistiquesGenerales e) => e.SgFluxIn).HasColumnName("SG_FluxIn");
			entity.Property((StatistiquesGenerales e) => e.SgFluxOut).HasColumnName("SG_FluxOut");
			entity.Property((StatistiquesGenerales e) => e.SgMoteur).IsRequired().HasMaxLength(30)
				.HasColumnName("SG_Moteur");
			entity.Property((StatistiquesGenerales e) => e.SgStock).HasColumnName("SG_Stock");
			entity.Property((StatistiquesGenerales e) => e.SgTimeUpdate).HasColumnType("datetime").HasColumnName("SG_TimeUpdate");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Stats> entity)
		{
			entity.HasKey((Stats e) => e.SDate).HasName("PRIMARY");
			entity.ToTable("stats", delegate(TableBuilder<Stats> tb)
			{
				tb.HasComment("Statistique de l'entreprise");
			}).HasCharSet("latin1").UseCollation("latin1_german1_ci");
			entity.Property((Stats e) => e.SDate).HasColumnName("S_Date");
			entity.Property((Stats e) => e.SClientsActifs).HasComment("nombre de clients actifs").HasColumnName("S_Clients_Actifs");
			entity.Property((Stats e) => e.SClientsAttenteSig).HasComment("nombre d'attentes de signature").HasColumnName("S_Clients_AttenteSig");
			entity.Property((Stats e) => e.SClientsRechActiv).HasComment("nombre de recherches actives").HasColumnName("S_Clients_RechActiv");
			entity.Property((Stats e) => e.SClientsRechSusp).HasComment("nombre de recherches suspendues").HasColumnName("S_Clients_RechSusp");
			entity.Property((Stats e) => e.SFluxAnnonces).HasComment("Flux de nouvelles annonces du jour").HasColumnName("S_Flux_Annonces");
			entity.Property((Stats e) => e.SFluxClientsActifs).HasComment("Flux de nouveaux clients actifs").HasColumnName("S_Flux_ClientsActifs");
			entity.Property((Stats e) => e.SFluxClientsMorts).HasComment("Flux de nouveaux clients morts").HasColumnName("S_Flux_ClientsMorts");
			entity.Property((Stats e) => e.SFluxEventCrVisite).HasComment("Flux d'evenements CR VISITE du jour").HasColumnName("S_Flux_Event_CR_Visite");
			entity.Property((Stats e) => e.SFluxEventNewCrVisite).HasComment("Flux de nouveaux evenements CR VISITE").HasColumnName("S_Flux_Event_New_CR_Visite");
			entity.Property((Stats e) => e.SFluxEventNewRvClient).HasComment("Flux de nouveaux evenements RV VISITE CLIENT").HasColumnName("S_Flux_Event_New_RV_Client");
			entity.Property((Stats e) => e.SFluxEventNewRvSeul).HasComment("Flux de nouveaux evenements RV VISITE SEUL").HasColumnName("S_Flux_Event_New_RV_Seul");
			entity.Property((Stats e) => e.SFluxEventRvClient).HasComment("Flux d'evenements RV VISITE CLIENT du jour").HasColumnName("S_Flux_Event_RV_Client");
			entity.Property((Stats e) => e.SFluxEventRvSeul).HasComment("Flux d'evenements RV VISITE SEUL du jour").HasColumnName("S_Flux_Event_RV_Seul");
			entity.Property((Stats e) => e.SFluxProspectsActifs).HasComment("Flux de nouveaux prospects actifs").HasColumnName("S_Flux_ProspectsActifs");
			entity.Property((Stats e) => e.SFluxProspectsMorts).HasComment("Flux de nouveaux prospects morts").HasColumnName("S_Flux_ProspectsMorts");
			entity.Property((Stats e) => e.SProspectsActifs).HasComment("Stock de prospects actifs").HasColumnName("S_Prospects_Actifs");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<StatutsContacts> entity)
		{
			entity.HasKey((StatutsContacts e) => e.SRéfStatutClient).HasName("PRIMARY");
			entity.ToTable("statuts_contacts").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.Property((StatutsContacts e) => e.SRéfStatutClient).ValueGeneratedNever().HasColumnName("S-RéfStatut client");
			entity.Property((StatutsContacts e) => e.SStatut).HasMaxLength(50).HasColumnName("S-Statut");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<StratalisExpired> entity)
		{
			entity.HasKey((StratalisExpired e) => e.AnnonceId).HasName("PRIMARY");
			entity.ToTable("stratalis_expired").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.Property((StratalisExpired e) => e.AnnonceId).HasColumnName("annonce_id");
			entity.Property((StratalisExpired e) => e.LastSeenOn).HasColumnType("timestamp").HasColumnName("last_seen_on");
			entity.Property((StratalisExpired e) => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnType("timestamp")
				.HasColumnName("timestamp");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Taches> entity)
		{
			entity.HasKey((Taches e) => e.TRef).HasName("PRIMARY");
			entity.ToTable("taches").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.HasIndex((Taches e) => e.TRefContact, "FK_taches_contacts_recherche");
			entity.HasIndex((Taches e) => e.TQui, "T_Qui");
			entity.Property((Taches e) => e.TRef).HasColumnName("T_Ref");
			entity.Property((Taches e) => e.TCom).IsRequired().HasMaxLength(300)
				.HasColumnName("T_Com");
			entity.Property((Taches e) => e.TDateCreation).HasColumnType("datetime").HasColumnName("T_Date_Creation");
			entity.Property((Taches e) => e.TDateRealisation).HasColumnType("datetime").HasColumnName("T_Date_Realisation");
			entity.Property((Taches e) => e.TEtat).IsRequired().HasMaxLength(20)
				.HasColumnName("T_Etat");
			entity.Property((Taches e) => e.TLien).IsRequired().HasMaxLength(300)
				.HasColumnName("T_Lien");
			entity.Property((Taches e) => e.TPropertyId).HasMaxLength(40).HasColumnName("T_PropertyId");
			entity.Property((Taches e) => e.TQui).IsRequired().HasMaxLength(100)
				.HasColumnName("T_Qui");
			entity.Property((Taches e) => e.TRefAnnonce).HasColumnName("T_Ref_Annonce");
			entity.Property((Taches e) => e.TRefContact).HasColumnName("T_Ref_Contact");
			entity.Property((Taches e) => e.TType).IsRequired().HasMaxLength(20)
				.HasColumnName("T_Type");
			entity.HasOne((Taches d) => d.TRefContactNavigation).WithMany((ContactsRecherche p) => p.Taches).HasForeignKey((Taches d) => d.TRefContact)
				.HasConstraintName("FK_taches_contacts_recherche");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<TagsAnnonce> entity)
		{
			entity.HasKey((TagsAnnonce e) => e.TaRef).HasName("PRIMARY");
			entity.ToTable("tags_annonce").HasCharSet("latin1").UseCollation("latin1_german1_ci");
			entity.Property((TagsAnnonce e) => e.TaRef).HasColumnType("mediumint unsigned").HasColumnName("TA_Ref");
			entity.Property((TagsAnnonce e) => e.TaAlias).IsRequired().HasMaxLength(200)
				.HasColumnName("TA_Alias")
				.UseCollation("utf8mb3_general_ci")
				.HasCharSet("utf8mb3");
			entity.Property((TagsAnnonce e) => e.TaFixe).HasColumnName("TA_Fixe");
			entity.Property((TagsAnnonce e) => e.TaLibelle).IsRequired().HasMaxLength(50)
				.HasColumnName("TA_Libelle")
				.UseCollation("utf8mb3_general_ci")
				.HasCharSet("utf8mb3");
			entity.Property((TagsAnnonce e) => e.TaOrdreTriPourFixe).HasComment("N'est utilise que pour trier pour affichage dans criteres_rech.php").HasColumnName("TA_OrdreTriPourFixe");
			entity.Property((TagsAnnonce e) => e.TaTypeB).IsRequired().HasDefaultValueSql("'Appartement,Maison,Locaux Pro'")
				.HasColumnType("set('Appartement','Maison','Locaux Pro')")
				.HasColumnName("TA_TypeB")
				.UseCollation("latin1_general_ci");
			entity.Property((TagsAnnonce e) => e.TaTypeT).IsRequired().HasDefaultValueSql("'A,L'")
				.HasColumnType("set('A','L')")
				.HasColumnName("TA_TypeT")
				.UseCollation("latin1_general_ci");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Templates> entity)
		{
			entity.HasKey((Templates e) => e.Id).HasName("PRIMARY");
			entity.ToTable("templates").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.Property((Templates e) => e.ContactStatut).IsRequired().HasMaxLength(50);
			entity.Property((Templates e) => e.Createdon).HasColumnType("datetime");
			entity.Property((Templates e) => e.Error).HasMaxLength(1000);
			entity.Property((Templates e) => e.File).IsRequired().HasMaxLength(100);
			entity.Property((Templates e) => e.Name).IsRequired().HasMaxLength(100);
			entity.Property((Templates e) => e.Nompj).HasMaxLength(200);
			entity.Property((Templates e) => e.Testedon).HasColumnType("datetime");
			entity.Property((Templates e) => e.TypeTransaction).IsRequired().HasMaxLength(1);
			entity.Property((Templates e) => e.Updatedon).HasColumnType("datetime");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<TypesBiens> entity)
		{
			entity.HasKey((TypesBiens e) => e.N).HasName("PRIMARY");
			entity.ToTable("types_biens").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.Property((TypesBiens e) => e.N).HasColumnType("mediumint");
			entity.Property((TypesBiens e) => e.Champ1).HasMaxLength(50);
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<TypesCatInterIndirect> entity)
		{
			entity.HasKey((TypesCatInterIndirect e) => e.TciiId).HasName("PRIMARY");
			entity.ToTable("types_cat_inter_indirect").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.Property((TypesCatInterIndirect e) => e.TciiId).HasColumnName("tcii_id");
			entity.Property((TypesCatInterIndirect e) => e.TciiCategorie).IsRequired().HasMaxLength(100)
				.HasColumnName("tcii_categorie");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<TypesEvenements> entity)
		{
			entity.HasKey((TypesEvenements e) => e.TeRefTypeEvenement).HasName("PRIMARY");
			entity.ToTable("types_evenements").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.Property((TypesEvenements e) => e.TeRefTypeEvenement).HasDefaultValueSql("'0'").HasColumnType("mediumint")
				.HasColumnName("TE_RefTypeEvenement");
			entity.Property((TypesEvenements e) => e.TeCategorieEvenement).HasMaxLength(50).HasColumnName("TE_CategorieEvenement");
			entity.Property((TypesEvenements e) => e.TeContactStatut).IsRequired().HasMaxLength(50)
				.HasColumnName("TE_ContactStatut");
			entity.Property((TypesEvenements e) => e.TeGenreEvenement).HasMaxLength(50).HasColumnName("TE_GenreEvenement");
			entity.Property((TypesEvenements e) => e.TeTypeEvenement).HasMaxLength(50).HasColumnName("TE_TypeEvenement");
			entity.Property((TypesEvenements e) => e.TeTypeTransaction).IsRequired().HasMaxLength(1)
				.HasColumnName("TE_TypeTransaction");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<TypesMails> entity)
		{
			entity.HasKey((TypesMails e) => e.TmRefType).HasName("PRIMARY");
			entity.ToTable("types_mails").HasCharSet("latin1").UseCollation("latin1_german1_ci");
			entity.Property((TypesMails e) => e.TmRefType).HasColumnType("mediumint").HasColumnName("TM_RefType");
			entity.Property((TypesMails e) => e.TmContactStatut).IsRequired().HasMaxLength(50)
				.HasColumnName("TM_ContactStatut");
			entity.Property((TypesMails e) => e.TmLibelle).IsRequired().HasMaxLength(50)
				.HasColumnName("TM_Libelle");
			entity.Property((TypesMails e) => e.TmMessage).HasColumnType("text").HasColumnName("TM_Message");
			entity.Property((TypesMails e) => e.TmSujet).HasMaxLength(100).HasColumnName("TM_Sujet");
			entity.Property((TypesMails e) => e.TmTypeTransaction).IsRequired().HasMaxLength(1)
				.HasColumnName("TM_TypeTransaction");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<TypesMissions> entity)
		{
			entity.HasKey((TypesMissions e) => e.TmId).HasName("PRIMARY");
			entity.ToTable("types_missions").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.Property((TypesMissions e) => e.TmId).HasColumnName("TM_Id");
			entity.Property((TypesMissions e) => e.TmNom).IsRequired().HasMaxLength(255)
				.HasColumnName("TM_Nom");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<TypesModeles> entity)
		{
			entity.HasKey((TypesModeles e) => e.TmRef).HasName("PRIMARY");
			entity.ToTable("types_modeles").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.Property((TypesModeles e) => e.TmRef).HasColumnName("TM_Ref");
			entity.Property((TypesModeles e) => e.TmNom).IsRequired().HasMaxLength(100)
				.HasColumnName("TM_Nom");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<TypesSupprform> entity)
		{
			entity.HasKey((TypesSupprform e) => e.TsfRef).HasName("PRIMARY");
			entity.ToTable("types_supprform", delegate(TableBuilder<TypesSupprform> tb)
			{
				tb.HasComment("Utilisé avec la table `formulaires` pour stocker la raison d");
			}).HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.Property((TypesSupprform e) => e.TsfRef).ValueGeneratedNever().HasColumnType("mediumint")
				.HasColumnName("TSF_Ref");
			entity.Property((TypesSupprform e) => e.TsfTexte).IsRequired().HasMaxLength(50)
				.HasColumnName("TSF_Texte");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<TypesTaches> entity)
		{
			entity.HasKey((TypesTaches e) => e.TtType).HasName("PRIMARY");
			entity.ToTable("types_taches").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.Property((TypesTaches e) => e.TtType).HasMaxLength(20).HasColumnName("TT_Type");
			entity.Property((TypesTaches e) => e.TtContactStatut).IsRequired().HasMaxLength(50)
				.HasColumnName("TT_ContactStatut");
			entity.Property((TypesTaches e) => e.TtTypeTransaction).IsRequired().HasMaxLength(1)
				.HasColumnName("TT_TypeTransaction");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<UrlLogs> entity)
		{
			entity.HasKey((UrlLogs e) => e.UlId).HasName("PRIMARY");
			entity.ToTable("url_logs").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.Property((UrlLogs e) => e.UlId).HasColumnName("ul_id");
			entity.Property((UrlLogs e) => e.UlDate).HasDefaultValueSql("'0000-00-00 00:00:00'").HasColumnType("datetime")
				.HasColumnName("ul_date");
			entity.Property((UrlLogs e) => e.UlIsIn).HasColumnName("ul_isIn");
			entity.Property((UrlLogs e) => e.UlIsUpdate).HasColumnName("ul_isUpdate");
			entity.Property((UrlLogs e) => e.UlNb).HasColumnName("ul_nb");
			entity.Property((UrlLogs e) => e.UlUrl).IsRequired().HasMaxLength(250)
				.HasDefaultValueSql("''")
				.HasColumnName("ul_url");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<UrlSearch> entity)
		{
			entity.HasKey((UrlSearch e) => e.UsCle).HasName("PRIMARY");
			entity.ToTable("url_search").HasCharSet("latin1").UseCollation("latin1_swedish_ci");
			entity.Property((UrlSearch e) => e.UsCle).HasColumnName("us_cle");
			entity.Property((UrlSearch e) => e.UsBien).IsRequired().HasMaxLength(255)
				.HasColumnName("us_bien")
				.UseCollation("utf8mb3_general_ci")
				.HasCharSet("utf8mb3");
			entity.Property((UrlSearch e) => e.UsDernierPassage).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnType("timestamp")
				.HasColumnName("us_dernier_passage");
			entity.Property((UrlSearch e) => e.UsEnCours).HasColumnName("us_en_cours");
			entity.Property((UrlSearch e) => e.UsLastCount).HasColumnName("us_last_count");
			entity.Property((UrlSearch e) => e.UsLocalite).IsRequired().HasMaxLength(255)
				.HasColumnName("us_localite")
				.UseCollation("utf8mb3_general_ci")
				.HasCharSet("utf8mb3");
			entity.Property((UrlSearch e) => e.UsMajoration).HasColumnName("us_majoration");
			entity.Property((UrlSearch e) => e.UsMaxCount).HasColumnName("us_max_count");
			entity.Property((UrlSearch e) => e.UsMoteur).HasColumnName("us_moteur");
			entity.Property((UrlSearch e) => e.UsProchainPassage).HasDefaultValueSql("'0000-00-00 00:00:00'").HasColumnType("timestamp")
				.HasColumnName("us_prochain_passage");
			entity.Property((UrlSearch e) => e.UsSuspendu).HasColumnName("us_suspendu");
			entity.Property((UrlSearch e) => e.UsTransaction).IsRequired().HasMaxLength(255)
				.HasColumnName("us_transaction")
				.UseCollation("utf8mb3_general_ci")
				.HasCharSet("utf8mb3");
			entity.Property((UrlSearch e) => e.UsType).HasColumnName("us_type");
			entity.Property((UrlSearch e) => e.UsUrl).IsRequired().HasColumnType("text")
				.HasColumnName("us_url")
				.UseCollation("utf8mb3_general_ci")
				.HasCharSet("utf8mb3");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<UserAgents> entity)
		{
			entity.HasKey((UserAgents e) => e.Id).HasName("PRIMARY");
			entity.ToTable("user_agents").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.Property((UserAgents e) => e.Id).HasColumnName("id");
			entity.Property((UserAgents e) => e.Mobile).HasColumnName("mobile");
			entity.Property((UserAgents e) => e.Popularity).HasColumnName("popularity");
			entity.Property((UserAgents e) => e.Useragents1).IsRequired().HasMaxLength(255)
				.HasColumnName("useragents");
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<YanportInterruptions> entity)
		{
			entity.HasKey((YanportInterruptions e) => e.YiId).HasName("PRIMARY");
			entity.ToTable("yanport_interruptions").HasCharSet("utf8mb3").UseCollation("utf8mb3_general_ci");
			entity.Property((YanportInterruptions e) => e.YiId).HasColumnName("YI_Id");
			entity.Property((YanportInterruptions e) => e.YiDateDebut).HasColumnType("datetime").HasColumnName("YI_DateDebut");
			entity.Property((YanportInterruptions e) => e.YiDateFin).HasColumnType("datetime").HasColumnName("YI_DateFin");
			entity.Property((YanportInterruptions e) => e.YiRaison).IsRequired().HasMaxLength(50)
				.HasColumnName("YI_Raison");
		});
	}
}
