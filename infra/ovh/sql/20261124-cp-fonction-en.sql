-- Ajoute conseillers_personnels.CP_FonctionEn : titre affiché sous le nom
-- sur la page anglaise du conseiller. La page française continue de lire
-- CP_Fonction. La colonne naît vide : un titre vide ne retire pas celui
-- déjà publié. Un second passage ne réécrit pas une valeur déjà posée.
--
-- Contrôle avant :
--   SELECT COLUMN_NAME FROM information_schema.COLUMNS
--    WHERE TABLE_SCHEMA = 'perle-rareinfo'
--      AND TABLE_NAME = 'conseillers_personnels'
--      AND COLUMN_NAME = 'CP_FonctionEn';
--   -- attendu : aucune ligne
--
-- Retour arrière : 20261124-cp-fonction-en-rollback.sql

CREATE TABLE IF NOT EXISTS conseillers_personnels_bak_20261124_fonction_en
  AS SELECT CP_RefConseiller, CP_Fonction
       FROM conseillers_personnels;

ALTER TABLE conseillers_personnels
  ADD COLUMN CP_FonctionEn VARCHAR(70)
    CHARACTER SET latin1
    COLLATE latin1_swedish_ci
    NULL
    DEFAULT NULL,
  ALGORITHM=INSTANT;

-- Contrôle après :
--   SHOW COLUMNS FROM conseillers_personnels LIKE 'CP_FonctionEn';
--   -- varchar(70), NULL
