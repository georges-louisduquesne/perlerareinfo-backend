-- Ajoute conseillers_personnels.CP_photoWeb et y range l'URL du portrait
-- jpg 555x555 déjà publié sur perle-rare.com, pour les membres actifs
-- qui ne sont pas candidats. Cette URL est la donnée maîtresse : à
-- l'enregistrement de la fiche, les pages web FR et EN reprennent ce portrait.
-- Les autres lignes restent NULL. Un second passage ne réécrit pas une URL déjà posée.
-- CP_Photo (fichier interne cp_files) n'est pas modifié.
-- Jeu de caractères de la table : latin1. Les URL sont en ASCII.
--
-- Contrôle avant (2026-11-23) :
--   SELECT COLUMN_NAME FROM information_schema.COLUMNS
--    WHERE TABLE_SCHEMA = 'perle-rareinfo'
--      AND TABLE_NAME = 'conseillers_personnels'
--      AND COLUMN_NAME = 'CP_photoWeb';
--   -- attendu : aucune ligne
--   SELECT COUNT(*) AS actifs
--     FROM conseillers_personnels
--    WHERE CP_Actif = 1 AND IFNULL(CP_Statut, '') <> 'Candidat';
--   -- attendu : 40
--
-- Retour arrière : 20261123-cp-photo-web-rollback.sql

-- 1. Empreinte des lignes, sans les autres colonnes.
CREATE TABLE IF NOT EXISTS conseillers_personnels_bak_20261123_photo_web
  AS SELECT CP_RefConseiller, CP_Actif, CP_Statut
       FROM conseillers_personnels;

-- 2. Colonne nullable, en fin de table, sans réécriture des lignes.
ALTER TABLE conseillers_personnels
  ADD COLUMN CP_photoWeb VARCHAR(512)
    CHARACTER SET latin1
    COLLATE latin1_swedish_ci
    NULL
    DEFAULT NULL,
  ALGORITHM=INSTANT;

-- 3. URL du jpg 555x555, seulement tant que la colonne est vide pour cette ligne.
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2023/01/chasseur-immobilier-perle-rare-ariane-angeloglou-1.jpg' WHERE CP_RefConseiller = 55 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2026/05/chasseur-immobilier-perle-rare-roxane-bache-1.jpg' WHERE CP_RefConseiller = 347 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2022/10/chasseur-immobilier-perle-rare-marie-bariller-1.jpg' WHERE CP_RefConseiller = 167 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2023/01/chasseur-immobilier-perle-rare-laurence-berry-1.jpg' WHERE CP_RefConseiller = 81 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2023/01/chasseur-immobilier-perle-rare-catherine-blanchard-1.jpg' WHERE CP_RefConseiller = 164 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2023/01/chasseur-immobilier-perle-rare-amelie-cailliez-1.jpg' WHERE CP_RefConseiller = 102 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2025/04/chasseur-immobilier-perle-rare-aude-chamboredon-1.jpg' WHERE CP_RefConseiller = 256 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2024/05/chasseur-immobilier-perle-rare-charles-champin-1.jpg' WHERE CP_RefConseiller = 191 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2023/01/chasseur-immobilier-perle-rare-caroline-charvet-1.jpg' WHERE CP_RefConseiller = 128 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2026/06/chasseur-immobilier-perle-rare-isabelle-chetochine-1.jpg' WHERE CP_RefConseiller = 357 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2026/09/chasseur-immobilier-perle-rare-marta-clery-1.jpg' WHERE CP_RefConseiller = 343 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2025/10/chasseur-immobilier-perle-rare-anne-coste-1-1.jpg' WHERE CP_RefConseiller = 278 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2025/06/chasseur-immobilier-perle-rare-anne-davoli-1.jpg' WHERE CP_RefConseiller = 265 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2026/11/chasseur-immobilier-perle-rare-solveig-de-Fontgalland-1.jpg' WHERE CP_RefConseiller = 374 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2026/11/chasseur-immobilier-perle-rare-diane-de-Monteynard-1.jpg' WHERE CP_RefConseiller = 367 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2023/01/chasseur-immobilier-perle-rare-georges-louis-duquesne-1.jpg' WHERE CP_RefConseiller = 0 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2023/01/chasseur-immobilier-perle-rare-claire-charlotte-foity-1.jpg' WHERE CP_RefConseiller = 168 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2023/09/chasseur-immobilier-perle-rare-alexandre-gagnat-1.jpg' WHERE CP_RefConseiller = 178 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2025/06/chasseur-immobilier-perle-rare-fabiola-hemmet-1.jpg' WHERE CP_RefConseiller = 231 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2023/04/chasseur-immobilier-perle-rare-catherine-heuze-1.jpg' WHERE CP_RefConseiller = 115 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2026/06/chasseur-immobilier-perle-rare-lionel-jamet-1.jpg' WHERE CP_RefConseiller = 339 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2025/09/chasseur-immobilier-perle-rare-clemence-kamienny-1.jpg' WHERE CP_RefConseiller = 280 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2025/04/chasseur-immobilier-perle-rare-corinne-lelarge-1.jpg' WHERE CP_RefConseiller = 239 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2023/01/chasseur-immobilier-perle-rare-isabelle-lerre-1.jpg' WHERE CP_RefConseiller = 140 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2025/10/chasseur-immobilier-perle-rare-sophie-maccaffry-1.jpg' WHERE CP_RefConseiller = 274 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2025/01/chasseur-immobilier-perle-rare-marion-normand-1.jpg' WHERE CP_RefConseiller = 194 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2026/01/chasseur-immobilier-perle-rare-dessislava-pavlova-1.jpg' WHERE CP_RefConseiller = 282 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2023/01/chasseur-immobilier-perle-rare-constance-perrin-1.jpg' WHERE CP_RefConseiller = 127 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2025/09/chasseur-immobilier-perle-rare-chloris-perrotin-1.jpg' WHERE CP_RefConseiller = 276 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2023/01/chasseur-immobilier-perle-rare-benedicte-peynet-1.jpg' WHERE CP_RefConseiller = 70 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2026/06/chasseur-immobilier-perle-rare-laurence-pinaire-1.jpg' WHERE CP_RefConseiller = 322 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2026/01/chasseur-immobilier-perle-rare-chloe-poissonnet-1.jpg' WHERE CP_RefConseiller = 305 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2026/09/chasseur-immobilier-perle-rare-elsa-pons-1.jpg' WHERE CP_RefConseiller = 290 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2024/09/chasseur-immobilier-perle-rare-caroline-rapp-trevisan-1.jpg' WHERE CP_RefConseiller = 193 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2026/06/chasseur-immobilier-perle-rare-florence-roux-de-bezieux-1.jpg' WHERE CP_RefConseiller = 356 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2023/03/chasseur-immobilier-perle-rare-margot-saidy-1.jpg' WHERE CP_RefConseiller = 171 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2023/09/chasseur-immobilier-perle-rare-pascale-saint-jean-1.jpg' WHERE CP_RefConseiller = 117 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2023/01/chasseur-immobilier-perle-rare-celine-thieriet-1.jpg' WHERE CP_RefConseiller = 3 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2023/09/chasseur-immobilier-perle-rare-stephane-trichart-1.jpg' WHERE CP_RefConseiller = 162 AND CP_photoWeb IS NULL;
UPDATE conseillers_personnels SET CP_photoWeb = 'https://perle-rare.com/wp-content/uploads/2023/01/chasseur-immobilier-perle-rare-karine-warman-1.jpg' WHERE CP_RefConseiller = 93 AND CP_photoWeb IS NULL;

-- Contrôle après :
--   SHOW COLUMNS FROM conseillers_personnels LIKE 'CP_photoWeb';
--   SELECT COUNT(*) AS filled FROM conseillers_personnels WHERE CP_photoWeb IS NOT NULL;
--   -- filled = 40
