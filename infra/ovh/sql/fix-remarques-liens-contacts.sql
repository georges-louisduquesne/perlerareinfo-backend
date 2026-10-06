-- Liens vers une fiche contact de l'ancien CRM dans C_Remarques
--   https://perle-rare.info/#/view/page/contacts_recherche_form&ref%3D123 (et =, index.php?page=, old., www., dev., http)
-- → nouvelle fiche https://perle-rare.info/#/contact/123
-- Inventaire 2026-10-06 (SELECT) : 2293 lignes ; triggers BEFORE UPDATE sans effet sur ces lignes
-- (nom / adresse / ville / pays déjà en majuscules, téléphones sans espaces) ; longueur max après 120 (< 200).
-- 9 lignes mal formées laissées telles quelles, à corriger à la main :
--   47027, 47077, 47440, 54926, 56319, 58255, 59542, 62102, 62175.
-- Idempotent : une seconde exécution ne trouve plus rien et ne réécrit pas la sauvegarde.
-- À exécuter uniquement sur ordre écrit (écriture sur la base prod partagée).

SET @re = 'https?://(www\\.|old\\.|dev\\.)?perle-rare\\.info/(#/view/page/|index\\.php\\?page=)contacts_recherche_form&ref(=|%3[dD])([0-9]+)';

CREATE TABLE IF NOT EXISTS contacts_recherche_bak_remarques_20261006 AS
SELECT C_RefContact, C_Remarques
FROM contacts_recherche
WHERE C_Remarques REGEXP @re;

START TRANSACTION;

UPDATE contacts_recherche
SET C_Remarques = REGEXP_REPLACE(C_Remarques, @re, 'https://perle-rare.info/#/contact/\\4')
WHERE C_Remarques REGEXP @re;

SELECT ROW_COUNT() AS lignes_modifiees;

-- Contrôle : 0 attendu.
SELECT COUNT(*) AS encore_anciens_liens FROM contacts_recherche WHERE C_Remarques REGEXP @re;

COMMIT;

-- Retour arrière (écrase aussi les Remarques modifiées depuis par les utilisateurs sur ces lignes) :
-- UPDATE contacts_recherche c JOIN contacts_recherche_bak_remarques_20261006 b USING (C_RefContact)
-- SET c.C_Remarques = b.C_Remarques;
