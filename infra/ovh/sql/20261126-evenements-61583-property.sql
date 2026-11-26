-- Complète E_PropertyId sur les évènements actifs du contact 61583
-- et du bien 26616 (24 Rue Berthe, 75018) qui sont encore vides.
-- Valeur reprise des évènements 512551 et 515251, déjà identique :
-- d0b9da10-b1f9-11f1-9151-259e3b911188.
-- Inventaire du 2026-11-26 : 500323, 503048, 503599 (chaîne vide),
-- 503805, 514308. Aucune fiche property_contact pour ce contact et ce
-- property : le trigger evenements_before_update laisse E_PC_Id NULL.
-- E_RefInterD = 4974 et I_RefGroupe = 75, déjà égal à E_RefInterI.
-- À exécuter uniquement sur ordre écrit. Ne pas relancer si
-- evenements_bak_property_61583_20261126 existe déjà.
-- Retour arrière : 20261126-evenements-61583-property-rollback.sql

CREATE TABLE evenements_bak_property_61583_20261126 AS
SELECT E_RefEvenement, E_PC_Id, E_PropertyId, E_RefInterI
  FROM evenements
 WHERE E_RefEvenement IN (500323, 503048, 503599, 503805, 514308)
   AND E_RefContact = 61583
   AND E_RefBien = 26616
   AND (E_PropertyId IS NULL OR E_PropertyId = '');

ALTER TABLE evenements_bak_property_61583_20261126
  ADD PRIMARY KEY (E_RefEvenement);

UPDATE evenements
   SET E_PropertyId = 'd0b9da10-b1f9-11f1-9151-259e3b911188'
 WHERE E_RefEvenement IN (500323, 503048, 503599, 503805, 514308)
   AND E_RefContact = 61583
   AND E_RefBien = 26616
   AND (E_PropertyId IS NULL OR E_PropertyId = '');

SELECT E_RefEvenement, E_TypeEvenement, E_PC_Id, E_PropertyId, E_RefInterI
  FROM evenements
 WHERE E_RefEvenement IN (500323, 503048, 503599, 503805, 514308, 512551, 515251)
 ORDER BY E_RefEvenement;
