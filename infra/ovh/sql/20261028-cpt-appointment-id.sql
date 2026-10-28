-- Ajoute conseillers_personnels_taches.CPT_AppointmentId : identifiant du
-- rendez-vous Exchange lié à une alerte candidat (créneau libre, pas occupé).
-- Aucune ligne existante n’est réécrite. La colonne est nullable.
--
-- Contrôle avant :
--   SELECT COLUMN_NAME
--     FROM INFORMATION_SCHEMA.COLUMNS
--    WHERE TABLE_SCHEMA = DATABASE()
--      AND TABLE_NAME = 'conseillers_personnels_taches'
--      AND COLUMN_NAME = 'CPT_AppointmentId';
--   -- 0 ligne attendue.

ALTER TABLE conseillers_personnels_taches
  ADD COLUMN CPT_AppointmentId VARCHAR(512)
    CHARACTER SET utf8
    COLLATE utf8_general_ci
    NULL
    DEFAULT NULL;

-- Contrôle après :
--   SHOW COLUMNS FROM conseillers_personnels_taches LIKE 'CPT_AppointmentId';
--   -- Type attendu : varchar(512), Null YES, Default NULL
--
-- Retour arrière :
--   ALTER TABLE conseillers_personnels_taches DROP COLUMN CPT_AppointmentId;
