-- Agence 6946 (IMMO DE FRANCE PARIS ILE DE FRANCE).
-- I_IdYanport = 6511093365567488, mapping source « saisi » déjà présent.
-- Le rattachement à la saisie ne prenait que P_State=1. Inventaire 2026-11-06 :
-- 20 biens portent cet id, tous terminés (P_State=0), 0 ligne dans rel_property_inter_direct.
-- Appliqué le 2026-11-06 : 20 lignes insérées en 9 s. Contrôle ensuite : 20 liens, 0 publié.
-- Cette passe les rattache. Idempotente. Aucune ligne existante n'est modifiée.
-- Retour arrière : le DELETE commenté en bas. Avant l'INSERT, l'agence n'avait aucun lien.
-- À exécuter uniquement sur ordre écrit.

-- Contrôle avant écriture :
-- SELECT COUNT(*) FROM rel_property_inter_direct WHERE I_RefIntermediaire = 6946;
-- SELECT p.P_State, COUNT(*) FROM property p
-- WHERE p.P_Annonceurs LIKE '%"Id":6511093365567488,%'
--    OR p.P_Annonceurs LIKE '%"Id":6511093365567488}%'
--    OR p.P_Annonceurs LIKE '%"Id": 6511093365567488,%'
--    OR p.P_Annonceurs LIKE '%"Id": 6511093365567488}%'
-- GROUP BY p.P_State;

INSERT INTO rel_property_inter_direct (P_PropertyId, I_RefIntermediaire)
SELECT p.P_PropertyId, 6946
FROM property p
WHERE (
    p.P_Annonceurs LIKE '%"Id":6511093365567488,%'
    OR p.P_Annonceurs LIKE '%"Id":6511093365567488}%'
    OR p.P_Annonceurs LIKE '%"Id": 6511093365567488,%'
    OR p.P_Annonceurs LIKE '%"Id": 6511093365567488}%'
  )
  AND NOT EXISTS (
    SELECT 1
    FROM rel_property_inter_direct r
    WHERE r.P_PropertyId = p.P_PropertyId
      AND r.I_RefIntermediaire = 6946
  );

-- Contrôle : 20 liens, dont 0 publié.
-- SELECT COUNT(*) FROM rel_property_inter_direct WHERE I_RefIntermediaire = 6946;

-- Retour arrière :
-- DELETE r FROM rel_property_inter_direct r
-- INNER JOIN property p ON p.P_PropertyId = r.P_PropertyId
-- WHERE r.I_RefIntermediaire = 6946
--   AND (
--     p.P_Annonceurs LIKE '%"Id":6511093365567488,%'
--     OR p.P_Annonceurs LIKE '%"Id":6511093365567488}%'
--     OR p.P_Annonceurs LIKE '%"Id": 6511093365567488,%'
--     OR p.P_Annonceurs LIKE '%"Id": 6511093365567488}%'
--   );
