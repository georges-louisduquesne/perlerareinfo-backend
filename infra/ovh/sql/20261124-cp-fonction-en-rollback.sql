-- Annule 20261124-cp-fonction-en.sql : remet le titre anglais à NULL.
-- La colonne reste en place. La suppression de colonne est commentée.
--
-- Contrôle avant :
--   SELECT COUNT(*) FROM conseillers_personnels WHERE CP_FonctionEn IS NOT NULL;

UPDATE conseillers_personnels
   SET CP_FonctionEn = NULL
 WHERE CP_FonctionEn IS NOT NULL;

-- ALTER TABLE conseillers_personnels DROP COLUMN CP_FonctionEn;

-- Contrôle après :
--   SELECT COUNT(*) FROM conseillers_personnels WHERE CP_FonctionEn IS NOT NULL;
--   -- attendu : 0
