-- Demo API only. Run as MariaDB admin on the VPS (localhost).
-- Same save rights as local API (cursor_client): SELECT / INSERT / UPDATE / CREATE.
-- Does NOT GRANT DROP / ALTER / TRUNCATE. Does NOT touch cursor_client or prod root.
-- Table DELETE cannot encode a WHERE; the API still accepts only PC_RefContact = <id>.
-- Replace CHANGE_ME before running. Never commit the real password.

CREATE USER IF NOT EXISTS 'pr_api_demo'@'localhost' IDENTIFIED BY 'CHANGE_ME';
CREATE USER IF NOT EXISTS 'pr_api_demo'@'127.0.0.1' IDENTIFIED BY 'CHANGE_ME';

GRANT SELECT, INSERT, UPDATE, CREATE ON `perle-rareinfo`.*
  TO 'pr_api_demo'@'localhost', 'pr_api_demo'@'127.0.0.1';

GRANT DELETE ON `perle-rareinfo`.`property_contact`
  TO 'pr_api_demo'@'localhost', 'pr_api_demo'@'127.0.0.1';

FLUSH PRIVILEGES;

-- Verify:
-- SHOW GRANTS FOR 'pr_api_demo'@'localhost';
-- Expect SELECT, INSERT, UPDATE, CREATE on perle-rareinfo.*
-- and DELETE on perle-rareinfo.property_contact only.
