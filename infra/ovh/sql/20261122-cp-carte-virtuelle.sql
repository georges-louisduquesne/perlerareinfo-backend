-- Ajoute conseillers_personnels.CP_CarteVirtuelle et y range le lien public
-- de la carte de visite, pour les membres actifs qui ne sont pas candidats.
-- Georges-Louis (CP_RefConseiller = 0) conserve la page déjà publiée.
-- Les autres lignes restent NULL. Un second passage ne réécrit pas un lien déjà posé.
-- Jeu de caractères de la table : latin1 / latin1_swedish_ci. Les URL sont en ASCII.
--
-- Contrôle avant :
--   SELECT COLUMN_NAME FROM information_schema.COLUMNS
--    WHERE TABLE_SCHEMA = 'perle-rareinfo'
--      AND TABLE_NAME = 'conseillers_personnels'
--      AND COLUMN_NAME = 'CP_CarteVirtuelle';
--   -- attendu : aucune ligne
--
-- Retour arrière : 20261122-cp-carte-virtuelle-rollback.sql

-- 1. Empreinte des lignes, sans les autres colonnes.
CREATE TABLE IF NOT EXISTS conseillers_personnels_bak_20261122_carte
  AS SELECT CP_RefConseiller, CP_Actif, CP_Statut
       FROM conseillers_personnels;

-- 2. Colonne nullable, en fin de table, sans réécriture des lignes.
ALTER TABLE conseillers_personnels
  ADD COLUMN CP_CarteVirtuelle VARCHAR(255)
    CHARACTER SET latin1
    COLLATE latin1_swedish_ci
    NULL
    DEFAULT NULL,
  ALGORITHM=INSTANT;

-- 3. Lien public, seulement tant que la colonne est vide pour cette ligne.
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-ariane-angeloglou/' WHERE CP_RefConseiller = 55 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-roxane-bache/' WHERE CP_RefConseiller = 347 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-marie-bariller/' WHERE CP_RefConseiller = 167 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-laurence-berry/' WHERE CP_RefConseiller = 81 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-catherine-blanchard/' WHERE CP_RefConseiller = 164 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-amelie-cailliez/' WHERE CP_RefConseiller = 102 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-aude-chamboredon/' WHERE CP_RefConseiller = 256 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-charles-champin/' WHERE CP_RefConseiller = 191 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-caroline-charvet/' WHERE CP_RefConseiller = 128 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-isabelle-chetochine/' WHERE CP_RefConseiller = 357 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-marta-clery/' WHERE CP_RefConseiller = 343 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-anne-coste/' WHERE CP_RefConseiller = 278 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-anne-davoli/' WHERE CP_RefConseiller = 265 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-solveig-de-fontgalland/' WHERE CP_RefConseiller = 374 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-diane-de-monteynard/' WHERE CP_RefConseiller = 367 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-georges-louis-duquesne/' WHERE CP_RefConseiller = 0 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-claire-charlotte-foity/' WHERE CP_RefConseiller = 168 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-alexandre-gagnat/' WHERE CP_RefConseiller = 178 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-fabiola-hemmet/' WHERE CP_RefConseiller = 231 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-catherine-heuze/' WHERE CP_RefConseiller = 115 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-lionel-jamet/' WHERE CP_RefConseiller = 339 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-clemence-kamienny/' WHERE CP_RefConseiller = 280 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-corinne-noelle-lelarge/' WHERE CP_RefConseiller = 239 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-isabelle-lerre/' WHERE CP_RefConseiller = 140 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-sophie-mccaffry/' WHERE CP_RefConseiller = 274 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-marion-normand/' WHERE CP_RefConseiller = 194 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-dessislava-pavlova/' WHERE CP_RefConseiller = 282 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-constance-perrin/' WHERE CP_RefConseiller = 127 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-chloris-perrotin/' WHERE CP_RefConseiller = 276 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-benedicte-peynet/' WHERE CP_RefConseiller = 70 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-laurence-pinaire/' WHERE CP_RefConseiller = 322 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-chloe-poissonnet/' WHERE CP_RefConseiller = 305 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-elsa-pons/' WHERE CP_RefConseiller = 290 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-caroline-rapp-trevisan/' WHERE CP_RefConseiller = 193 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-florence-roux-de-bezieux/' WHERE CP_RefConseiller = 356 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-margot-saidy/' WHERE CP_RefConseiller = 171 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-pascale-saint-jean/' WHERE CP_RefConseiller = 117 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-celine-thieriet/' WHERE CP_RefConseiller = 3 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-stephane-trichard/' WHERE CP_RefConseiller = 162 AND CP_CarteVirtuelle IS NULL;
UPDATE conseillers_personnels SET CP_CarteVirtuelle = 'https://perle-rare.com/fr/carte-virtuelle-karine-warman/' WHERE CP_RefConseiller = 93 AND CP_CarteVirtuelle IS NULL;

-- Contrôle après :
--   SHOW COLUMNS FROM conseillers_personnels LIKE 'CP_CarteVirtuelle';
--   SELECT COUNT(*) AS filled FROM conseillers_personnels WHERE CP_CarteVirtuelle IS NOT NULL;
--   -- filled = 40
