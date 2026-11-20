-- Retour arrière pour 20261120-cp-pageweb-parcours.sql
-- Remet à NULL les 4 champs sur les 40 conseillers alimentés.
-- La table de sauvegarde conseillers_personnels_bak_20261120_pageweb_parcours est conservée.

SET NAMES utf8mb4;

START TRANSACTION;

UPDATE conseillers_personnels
   SET CP_pageWebFr = NULL,
       CP_pageWebEn = NULL,
       CP_parcours_fr = NULL,
       CP_parcours_en = NULL
 WHERE CP_RefConseiller IN (0, 3, 55, 70, 81, 93, 102, 115, 117, 127, 128, 140, 162, 164, 167, 168, 171, 178, 191, 193, 194, 231, 239, 256, 265, 274, 276, 278, 280, 282, 290, 305, 322, 339, 343, 347, 356, 357, 367, 374);

COMMIT;

-- Suppression complète des 4 colonnes (uniquement si l'API n'y référence plus rien) :
-- ALTER TABLE conseillers_personnels
--   DROP COLUMN CP_pageWebFr,
--   DROP COLUMN CP_pageWebEn,
--   DROP COLUMN CP_parcours_fr,
--   DROP COLUMN CP_parcours_en;
