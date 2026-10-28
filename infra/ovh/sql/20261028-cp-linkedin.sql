-- Ajoute conseillers_personnels.CP_LinkedIn : lien de page LinkedIn, vide par défaut.
-- Aucune valeur existante n’est réécrite. Les triggers (ville, adresse, téléphone) ne lisent pas cette colonne.
-- Jeu de caractères de la table : latin1 / latin1_swedish_ci. Les URL LinkedIn sont en ASCII.
--
-- Contrôle avant :
--   SELECT COUNT(*) FROM information_schema.COLUMNS
--    WHERE TABLE_SCHEMA = 'perle-rareinfo'
--      AND TABLE_NAME = 'conseillers_personnels'
--      AND COLUMN_NAME = 'CP_LinkedIn';
--   -- attendu : 0
--   SELECT COUNT(*) AS rows_before FROM conseillers_personnels;
--
-- Appliqué le 2026-10-28 sur MariaDB perle-rareinfo si le contrôle avant est à 0.

ALTER TABLE conseillers_personnels
  ADD COLUMN CP_LinkedIn VARCHAR(100)
    CHARACTER SET latin1
    COLLATE latin1_swedish_ci
    NULL
    DEFAULT NULL,
  ALGORITHM=INSTANT;

-- Contrôle après :
--   SHOW COLUMNS FROM conseillers_personnels LIKE 'CP_LinkedIn';
--   -- Type varchar(100), Null YES, Default NULL
--   SELECT COUNT(*) AS rows_after,
--          SUM(CP_LinkedIn IS NOT NULL) AS filled
--     FROM conseillers_personnels;
--   -- rows_after = rows_before, filled = 0
--
-- Retour arrière, seulement tant que la colonne est encore entièrement vide
-- (filled = 0). Ne pas l’exécuter sinon : ce serait perdre des liens saisis.
--   ALTER TABLE conseillers_personnels DROP COLUMN CP_LinkedIn;
