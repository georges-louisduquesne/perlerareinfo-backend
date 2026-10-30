-- Ajoute conseillers_personnels_evenements.CPE_AppointmentId : identifiant
-- Exchange du rendez-vous candidat (RV CANDIDAT, RV TEL CANDIDAT, RV VISIO CANDIDAT).
-- Aucune ligne existante n’est réécrite. La colonne est nullable.
--
-- Contrôle avant :
--   SELECT COLUMN_NAME
--     FROM INFORMATION_SCHEMA.COLUMNS
--    WHERE TABLE_SCHEMA = DATABASE()
--      AND TABLE_NAME = 'conseillers_personnels_evenements'
--      AND COLUMN_NAME = 'CPE_AppointmentId';
--   -- 0 ligne attendue.

ALTER TABLE conseillers_personnels_evenements
  ADD COLUMN CPE_AppointmentId VARCHAR(512)
    CHARACTER SET utf8
    COLLATE utf8_general_ci
    NULL
    DEFAULT NULL;

-- Contrôle après :
--   SHOW COLUMNS FROM conseillers_personnels_evenements LIKE 'CPE_AppointmentId';
--   -- Type attendu : varchar(512), Null YES, Default NULL
--
-- Retour arrière :
--   ALTER TABLE conseillers_personnels_evenements DROP COLUMN CPE_AppointmentId;
