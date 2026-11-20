-- Alimente conseillers_personnels.CP_LinkedIn pour Georges-Louis Duquesne
-- (CP_RefConseiller = 0) avec l’URL affichée sur sa page biographie.
-- Aucune autre ligne n’est modifiée. La colonne est en latin1 ; l’URL est en ASCII (40 caractères, varchar(100)).
--
-- Contrôle avant (2026-11-20) :
--   SELECT CP_RefConseiller, CP_Prenom, CP_NomFamille, CP_LinkedIn
--     FROM conseillers_personnels
--    WHERE CP_RefConseiller = 0;
--   -- résultat : Georges-Louis Duquesne, CP_LinkedIn NULL
--
-- La création d’une table de sauvegarde a été refusée par le compte
-- (DDL operations are not allowed, SCHEMA_DDL_PERMISSIONS).
-- La valeur d’origine est NULL, vérifiée par le SELECT ci-dessus.
-- Retour arrière : 20261120-cp-linkedin-gld-rollback.sql.

UPDATE conseillers_personnels
   SET CP_LinkedIn = 'https://www.linkedin.com/in/glduquesne/'
 WHERE CP_RefConseiller = 0
   AND CP_Prenom = 'Georges-Louis'
   AND CP_NomFamille = 'Duquesne'
   AND CP_LinkedIn IS NULL;

-- Contrôle après :
--   SELECT CP_RefConseiller, CP_Prenom, CP_NomFamille, CP_LinkedIn
--     FROM conseillers_personnels
--    WHERE CP_RefConseiller = 0;
--   -- attendu : https://www.linkedin.com/in/glduquesne/
