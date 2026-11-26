-- Retour arrière du rattrapage E_PropertyId du 2026-11-26 (141 évènements).
-- Nécessite evenements_bak_property_vides_20261126. Ne la supprime pas.
-- Quand E_PropertyId redevient NULL, vide ou le texte « null », le trigger
-- evenements_before_update remet E_PC_Id à NULL s'il ne trouve pas de fiche.
-- Si E_RefInterD est renseigné, le même trigger réécrit E_RefInterI avec
-- I_RefGroupe : les sept lignes qui étaient NULL (423677, 424328, 424413,
-- 424414, 479289, 483823, 490453) gardent le groupe après ce retour arrière.
-- À exécuter uniquement sur ordre écrit.

UPDATE evenements e
  JOIN evenements_bak_property_vides_20261126 b
    ON b.E_RefEvenement = e.E_RefEvenement
   SET e.E_PropertyId = b.E_PropertyId,
       e.E_RefInterI = b.E_RefInterI
 WHERE e.E_RefContact = b.E_RefContact
   AND e.E_RefBien = b.E_RefBien;

SELECT e.E_RefEvenement, e.E_PC_Id, e.E_PropertyId, e.E_RefInterI,
       b.E_PropertyId AS property_avant, b.E_RefInterI AS inter_i_avant
  FROM evenements e
  JOIN evenements_bak_property_vides_20261126 b
    ON b.E_RefEvenement = e.E_RefEvenement
 WHERE NOT (e.E_PropertyId <=> b.E_PropertyId)
    OR e.E_PC_Id IS NOT NULL;
