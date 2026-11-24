-- Passe la note du badge Google du pied de page perle-rare.com de 4.7 à 4.8.
-- Base : perle-rarecom-v2 (WordPress).
-- Le pied de page FR et EN contient le shortcode [trustindex no-registration=google].
-- Le plugin lit la note dans l'option trustindex-google-page-details
-- (champ sérialisé rating_score). « 4.7 » et « 4.8 » ont la même longueur :
-- le PHP sérialisé reste valide. Un second passage ne change plus rien.
--
-- Contrôle avant :
--   SELECT option_name,
--          (CHAR_LENGTH(option_value) - CHAR_LENGTH(REPLACE(option_value, 's:12:"rating_score";s:3:"4.7"', '')))
--            / CHAR_LENGTH('s:12:"rating_score";s:3:"4.7"') AS nb
--     FROM kdg_options
--    WHERE option_name = 'trustindex-google-page-details';
--   -- attendu : nb = 1
--
-- Retour arrière : 20261124-footer-note-google-48-rollback.sql

CREATE TABLE IF NOT EXISTS kdg_options_bak_20261124_google_note
  AS SELECT option_id, option_name, option_value
       FROM kdg_options
      WHERE option_name = 'trustindex-google-page-details'
        AND option_value LIKE '%s:12:"rating_score";s:3:"4.7"%';

UPDATE kdg_options
   SET option_value = REPLACE(option_value, 's:12:"rating_score";s:3:"4.7"', 's:12:"rating_score";s:3:"4.8"')
 WHERE option_name = 'trustindex-google-page-details'
   AND option_value LIKE '%s:12:"rating_score";s:3:"4.7"%';

-- Contrôle après :
--   SELECT (CHAR_LENGTH(option_value) - CHAR_LENGTH(REPLACE(option_value, 's:12:"rating_score";s:3:"4.8"', '')))
--            / CHAR_LENGTH('s:12:"rating_score";s:3:"4.8"') AS nb_48,
--          (CHAR_LENGTH(option_value) - CHAR_LENGTH(REPLACE(option_value, 's:12:"rating_score";s:3:"4.7"', '')))
--            / CHAR_LENGTH('s:12:"rating_score";s:3:"4.7"') AS nb_47
--     FROM kdg_options
--    WHERE option_name = 'trustindex-google-page-details';
--   -- attendu : nb_48 = 1, nb_47 = 0
