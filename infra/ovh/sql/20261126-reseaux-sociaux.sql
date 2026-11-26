-- Page Réseaux sociaux : quota hebdomadaire, modèles WhatsApp, publications et sollicitations.
--
-- Contrôle avant :
--   SELECT COLUMN_NAME FROM information_schema.COLUMNS
--    WHERE TABLE_SCHEMA = 'perle-rareinfo'
--      AND TABLE_NAME = 'conseillers_personnels'
--      AND COLUMN_NAME IN ('CP_Quota_Reseaux', 'CP_Solde_Quota');
--   -- attendu : aucune ligne
--
-- Retour arrière : 20261126-reseaux-sociaux-rollback.sql

CREATE TABLE IF NOT EXISTS conseillers_personnels_bak_20261126_quota_reseaux
  AS SELECT CP_RefConseiller, CP_Actif
       FROM conseillers_personnels;

ALTER TABLE conseillers_personnels
  ADD COLUMN CP_Quota_Reseaux INT NOT NULL DEFAULT 0,
  ADD COLUMN CP_Solde_Quota INT NOT NULL DEFAULT 0,
  ALGORITHM=INSTANT;

CREATE TABLE IF NOT EXISTS modeles_reseaux (
  MR_Id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  MR_Actif TINYINT(1) NOT NULL DEFAULT 1,
  MR_Type VARCHAR(20) NOT NULL,
  MR_Titre VARCHAR(120) NOT NULL,
  MR_Contenu TEXT NOT NULL,
  PRIMARY KEY (MR_Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS publications_reseaux (
  PUB_Id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  PUB_PostId VARCHAR(128) NOT NULL,
  PUB_Reseau VARCHAR(16) NOT NULL,
  PUB_Url VARCHAR(500) NOT NULL,
  PUB_DatePublication DATETIME NOT NULL,
  PUB_Vignette TEXT NULL,
  PUB_Texte VARCHAR(500) NULL,
  PUB_Maj DATETIME NOT NULL,
  PRIMARY KEY (PUB_Id),
  UNIQUE KEY uq_publications_reseaux (PUB_Reseau, PUB_PostId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS sollicitations_reseaux (
  SR_Id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  SR_PostId VARCHAR(128) NOT NULL,
  SR_Reseau VARCHAR(16) NOT NULL,
  SR_RefConseiller INT UNSIGNED NOT NULL,
  SR_DatePublication DATETIME NOT NULL,
  SR_DateEnvoi DATETIME NOT NULL,
  SR_Type VARCHAR(20) NOT NULL,
  PRIMARY KEY (SR_Id),
  UNIQUE KEY uq_sollicitations_reseaux (SR_Reseau, SR_PostId, SR_RefConseiller),
  KEY ix_sollicitations_reseaux_conseiller (SR_RefConseiller)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO modeles_reseaux (MR_Actif, MR_Type, MR_Titre, MR_Contenu)
SELECT 1, 'like', 'Like', CONCAT('Bonjour {prenom},', CHAR(10), CHAR(10), 'Pourrais-tu aimer cette publication Perle Rare ?', CHAR(10), '{url}', CHAR(10), CHAR(10), 'Merci !')
WHERE NOT EXISTS (SELECT 1 FROM modeles_reseaux WHERE MR_Type = 'like');

INSERT INTO modeles_reseaux (MR_Actif, MR_Type, MR_Titre, MR_Contenu)
SELECT 1, 'commentaire', 'Commentaire', CONCAT('Bonjour {prenom},', CHAR(10), CHAR(10), 'Pourrais-tu copier-coller un message en commentaire de cette publication ?', CHAR(10), '{url}', CHAR(10), CHAR(10), 'Merci !')
WHERE NOT EXISTS (SELECT 1 FROM modeles_reseaux WHERE MR_Type = 'commentaire');

-- Contrôle après :
--   SHOW COLUMNS FROM conseillers_personnels LIKE 'CP_Quota_Reseaux';
--   SHOW COLUMNS FROM conseillers_personnels LIKE 'CP_Solde_Quota';
--   SELECT MR_Type, MR_Actif FROM modeles_reseaux;
