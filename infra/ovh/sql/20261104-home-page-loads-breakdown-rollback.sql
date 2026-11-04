-- Rollback télémétrie fine accueil (supprime la colonne JSON, conserve les lignes existantes).
-- Contrôle avant :
--   SELECT COLUMN_NAME FROM information_schema.COLUMNS
--   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'home_page_loads' AND COLUMN_NAME = 'HPL_Breakdown';

ALTER TABLE home_page_loads
  DROP COLUMN HPL_Breakdown;
