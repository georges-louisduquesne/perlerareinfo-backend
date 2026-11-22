-- Retour arrière du rattrapage Lot 2 (2026-11-22) : liens de doublons multi-agences (P_DoublonPropertyId).
-- Restaure exactement P_DoublonPropertyId depuis backup_property_20261122_yanport_doublons.
-- Ne supprime pas la table de sauvegarde (aucun DROP/TRUNCATE) et ne modifie ni P_State, ni P_DateFin, ni P_DateLastUpdate.

START TRANSACTION;

UPDATE property p
INNER JOIN backup_property_20261122_yanport_doublons b ON b.P_PropertyId = p.P_PropertyId
SET p.P_DoublonPropertyId = b.old_doublon_id
WHERE b.captured = 1;

SELECT ROW_COUNT() AS restored_rows;

SELECT
  COUNT(*) AS actifs_avec_doublon_apres_rollback
FROM property
WHERE P_State = 1
  AND (P_DateFin IS NULL OR P_DateFin < '1000-01-01')
  AND P_DoublonPropertyId IS NOT NULL
  AND P_DoublonPropertyId <> '';

COMMIT;
