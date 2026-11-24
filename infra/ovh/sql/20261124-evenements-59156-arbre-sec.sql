-- Rattache les évènements du contact 59156 sur le bien 26135
-- (46 Rue de l'Arbre Sec, 75001) au property déjà porté par l'offre acceptée 494402.
-- Aucune fiche property_contact n'existait pour ce contact : le trigger laissait E_PC_Id NULL.
-- À exécuter uniquement sur ordre écrit.
-- Retour arrière : 20261124-evenements-59156-arbre-sec-rollback.sql

CREATE TABLE evenements_bak_59156_arbre_sec_20261124 AS
SELECT E_RefEvenement, E_PC_Id, E_PropertyId, E_RefInterI
  FROM evenements
 WHERE E_RefEvenement IN (491100, 491471, 492018, 492019, 492500, 494402);

INSERT INTO property_contact (PC_PropertyId, PC_RefContact, PC_Actif, PC_Rate, PC_Vu)
SELECT '7f92ae10-4a7e-11f1-ace9-797ad7f35151', 59156, 1, 0, 0
  FROM DUAL
 WHERE NOT EXISTS (
       SELECT 1
         FROM property_contact
        WHERE PC_PropertyId = '7f92ae10-4a7e-11f1-ace9-797ad7f35151'
          AND PC_RefContact = 59156
 );

UPDATE evenements
   SET E_PropertyId = '7f92ae10-4a7e-11f1-ace9-797ad7f35151'
 WHERE E_RefEvenement IN (491100, 491471, 492018, 492019, 492500, 494402)
   AND E_RefContact = 59156
   AND E_RefBien = 26135;

SELECT e.E_RefEvenement, e.E_TypeEvenement, e.E_PC_Id, e.E_PropertyId, e.E_RefInterI
  FROM evenements e
 WHERE e.E_RefEvenement IN (491100, 491471, 492018, 492019, 492500, 494402)
 ORDER BY e.E_RefEvenement;

SELECT PC_Id, PC_RefContact, PC_PropertyId, PC_Actif
  FROM property_contact
 WHERE PC_RefContact = 59156
   AND PC_PropertyId = '7f92ae10-4a7e-11f1-ace9-797ad7f35151';
