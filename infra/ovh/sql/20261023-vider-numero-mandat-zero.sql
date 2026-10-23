-- Vide C_NumeroMandat lorsqu'il vaut 0 (98 fiches au 23 oct. 2026, toutes à 0).
-- À blanc : 98 lignes, 0 nom/adresse/ville/pays hors majuscules, 0 téléphone avec espace.
-- Le trigger before_update ne réécrit donc aucun autre champ sur ce lot.
-- La création de table de copie est refusée par le compte (SCHEMA_DDL_PERMISSIONS).
-- La liste des C_RefContact ci-dessous est la copie : chaque valeur d'origine est 0.
-- Appliqué le 23 oct. 2026 : 98 lignes passées à NULL.

UPDATE contacts_recherche
SET C_NumeroMandat = NULL
WHERE C_NumeroMandat = 0
  AND C_RefContact IN (
    2154, 16528, 17499, 19673, 19674, 20566, 23856, 24348, 26020, 26481,
    26515, 27308, 28280, 28511, 28942, 29273, 29639, 30766, 30768, 31811,
    31831, 33622, 34046, 35925, 36140, 36352, 36613, 36619, 36916, 37297,
    37802, 37837, 37845, 37846, 37859, 38015, 38092, 38219, 39226, 39768,
    40026, 40206, 40264, 40443, 40508, 41180, 41257, 41304, 41331, 41655,
    41877, 41881, 41920, 41980, 41993, 42608, 42651, 42660, 42680, 42682,
    42692, 42830, 42909, 42983, 43032, 43033, 43346, 43359, 43576, 43588,
    43640, 44511, 44524, 44678, 44687, 45085, 45133, 45913, 45983, 46926,
    48308, 48699, 49610, 50301, 50565, 51163, 56618, 56817, 57936, 58567,
    58632, 60253, 60571, 60880, 61411, 62873, 62874, 63319
  );

-- Retour arrière :
-- UPDATE contacts_recherche
-- SET C_NumeroMandat = 0
-- WHERE C_NumeroMandat IS NULL
--   AND C_RefContact IN ( ... même liste ... );
