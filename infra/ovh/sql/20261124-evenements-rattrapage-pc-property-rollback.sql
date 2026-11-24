-- Retour arrière du rattrapage E_PropertyId / E_PC_Id du 2026-11-24.
-- Nécessite evenements_bak_pc_property_20261124, créée par
-- 20261124-evenements-rattrapage-pc-property.sql. Ne la supprime pas.
-- Restaure E_PropertyId et E_RefInterI. Quand E_PropertyId redevient NULL,
-- le trigger evenements_before_update remet E_PC_Id à NULL.
-- Si E_RefInterD est renseigné, le même trigger réécrit E_RefInterI avec
-- I_RefGroupe : la valeur restaurée ne tient que si elle est déjà égale.
-- À exécuter uniquement sur ordre écrit, avec le client mysql, sans --force.

DROP PROCEDURE IF EXISTS pr_assert_rollback_pc_property_20261124;

DELIMITER $$
CREATE PROCEDURE pr_assert_rollback_pc_property_20261124()
BEGIN
  DECLARE n INT DEFAULT 0;
  SELECT COUNT(*) INTO n
    FROM evenements_bak_pc_property_20261124 b
    JOIN evenements e ON e.E_RefEvenement = b.E_RefEvenement
   WHERE NOT (e.E_PropertyId <=> b.E_PropertyId)
      OR (
           (b.E_PropertyId IS NULL OR b.E_PropertyId = '')
           AND e.E_PC_Id IS NOT NULL
           AND e.E_PC_Id <> 0
         );
  IF n > 0 THEN
    ROLLBACK;
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'Controle retour arriere en echec : transaction annulee';
  END IF;
END$$
DELIMITER ;

START TRANSACTION;

UPDATE evenements e
  JOIN evenements_bak_pc_property_20261124 b
    ON b.E_RefEvenement = e.E_RefEvenement
   SET e.E_PropertyId = b.E_PropertyId,
       e.E_RefInterI = b.E_RefInterI;

-- Doit être vide. Sinon la procédure annule la transaction.
SELECT e.E_RefEvenement, e.E_PC_Id, e.E_PropertyId,
       b.E_PC_Id AS pc_avant, b.E_PropertyId AS property_avant
  FROM evenements e
  JOIN evenements_bak_pc_property_20261124 b
    ON b.E_RefEvenement = e.E_RefEvenement
 WHERE NOT (e.E_PropertyId <=> b.E_PropertyId)
    OR (
         (b.E_PropertyId IS NULL OR b.E_PropertyId = '')
         AND e.E_PC_Id IS NOT NULL
         AND e.E_PC_Id <> 0
       );

-- E_RefInterI non restauré (le trigger l'a réaligné sur I_RefGroupe). Informatif.
SELECT e.E_RefEvenement, e.E_RefInterI, b.E_RefInterI AS ref_inter_i_avant
  FROM evenements e
  JOIN evenements_bak_pc_property_20261124 b
    ON b.E_RefEvenement = e.E_RefEvenement
 WHERE NOT (e.E_RefInterI <=> b.E_RefInterI);

CALL pr_assert_rollback_pc_property_20261124();

COMMIT;

DROP PROCEDURE IF EXISTS pr_assert_rollback_pc_property_20261124;
