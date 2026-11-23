-- Retour arrière de C_FraisPercus vers int(11).
-- À n’exécuter que s’il n’y a plus de centimes, sinon l’ALTER les tronque.
--
--   SELECT COUNT(*) FROM contacts_recherche
--    WHERE C_FraisPercus IS NOT NULL
--      AND C_FraisPercus <> TRUNCATE(C_FraisPercus, 0);
--   -- Doit être 0 avant de continuer.
--
-- Les valeurs d’origine restent dans contacts_recherche_fraispercus_bak_20261123
-- (C_RefContact, C_FraisPercus). Ne pas supprimer cette table.

ALTER TABLE contacts_recherche
  MODIFY COLUMN C_FraisPercus INT(11) NULL DEFAULT NULL;
