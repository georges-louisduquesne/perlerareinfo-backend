-- Retour arrière du rattachement 46 Rue de l'Arbre Sec / contact 59156.
-- Supprime d'abord la fiche créée, puis restaure E_PropertyId.
-- Le trigger remet E_PC_Id à NULL quand le property redevient vide,
-- et le recalcule depuis property_contact sinon.
-- E_RefInterI suit I_RefGroupe tant que E_RefInterD est renseigné.

DELETE FROM property_contact
 WHERE PC_RefContact = 59156
   AND PC_PropertyId = '7f92ae10-4a7e-11f1-ace9-797ad7f35151';

UPDATE evenements e
  JOIN evenements_bak_59156_arbre_sec_20261124 b
    ON b.E_RefEvenement = e.E_RefEvenement
   SET e.E_PropertyId = b.E_PropertyId,
       e.E_RefInterI = b.E_RefInterI;

SELECT e.E_RefEvenement, e.E_PC_Id, e.E_PropertyId, e.E_RefInterI
  FROM evenements e
 WHERE e.E_RefEvenement IN (491100, 491471, 492018, 492019, 492500, 494402)
 ORDER BY e.E_RefEvenement;
