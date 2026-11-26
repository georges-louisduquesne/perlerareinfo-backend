-- Retour arrière du rattrapage E_PropertyId du contact 61583 / bien 26616.
-- Nécessite evenements_bak_property_61583_20261126. Ne la supprime pas.
-- Quand E_PropertyId redevient NULL ou vide, le trigger
-- evenements_before_update remet E_PC_Id à NULL.
-- Si E_RefInterD est renseigné, le même trigger réécrit E_RefInterI
-- avec I_RefGroupe.
-- À exécuter uniquement sur ordre écrit.

UPDATE evenements e
  JOIN evenements_bak_property_61583_20261126 b
    ON b.E_RefEvenement = e.E_RefEvenement
   SET e.E_PropertyId = b.E_PropertyId,
       e.E_RefInterI = b.E_RefInterI
 WHERE e.E_RefContact = 61583
   AND e.E_RefBien = 26616;

SELECT e.E_RefEvenement, e.E_PC_Id, e.E_PropertyId, e.E_RefInterI
  FROM evenements e
 WHERE e.E_RefEvenement IN (500323, 503048, 503599, 503805, 514308)
 ORDER BY e.E_RefEvenement;
