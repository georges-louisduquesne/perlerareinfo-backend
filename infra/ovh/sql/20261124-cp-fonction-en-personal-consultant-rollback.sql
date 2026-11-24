-- Annule 20261124-cp-fonction-en-personal-consultant.sql
-- Restaure CP_FonctionEn des 42 actifs alimentés. Ne supprime pas la sauvegarde.
--
-- Contrôle avant :
--   SELECT COUNT(*) FROM conseillers_personnels_bak_20261124_fonction_en_valeurs;
--   -- attendu : 42

UPDATE conseillers_personnels AS c
  JOIN conseillers_personnels_bak_20261124_fonction_en_valeurs AS b
    ON b.CP_RefConseiller = c.CP_RefConseiller
   SET c.CP_FonctionEn = b.CP_FonctionEn;

-- Contrôle après :
--   SELECT COUNT(*) FROM conseillers_personnels
--    WHERE CP_Actif = 1
--      AND CP_RefConseiller NOT IN (0, 3, 194)
--      AND CP_FonctionEn = 'Personal consultant';
--   -- attendu : 0, si la sauvegarde était vide
