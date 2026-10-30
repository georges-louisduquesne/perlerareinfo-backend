-- Retour arrière de 20261030-origines-candidats.sql.
-- À n’exécuter que si aucune origine n’a été saisie :
--   SELECT COUNT(*) AS filled FROM conseillers_personnels WHERE CP_Origine IS NOT NULL;
--   -- attendu : 0
-- La table origines_candidats reste en place (pas de DROP).

ALTER TABLE conseillers_personnels
  DROP FOREIGN KEY FK_conseillers_personnels_origines_candidats;

ALTER TABLE conseillers_personnels
  DROP COLUMN CP_Origine;
