-- Annuaire interne : une seule liste de partenaires, visible par chaque utilisateur connecté.
-- L'API v2 crée la même table au premier appel (CREATE TABLE IF NOT EXISTS).
-- Pas de DELETE : l'enregistrement remplace le document JSON (INSERT ... ON DUPLICATE KEY UPDATE).

CREATE TABLE IF NOT EXISTS annuaire_partenaires_document (
  APD_Id TINYINT NOT NULL,
  APD_Json MEDIUMTEXT NOT NULL,
  APD_UpdatedAt DATETIME NOT NULL,
  PRIMARY KEY (APD_Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
