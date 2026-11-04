-- Télémétrie fine accueil (JSON v1) — colonne optionnelle.
-- SELECT de contrôle avant/après :
--   SHOW COLUMNS FROM home_page_loads LIKE 'HPL_Breakdown';

ALTER TABLE home_page_loads
  ADD COLUMN HPL_Breakdown varchar(2048) NULL DEFAULT NULL
  AFTER HPL_MeasuredAt;
