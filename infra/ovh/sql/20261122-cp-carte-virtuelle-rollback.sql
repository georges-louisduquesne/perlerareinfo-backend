-- Annule 20261122-cp-carte-virtuelle.sql : remet les 40 liens à NULL.
-- La colonne reste en place. La suppression de colonne est commentée :
-- elle ne doit pas être lancée avec le reste de ce fichier.
--
-- Contrôle avant :
--   SELECT COUNT(*) FROM conseillers_personnels WHERE CP_CarteVirtuelle IS NOT NULL;
--   -- attendu : 40

UPDATE conseillers_personnels
   SET CP_CarteVirtuelle = NULL
 WHERE CP_RefConseiller IN (
   0, 3, 55, 70, 81, 93, 102, 115, 117, 127, 128, 140, 162, 164, 167, 168,
   171, 178, 191, 193, 194, 231, 239, 256, 265, 274, 276, 278, 280, 282, 290,
   305, 322, 339, 343, 347, 356, 357, 367, 374
 );

-- ALTER TABLE conseillers_personnels DROP COLUMN CP_CarteVirtuelle;

-- Contrôle après :
--   SELECT COUNT(*) FROM conseillers_personnels WHERE CP_CarteVirtuelle IS NOT NULL;
--   -- attendu : 0
