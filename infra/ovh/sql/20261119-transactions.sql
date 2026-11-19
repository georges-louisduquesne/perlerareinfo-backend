-- Création de la table Transactions (transactions immobilières de biens anciens).
-- Source initiale : DVF géolocalisé data.gouv.fr (départements 75, 92, 93, 94 — 5 ans glissants),
-- avec colonnes enrichies NULLables (chambres, étage, surface normée, DPE, lien property, photo)
-- préservées lors des réimports semestriels grâce à la clé unique T_Fingerprint.
-- Création seule : pas de DROP.

CREATE TABLE IF NOT EXISTS Transactions (
  T_Ref bigint(20) unsigned NOT NULL AUTO_INCREMENT,

  -- Socle DVF
  T_DateTransaction date NOT NULL,
  T_NatureMutation varchar(60) NOT NULL DEFAULT 'Vente',
  T_Prix decimal(12,2) unsigned NOT NULL,
  T_AdresseNum varchar(16) DEFAULT NULL,
  T_AdresseSuffixe varchar(10) DEFAULT NULL,
  T_AdresseRue varchar(160) NOT NULL,
  T_AdresseRueNorm varchar(160) NOT NULL,
  T_CP varchar(5) NOT NULL,
  T_Commune varchar(100) DEFAULT NULL,
  T_CodeDepartement varchar(3) NOT NULL,
  T_TypeBien varchar(50) NOT NULL,
  T_NbPieces tinyint(3) unsigned DEFAULT NULL,
  T_SurfaceBatie decimal(8,2) unsigned DEFAULT NULL,
  T_PrixM2 decimal(10,2) unsigned DEFAULT NULL,

  -- Copropriété & annexes DVF
  T_SurfaceCarrez decimal(8,2) unsigned DEFAULT NULL,
  T_NbLots smallint(5) unsigned DEFAULT NULL,
  T_LotsJson varchar(500) DEFAULT NULL,
  T_NbDependances tinyint(3) unsigned NOT NULL DEFAULT 0,
  T_SurfaceTerrain decimal(10,2) unsigned DEFAULT NULL,
  T_NatureCulture varchar(80) DEFAULT NULL,

  -- Cadastre & géolocalisation DVF
  T_IdMutation varchar(40) DEFAULT NULL,
  T_Fingerprint char(64) NOT NULL,
  T_IdParcelle varchar(14) DEFAULT NULL,
  T_CodeCommuneInsee varchar(5) DEFAULT NULL,
  T_CodeVoieFantoir varchar(6) DEFAULT NULL,
  T_Lat double DEFAULT NULL,
  T_Lon double DEFAULT NULL,

  -- Colonnes enrichies (NULLables hors DVF, préservées lors des réimports)
  T_NbChambres tinyint(3) unsigned DEFAULT NULL,
  T_Etage smallint(6) DEFAULT NULL,
  T_SurfaceNormee decimal(8,2) unsigned DEFAULT NULL,
  T_NbEtages tinyint(3) unsigned DEFAULT NULL,
  T_Ascenseur tinyint(1) DEFAULT NULL,
  T_DernierEtage tinyint(1) DEFAULT NULL,
  T_Dpe char(1) DEFAULT NULL,
  T_Remarques text DEFAULT NULL,

  -- Lien CRM / médias & traçabilité
  T_PropertyId varchar(40) DEFAULT NULL,
  T_PhotoUrl varchar(500) DEFAULT NULL,
  T_Source varchar(40) NOT NULL DEFAULT 'DVF',
  T_DateImport datetime NOT NULL,
  T_DateMaj datetime DEFAULT NULL,

  PRIMARY KEY (T_Ref),
  UNIQUE KEY UX_Transactions_Fingerprint (T_Fingerprint),
  KEY IX_Transactions_CP_RueNorm_Num (T_CP, T_AdresseRueNorm, T_AdresseNum, T_DateTransaction),
  KEY IX_Transactions_Parcelle (T_IdParcelle, T_DateTransaction),
  KEY IX_Transactions_Date (T_DateTransaction),
  KEY IX_Transactions_PropertyId (T_PropertyId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
