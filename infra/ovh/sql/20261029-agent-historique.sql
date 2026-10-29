-- Ajoute contact_intermediaire.C_Historique : texte libre de la fiche Agent,
-- modifiable par les seuls admins (l’API ignore la saisie des autres rôles).
-- NULL = pas de saisie : l’écran affiche alors les lignes d’audit (actor_logs).
-- Jeu de caractères du tableau : utf8 / utf8_general_ci. Colonne nullable, sans défaut.
--
-- Contrôle avant (doit renvoyer 0 ligne) :
--   SELECT COLUMN_NAME
--     FROM INFORMATION_SCHEMA.COLUMNS
--    WHERE TABLE_SCHEMA = DATABASE()
--      AND TABLE_NAME = 'contact_intermediaire'
--      AND COLUMN_NAME = 'C_Historique';

CREATE TABLE IF NOT EXISTS contact_intermediaire_c_historique_bak_20261029 AS
SELECT C_RefCtcInter, CAST(NULL AS CHAR(1)) AS C_Historique
FROM contact_intermediaire
WHERE 1 = 0;

ALTER TABLE contact_intermediaire
  ADD COLUMN C_Historique TEXT
    CHARACTER SET utf8
    COLLATE utf8_general_ci
    NULL
    DEFAULT NULL;

-- Contrôle après :
--   SHOW COLUMNS FROM contact_intermediaire LIKE 'C_Historique';
--   -- Type attendu : text, Null YES
--
-- Retour arrière (la colonne est vide à la création ; ne pas l’exécuter
-- s’il y a déjà des saisies à conserver) :
--   ALTER TABLE contact_intermediaire DROP COLUMN C_Historique;
-- La table contact_intermediaire_c_historique_bak_20261029 reste vide
-- (aucune valeur avant l’ajout). Ne pas la supprimer.
