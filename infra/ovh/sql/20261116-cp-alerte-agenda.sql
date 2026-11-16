-- Ajoute conseillers_personnels.CP_AlerteAgenda.
-- 1 = le bouton « Ajouter à l'agenda » est coché à la création d'une alerte.
-- Contrôle avant (2026-11-16, MariaDB 10.5.23) :
--   colonne absente (information_schema = 0), 376 lignes, table ~1,56 Mo.
--   Les triggers before insert/update ne réécrivent que CP_Ville, CP_Adresse et CP_TelPersonnel.
--
-- Sauvegarde des lignes d'avant l'ALTER (sans la nouvelle colonne).
-- Retour arrière de la colonne : voir le bas du fichier. Ne pas supprimer la sauvegarde dans la foulée.

CREATE TABLE IF NOT EXISTS conseillers_personnels_bak_20261116_cp_alerte_agenda
  AS SELECT * FROM conseillers_personnels;

ALTER TABLE conseillers_personnels
  ADD COLUMN IF NOT EXISTS CP_AlerteAgenda TINYINT(1) NOT NULL DEFAULT 1,
  ALGORITHM=INSTANT;

-- Contrôle après :
--   SHOW COLUMNS FROM conseillers_personnels LIKE 'CP_AlerteAgenda';
--   -- tinyint(1), Null NO, Default 1
--   SELECT COUNT(*) AS rows_after,
--          SUM(CP_AlerteAgenda = 1) AS checked_default,
--          SUM(CP_AlerteAgenda = 0) AS unchecked
--     FROM conseillers_personnels;
--   -- rows_after = 376, checked_default = 376, unchecked = 0
--   SELECT COUNT(*) AS backup_rows FROM conseillers_personnels_bak_20261116_cp_alerte_agenda;
--   -- backup_rows = 376
--
-- Retour arrière, tant qu'aucune préférence personnelle ne doit être conservée :
--   ALTER TABLE conseillers_personnels DROP COLUMN CP_AlerteAgenda;
-- La table de sauvegarde reste tant que ce retour arrière n'a pas été vérifié.
--   DROP TABLE conseillers_personnels_bak_20261116_cp_alerte_agenda;
