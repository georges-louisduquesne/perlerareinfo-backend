-- Retire les index de GET Biens/NonVus. Aucune donnée à restaurer.
ALTER TABLE property_contact DROP INDEX IX_property_contact_non_vus;
ALTER TABLE contacts_recherche DROP INDEX IX_contacts_recherche_conseiller2;
ALTER TABLE contacts_recherche DROP INDEX IX_contacts_recherche_conseiller;
