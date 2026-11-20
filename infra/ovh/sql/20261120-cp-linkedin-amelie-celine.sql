-- Alimente conseillers_personnels.CP_LinkedIn pour Amélie Cailliez et Céline Thieriet
-- avec l’URL publiée sur leur page WordPress (perle-rare.com).
-- Aucune autre ligne n’est modifiée. La colonne est en latin1 ; les URL sont en ASCII.
--
-- Contrôle avant (2026-11-20) :
--   SELECT CP_RefConseiller, CP_Prenom, CP_NomFamille, CP_LinkedIn
--     FROM conseillers_personnels
--    WHERE CP_RefConseiller IN (3, 102);
--   -- Céline Thieriet (3) et Amélie Cailliez (102), CP_LinkedIn NULL
--
-- La création d’une table de sauvegarde a été refusée par le compte
-- (DDL operations are not allowed, SCHEMA_DDL_PERMISSIONS).
-- La valeur d’origine est NULL, vérifiée par le SELECT ci-dessus.
-- Retour arrière : 20261120-cp-linkedin-amelie-celine-rollback.sql.

UPDATE conseillers_personnels
   SET CP_LinkedIn = 'https://www.linkedin.com/in/celine-thieriet-61b1b71/'
 WHERE CP_RefConseiller = 3
   AND CP_Prenom = 'Céline'
   AND CP_NomFamille = 'Thieriet'
   AND CP_LinkedIn IS NULL;

UPDATE conseillers_personnels
   SET CP_LinkedIn = 'https://www.linkedin.com/in/am%C3%A9lie-cailliez-867426170/'
 WHERE CP_RefConseiller = 102
   AND CP_Prenom = 'Amélie'
   AND CP_NomFamille = 'Cailliez'
   AND CP_LinkedIn IS NULL;

-- Contrôle après :
--   SELECT CP_RefConseiller, CP_Prenom, CP_NomFamille, CP_LinkedIn
--     FROM conseillers_personnels
--    WHERE CP_RefConseiller IN (3, 102);
