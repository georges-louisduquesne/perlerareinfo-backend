-- GET Biens/NonVus parcourait property_contact (559k lignes) puis property (5 Go)
-- parce que le OR sur C_NomFamilleConseiller / C_2emeConseiller n'utilisait aucun index.
-- La requête est réécrite en deux égalités. Ces index rendent chaque branche un lookup.
-- Aucune ligne modifiée : le retour arrière est le DROP INDEX du script rollback.
-- Contrôle après création :
--   EXPLAIN SELECT pc.PC_Id
--   FROM contacts_recherche cr
--   INNER JOIN property_contact pc
--     ON pc.PC_RefContact = cr.C_RefContact AND pc.PC_Actif = 1 AND pc.PC_Vu <> 1
--   INNER JOIN property p ON p.P_PropertyId = pc.PC_PropertyId
--   WHERE cr.C_NomFamilleConseiller = 'probe';

ALTER TABLE contacts_recherche
  ADD INDEX IX_contacts_recherche_conseiller (C_NomFamilleConseiller),
  ALGORITHM=INPLACE, LOCK=NONE;

ALTER TABLE contacts_recherche
  ADD INDEX IX_contacts_recherche_conseiller2 (C_2emeConseiller),
  ALGORITHM=INPLACE, LOCK=NONE;

ALTER TABLE property_contact
  ADD INDEX IX_property_contact_non_vus (PC_RefContact, PC_Actif, PC_Vu),
  ALGORITHM=INPLACE, LOCK=NONE;
