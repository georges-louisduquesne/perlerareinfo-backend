-- Ajoute conseillers_personnels.CP_Google : lien vers la fiche Google Business, vide pour toutes les lignes.
-- Aucune valeur existante n’est réécrite. Les triggers de la table ne sont pas déclenchés par un ajout de colonne.
-- Jeu de caractères de la table : latin1 / latin1_swedish_ci. Les URL sont en ASCII.
-- VARCHAR(512) : un lien maps.app.goo.gl ou une URL Maps de recherche tient largement.
--
-- Contrôle avant (2026-11-20) :
--   SELECT COLUMN_NAME FROM information_schema.COLUMNS
--    WHERE TABLE_SCHEMA = 'perle-rareinfo'
--      AND TABLE_NAME = 'conseillers_personnels'
--      AND COLUMN_NAME = 'CP_Google';
--   -- attendu : aucune ligne
--   SELECT COUNT(*) AS rows_before FROM conseillers_personnels;
--   -- attendu : 379
--
-- Retour arrière : 20261120-cp-google-rollback.sql, seulement tant que la colonne est entièrement vide.

-- 1. Empreinte des lignes, sans les autres colonnes.
CREATE TABLE IF NOT EXISTS conseillers_personnels_bak_20261120_cp_google
  AS SELECT CP_RefConseiller, CP_Actif, CP_Statut
       FROM conseillers_personnels;

-- 2. Colonne nullable, en fin de table, sans réécriture des lignes.
ALTER TABLE conseillers_personnels
  ADD COLUMN CP_Google VARCHAR(512)
    CHARACTER SET latin1
    COLLATE latin1_swedish_ci
    NULL
    DEFAULT NULL,
  ALGORITHM=INSTANT;

-- Contrôle après :
--   SHOW COLUMNS FROM conseillers_personnels LIKE 'CP_Google';
--   -- Type varchar(512), Null YES, Default NULL
--   SELECT COUNT(*) AS rows_after,
--          SUM(CP_Google IS NOT NULL) AS filled
--     FROM conseillers_personnels;
--   -- rows_after = 379, filled = 0
--   SELECT COUNT(*) FROM conseillers_personnels_bak_20261120_cp_google;
--   -- 379
