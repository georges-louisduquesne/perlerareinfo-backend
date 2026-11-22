-- Retour arrière du rattrapage Lot 1 (2026-11-22) : biens Yanport 404 (> 48 h) ou fermés côté Yanport.
-- Restaure exactement (P_State, P_DateFin, P_EstRattrapage) depuis backup_property_20261122_yanport_ghosts.
-- Ne supprime pas la table de sauvegarde (aucun DROP/TRUNCATE) et ne modifie pas P_DateLastUpdate.

START TRANSACTION;

UPDATE property p
INNER JOIN backup_property_20261122_yanport_ghosts b ON b.P_PropertyId = p.P_PropertyId
SET p.P_State = b.old_state,
    p.P_DateFin = b.old_date_fin,
    p.P_EstRattrapage = b.old_rattrapage
WHERE b.old_state = 1
  AND p.P_State = 0
  AND p.P_EstRattrapage = 5;

SELECT ROW_COUNT() AS restored_rows;

SELECT p.P_State, IFNULL(p.P_EstRattrapage, -1) AS rattrapage, COUNT(*) AS n
FROM property p
INNER JOIN backup_property_20261122_yanport_ghosts b ON b.P_PropertyId = p.P_PropertyId
WHERE b.old_state = 1
GROUP BY p.P_State, IFNULL(p.P_EstRattrapage, -1);

COMMIT;
