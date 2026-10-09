-- Agence 6236 (Breteuil Montparnasse) : I_IdYanport = 5477959295355904
-- saisi après la migration qui copiait ce champ vers intermediaires_directs_yanport.
-- L'import ne lit que cette table, donc les 49 biens publiés (P_State=1) qui portent
-- ce dealer ne sont pas dans rel_property_inter_direct.
-- Inventaire 2026-10-09 (SELECT) : 0 ligne yanport, 49 biens à rattacher, 0 déjà liés.
-- Appliqué le 2026-10-09 : INSERT yanport (1) + rel_property_inter_direct (49).
-- Contrôle : 49 biens publiés rattachés, les 8 liens déjà dépubliés inchangés.
-- CREATE TABLE de sauvegarde refusé (rôle SQL sans DDL). Rien n'a été écrasé :
-- le retour arrière est le DELETE commenté en bas, pas une restauration de lignes.
-- Idempotent : une seconde exécution n'insère plus rien.
-- À exécuter uniquement sur ordre écrit (écriture sur la base prod partagée).
-- Le job AgencyYanportLinkJob reprend ce même rattachement aux prochaines saisies.

CREATE TABLE IF NOT EXISTS intermediaires_directs_yanport_bak_20261009_6236 AS
SELECT *
FROM intermediaires_directs_yanport
WHERE I_RefIntermediaire = 6236;

CREATE TABLE IF NOT EXISTS rel_property_inter_direct_bak_20261009_6236 AS
SELECT r.*
FROM rel_property_inter_direct r
INNER JOIN property p ON p.P_PropertyId = r.P_PropertyId
WHERE p.P_State = 1
  AND (
    p.P_Annonceurs LIKE '%"Id":5477959295355904,%'
    OR p.P_Annonceurs LIKE '%"Id":5477959295355904}%'
    OR p.P_Annonceurs LIKE '%"Id": 5477959295355904,%'
    OR p.P_Annonceurs LIKE '%"Id": 5477959295355904}%'
  );

START TRANSACTION;

INSERT IGNORE INTO intermediaires_directs_yanport (I_RefIntermediaire, I_YanportId, I_Source)
VALUES (6236, 5477959295355904, 'saisi');

INSERT INTO rel_property_inter_direct (P_PropertyId, I_RefIntermediaire)
SELECT p.P_PropertyId, 6236
FROM property p
WHERE p.P_State = 1
  AND (
    p.P_Annonceurs LIKE '%"Id":5477959295355904,%'
    OR p.P_Annonceurs LIKE '%"Id":5477959295355904}%'
    OR p.P_Annonceurs LIKE '%"Id": 5477959295355904,%'
    OR p.P_Annonceurs LIKE '%"Id": 5477959295355904}%'
  )
  AND NOT EXISTS (
    SELECT 1
    FROM rel_property_inter_direct r
    WHERE r.P_PropertyId = p.P_PropertyId
      AND r.I_RefIntermediaire = 6236
  );

COMMIT;

-- Contrôle : 1 ligne yanport source saisi, 49 biens publiés rattachés.
-- SELECT I_YanportId, I_Source FROM intermediaires_directs_yanport WHERE I_RefIntermediaire = 6236;
-- SELECT COUNT(*) FROM rel_property_inter_direct r
-- INNER JOIN property p ON p.P_PropertyId = r.P_PropertyId
-- WHERE r.I_RefIntermediaire = 6236 AND p.P_State = 1
--   AND (p.P_Annonceurs LIKE '%"Id":5477959295355904,%' OR p.P_Annonceurs LIKE '%"Id":5477959295355904}%');

-- Retour arrière :
-- DELETE FROM rel_property_inter_direct
-- WHERE I_RefIntermediaire = 6236
--   AND P_PropertyId IN (
--     SELECT P_PropertyId FROM (
--       SELECT p.P_PropertyId
--       FROM property p
--       WHERE p.P_State = 1
--         AND (p.P_Annonceurs LIKE '%"Id":5477959295355904,%' OR p.P_Annonceurs LIKE '%"Id":5477959295355904}%'
--           OR p.P_Annonceurs LIKE '%"Id": 5477959295355904,%' OR p.P_Annonceurs LIKE '%"Id": 5477959295355904}%')
--     ) ids
--   )
--   AND (P_PropertyId, I_RefIntermediaire) NOT IN (
--     SELECT P_PropertyId, I_RefIntermediaire FROM rel_property_inter_direct_bak_20261009_6236
--   );
-- DELETE FROM intermediaires_directs_yanport
-- WHERE I_RefIntermediaire = 6236 AND I_YanportId = 5477959295355904 AND I_Source = 'saisi'
--   AND NOT EXISTS (
--     SELECT 1 FROM intermediaires_directs_yanport_bak_20261009_6236 b
--     WHERE b.I_RefIntermediaire = 6236 AND b.I_YanportId = 5477959295355904
--   );
