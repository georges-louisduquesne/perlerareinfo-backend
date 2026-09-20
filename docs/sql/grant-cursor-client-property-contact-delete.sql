-- Local API (cursor_client) : DELETE only on property_contact, for métamoteur
-- « Supprimer tous les résultats ». Does NOT GRANT DROP/ALTER/TRUNCATE.
-- Does NOT change pr_api_demo or prod root. Run as MariaDB admin on the VPS.

GRANT DELETE ON `perle-rareinfo`.`property_contact`
  TO 'cursor_client'@'%';

FLUSH PRIVILEGES;

-- Verify (expect DELETE on perle-rareinfo.property_contact):
-- SHOW GRANTS FOR 'cursor_client'@'%';
