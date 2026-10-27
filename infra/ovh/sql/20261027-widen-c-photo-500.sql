-- Élargit contact_intermediaire.C_Photo : varchar(300) → varchar(500).
-- L’URL de la photo agent est stockée telle quelle. Le plus long lien avant
-- l’ALTER fait 210 caractères (0 ligne au-delà de 300).
-- Jeu de caractères conservé : utf8 / utf8_general_ci, NULL, défaut NULL.
--
-- Contrôle avant :
--   SELECT COLUMN_TYPE, IS_NULLABLE, CHARACTER_SET_NAME, COLLATION_NAME
--     FROM INFORMATION_SCHEMA.COLUMNS
--    WHERE TABLE_SCHEMA = 'perle-rareinfo'
--      AND TABLE_NAME = 'contact_intermediaire'
--      AND COLUMN_NAME = 'C_Photo';
--   SELECT MAX(CHAR_LENGTH(C_Photo)) FROM contact_intermediaire;
--
-- Sauvegarde des liens existants (pas de DROP : la table reste pour le retour arrière).

CREATE TABLE IF NOT EXISTS contact_intermediaire_c_photo_bak_20261027 AS
SELECT C_RefCtcInter, C_Photo
FROM contact_intermediaire
WHERE C_Photo IS NOT NULL AND C_Photo <> '';

ALTER TABLE contact_intermediaire
  MODIFY COLUMN C_Photo VARCHAR(500)
    CHARACTER SET utf8
    COLLATE utf8_general_ci
    NULL
    DEFAULT NULL;

-- Contrôle après :
--   SHOW COLUMNS FROM contact_intermediaire LIKE 'C_Photo';
--   -- Type attendu : varchar(500)
--
-- Retour arrière, seulement si aucune photo ne dépasse 300 caractères :
--   SELECT MAX(CHAR_LENGTH(C_Photo)) FROM contact_intermediaire;  -- doit être <= 300
--   ALTER TABLE contact_intermediaire
--     MODIFY COLUMN C_Photo VARCHAR(300)
--       CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL;
-- Les valeurs d’origine restent dans contact_intermediaire_c_photo_bak_20261027
-- (C_RefCtcInter, C_Photo). Ne pas supprimer cette table.
