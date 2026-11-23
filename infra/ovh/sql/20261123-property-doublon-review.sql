-- Lot 3 : table des decisions Admin de revue des doublons suspects (doublon confirme / pas doublon).
-- A executer UNIQUEMENT sur ordre ecrit explicite de l utilisateur.
-- Aucun impact sur les tables existantes. Retour arriere : 20261123-property-doublon-review-rollback.sql

-- Test a blanc (SELECT uniquement) :
-- SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'property_doublon_review';

CREATE TABLE IF NOT EXISTS property_doublon_review (
  PDR_PropertyIdA VARCHAR(40) NOT NULL,
  PDR_PropertyIdB VARCHAR(40) NOT NULL,
  PDR_Decision VARCHAR(20) NOT NULL,
  PDR_Reason VARCHAR(120) NULL,
  PDR_ReviewedBy VARCHAR(100) NULL,
  PDR_ReviewedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (PDR_PropertyIdA, PDR_PropertyIdB),
  INDEX idx_pdr_decision (PDR_Decision)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3;

SELECT TABLE_NAME, TABLE_ROWS
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'property_doublon_review';
