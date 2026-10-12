-- Logo Terrasses et Jardins (réseau 197), demandé le 12 oct. 2026.
-- Valeur précédente (SELECT) : I2_Icon = ''.
-- Le fichier est /var/www/perle-rare/web/logos/reseaux/197.png
-- (copie du PNG https://www.terrasses-jardins.com/wp-content/themes/terrasses10/assets/images/logo-terrasses-jardins.png).

UPDATE intermediaires_indirects
SET I2_Icon = 'logos/reseaux/197.png'
WHERE I2_RefInterm_indirect = 197
  AND I2_Icon = '';

-- Retour arrière :
-- UPDATE intermediaires_indirects
-- SET I2_Icon = ''
-- WHERE I2_RefInterm_indirect = 197
--   AND I2_Icon = 'logos/reseaux/197.png';
-- puis supprimer logos/reseaux/197.png
