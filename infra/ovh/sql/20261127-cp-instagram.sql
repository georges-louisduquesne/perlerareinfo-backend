-- Ajoute conseillers_personnels.CP_Instagram : lien de page Instagram, vide pour toutes les lignes.
-- Aucune valeur existante n’est réécrite. Les triggers de la table ne lisent pas cette colonne.
-- Jeu de caractères de la table : latin1 / latin1_swedish_ci. Les URL Instagram sont en ASCII.
-- Même forme que CP_LinkedIn : VARCHAR(100), nullable, défaut NULL.
--
-- Contrôle avant (2026-11-27) :
--   SELECT COLUMN_NAME FROM information_schema.COLUMNS
--    WHERE TABLE_SCHEMA = 'perle-rareinfo'
--      AND TABLE_NAME = 'conseillers_personnels'
--      AND COLUMN_NAME = 'CP_Instagram';
--   -- attendu : aucune ligne
--   SELECT COUNT(*) AS rows_before FROM conseillers_personnels;
--   -- attendu : 383
--
-- Retour arrière : 20261127-cp-instagram-rollback.sql, seulement tant que la colonne est entièrement vide.

-- 1. Empreinte des lignes, sans les autres colonnes.
CREATE TABLE IF NOT EXISTS conseillers_personnels_bak_20261127_cp_instagram
  AS SELECT CP_RefConseiller, CP_Actif, CP_Statut
       FROM conseillers_personnels;

-- 2. Colonne nullable, en fin de table, sans réécriture des lignes.
ALTER TABLE conseillers_personnels
  ADD COLUMN CP_Instagram VARCHAR(100)
    CHARACTER SET latin1
    COLLATE latin1_swedish_ci
    NULL
    DEFAULT NULL,
  ALGORITHM=INSTANT;

-- Contrôle après :
--   SHOW COLUMNS FROM conseillers_personnels LIKE 'CP_Instagram';
--   -- Type varchar(100), Null YES, Default NULL
--   SELECT COUNT(*) AS rows_after,
--          SUM(CP_Instagram IS NOT NULL) AS filled
--     FROM conseillers_personnels;
--   -- rows_after = 383, filled = 0
--   SELECT COUNT(*) FROM conseillers_personnels_bak_20261127_cp_instagram;
--   -- 383
