-- Compteurs publics (likes / commentaires) et comptes qui ont aimé, sur les vignettes.
--
-- Contrôle avant :
--   SELECT COLUMN_NAME FROM information_schema.COLUMNS
--    WHERE TABLE_SCHEMA = 'perle-rareinfo'
--      AND TABLE_NAME = 'publications_reseaux'
--      AND COLUMN_NAME IN ('PUB_Likes', 'PUB_Commentaires', 'PUB_Likeurs');
--   -- attendu : aucune ligne
--
-- Retour arrière : 20261127-publications-engagement-rollback.sql

CREATE TABLE IF NOT EXISTS publications_reseaux_bak_20261127_engagement
  AS SELECT * FROM publications_reseaux;

ALTER TABLE publications_reseaux
  ADD COLUMN PUB_Likes INT NOT NULL DEFAULT 0,
  ADD COLUMN PUB_Commentaires INT NOT NULL DEFAULT 0,
  ADD COLUMN PUB_Likeurs VARCHAR(2000) NULL,
  ALGORITHM=INPLACE, LOCK=NONE;
