-- Annule les 119 désactivations d'agents Barnes du 3 novembre 2026
-- (actor_search 171, barnes-scraper, « Absent du listing Barnes (03/11/2026) »).
-- Le même soir, le scraper d'agences avait déjà coupé le réseau sur un scrape incomplet.
--
-- Le compte SQL ne peut pas créer de table de copie (SCHEMA_DDL_PERMISSIONS).
-- Avant : C_Statut = 'Inactif' et C_Com contient la mention du 03/11/2026.
-- Aucun de ces contacts n'a été redésactivé par un run ultérieur.
-- Le commentaire antérieur (avant « | Absent du listing… ») est conservé.
--
-- Appliqué le 10 novembre 2026 : 119 contacts
-- (statut Actif, mention retirée, commentaires antérieurs conservés).

UPDATE contact_intermediaire
SET C_Statut = 'Actif',
    C_Com = TRIM(REPLACE(REPLACE(IFNULL(C_Com, ''),
        ' | Absent du listing Barnes (03/11/2026)', ''),
        'Absent du listing Barnes (03/11/2026)', ''))
WHERE C_Statut = 'Inactif'
  AND C_Com LIKE '%Absent du listing Barnes (03/11/2026)%'
  AND C_RefCtcInter IN (
     929,  1597,  1762,  1941,  2174,  2341,  2618,  3842,  3958,  4254,
    4331,  4339,  4416,  4418,  4419,  4424,  4440,  4441,  4442,  4443,
    4445,  4449,  4464,  4467,  4470,  4471,  4526,  4964,  5145,  5352,
    5396,  5577,  5843,  6090,  6296,  6775,  6936,  7365,  7426,  7580,
    7584,  7750,  7785,  7893,  7976,  7979,  8410,  8507,  8657,  8671,
    8937,  9009,  9046,  9074,  9288,  9534,  9635,  9653,  9667, 10164,
   10187, 10280, 10449, 10460, 10486, 10512, 10525, 10567, 10701, 10717,
   10752, 10805, 10811, 10831, 10932, 11053, 11095, 11136, 11330, 11391,
   11404, 11424, 11426, 11433, 11436, 11461, 11728, 11739, 11868, 11911,
   12116, 12248, 12516, 12557, 12611, 12628, 12905, 12963, 12990, 13150,
   13297, 13317, 13322, 13478, 13493, 13507, 13583, 14095, 14112, 14131,
   14136, 14139, 14148, 14160, 14191, 14630, 14691, 14738, 15190
  );

-- Retour arrière :
-- UPDATE contact_intermediaire
-- SET C_Statut = 'Inactif',
--     C_Com = CASE
--       WHEN C_Com IS NULL OR TRIM(C_Com) = '' THEN 'Absent du listing Barnes (03/11/2026)'
--       ELSE CONCAT(TRIM(C_Com), ' | Absent du listing Barnes (03/11/2026)')
--     END
-- WHERE C_Statut = 'Actif'
--   AND C_RefCtcInter IN ( ... même liste ... );
