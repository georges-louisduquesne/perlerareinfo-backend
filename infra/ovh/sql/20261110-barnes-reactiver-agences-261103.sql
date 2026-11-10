-- Annule la désactivation Barnes du 3 novembre 2026 (actor_search 170,
-- barnes-agences-scraper). Le scrape était incomplet : 0 ajout, 0 modification,
-- 24 agences et 155 contacts éteints. Ces bureaux sont toujours sur
-- https://www.barnes-paris.com/fr/les-bureaux/
--
-- Le compte SQL ne peut pas créer de table de copie (SCHEMA_DDL_PERMISSIONS).
-- Avant : I_Actif = 0 et I_Commentaires = 'désactivée 261103' pour les 26 agences.
-- Avant : C_Statut = 'Inactif' pour les 46 contacts encore inactifs du run 170.
-- Aucun de ces contacts n'a été redésactivé par un run ultérieur.
-- BARNES PARIS EST (5733) était déjà réactivée : hors de cette liste.
--
-- À blanc le 10 novembre 2026 : 26 agences, 46 contacts.
-- Appliqué le 10 novembre 2026 : 26 agences réactivées, 46 contacts repassés Actif.

UPDATE intermediaires_directs
SET I_Actif = 1,
    I_Commentaires = 'réactivée 261110 (annule désactivation 261103)'
WHERE I_Actif = 0
  AND TRIM(I_Commentaires) = 'désactivée 261103'
  AND I_RefIntermediaire IN (
    4067, 4506, 4832, 5483, 5516, 5757, 5916, 5917, 6098, 6167,
    6168, 6170, 6171, 6172, 6174, 6175, 6176, 6177, 6178, 6179,
    6180, 6182, 6183, 6310, 6657, 6848
  );

UPDATE contact_intermediaire
SET C_Statut = 'Actif'
WHERE C_Statut = 'Inactif'
  AND C_RefCtcInter IN (
    4459, 9488, 10245, 10324, 10384, 10766, 11296, 11361, 11522, 11807,
    11814, 12108, 12241, 12244, 12558, 12942, 12973, 13016, 13064, 13088,
    13400, 13427, 13429, 13460, 13534, 13556, 14206, 14207, 14212, 14213,
    14214, 14218, 14219, 14221, 14229, 14233, 14248, 14252, 14258, 14271,
    14285, 14294, 14298, 14304, 14632, 15698
  );

-- Retour arrière :
-- UPDATE intermediaires_directs
-- SET I_Actif = 0, I_Commentaires = 'désactivée 261103'
-- WHERE I_Commentaires = 'réactivée 261110 (annule désactivation 261103)'
--   AND I_RefIntermediaire IN ( 4067, 4506, ... même liste ... );
-- UPDATE contact_intermediaire
-- SET C_Statut = 'Inactif'
-- WHERE C_Statut = 'Actif'
--   AND C_RefCtcInter IN ( 4459, 9488, ... même liste ... );
