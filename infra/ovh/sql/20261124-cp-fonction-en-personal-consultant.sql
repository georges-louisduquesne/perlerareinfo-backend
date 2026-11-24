-- Pose CP_FonctionEn = 'Personal consultant' pour les conseillers actifs,
-- sauf Céline Thieriet (3), Georges-Louis Duquesne (0) et Marion Normand (194).
-- La colonne est la source du titre sous le nom sur la page anglaise :
-- le prochain enregistrement de la fiche recopie cette valeur et republie la page.
-- Un second passage réécrit la même valeur.
--
-- Contrôle avant :
--   SELECT COUNT(*) FROM conseillers_personnels
--    WHERE CP_Actif = 1 AND CP_RefConseiller NOT IN (0, 3, 194);
--   -- attendu : 42
--   SELECT CP_RefConseiller, CP_Prenom, CP_NomFamille, CP_FonctionEn
--     FROM conseillers_personnels
--    WHERE CP_RefConseiller IN (0, 3, 194);
--   -- ces trois lignes ne doivent pas changer
--
-- Retour arrière : 20261124-cp-fonction-en-personal-consultant-rollback.sql

CREATE TABLE IF NOT EXISTS conseillers_personnels_bak_20261124_fonction_en_valeurs
  AS SELECT CP_RefConseiller, CP_Prenom, CP_NomFamille, CP_Actif, CP_FonctionEn
       FROM conseillers_personnels
      WHERE CP_Actif = 1
        AND CP_RefConseiller NOT IN (0, 3, 194);

UPDATE conseillers_personnels
   SET CP_FonctionEn = 'Personal consultant'
 WHERE CP_Actif = 1
   AND CP_RefConseiller NOT IN (0, 3, 194);

-- Contrôle après :
--   SELECT COUNT(*) FROM conseillers_personnels
--    WHERE CP_Actif = 1
--      AND CP_RefConseiller NOT IN (0, 3, 194)
--      AND CP_FonctionEn = 'Personal consultant';
--   -- attendu : 42
--   SELECT CP_Prenom, CP_NomFamille, CP_FonctionEn
--     FROM conseillers_personnels
--    WHERE CP_RefConseiller IN (0, 3, 194);
--   -- CP_FonctionEn inchangé (NULL)
