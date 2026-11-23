-- C_FraisPercus est un int(11) : MariaDB tronque les centimes du Montant PS.
-- C_MttHono est déjà decimal(10,2). On aligne C_FraisPercus.
-- 2 059 montants non nuls, aucun centime possible tant que la colonne est un entier.
--
-- Contrôle avant :
--   SELECT COLUMN_TYPE, IS_NULLABLE, COLUMN_DEFAULT
--     FROM INFORMATION_SCHEMA.COLUMNS
--    WHERE TABLE_SCHEMA = 'perle-rareinfo'
--      AND TABLE_NAME = 'contacts_recherche'
--      AND COLUMN_NAME = 'C_FraisPercus';
--   -- Attendu avant : int(11), YES, NULL
--
-- Sauvegarde des valeurs existantes (pas de DROP : la table reste pour le retour arrière).

CREATE TABLE IF NOT EXISTS contacts_recherche_fraispercus_bak_20261123 AS
SELECT C_RefContact, C_FraisPercus
FROM contacts_recherche
WHERE C_FraisPercus IS NOT NULL;

ALTER TABLE contacts_recherche
  MODIFY COLUMN C_FraisPercus DECIMAL(10,2) NULL DEFAULT NULL;

-- Contrôle après :
--   SHOW COLUMNS FROM contacts_recherche LIKE 'C_FraisPercus';
--   -- Type attendu : decimal(10,2)
