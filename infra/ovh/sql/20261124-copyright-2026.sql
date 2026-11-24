-- Passe le copyright du pied de page perle-rare.com de 2025 à 2026.
-- Base : perle-rarecom-v2 (WordPress). Une seule option, widget_text
-- (id 79) : le widget français text-4 et le widget anglais text-14.
-- « 2025 » et « 2026 » ont la même longueur : le PHP sérialisé reste valide.
-- Un second passage ne change plus rien (plus aucune occurrence de 2025).
--
-- Contrôle avant :
--   SELECT option_id, option_name,
--          (CHAR_LENGTH(option_value) - CHAR_LENGTH(REPLACE(option_value, 'Copyright 2025', '')))
--            / CHAR_LENGTH('Copyright 2025') AS nb
--     FROM kdg_options
--    WHERE option_name = 'widget_text';
--   -- attendu : option_id 79, nb = 2
--
-- Retour arrière : 20261124-copyright-2026-rollback.sql

CREATE TABLE IF NOT EXISTS kdg_options_bak_20261124_copyright
  AS SELECT option_id, option_name, option_value
       FROM kdg_options
      WHERE option_name = 'widget_text'
        AND option_value LIKE '%Copyright 2025%';

UPDATE kdg_options
   SET option_value = REPLACE(option_value, 'Copyright 2025', 'Copyright 2026')
 WHERE option_name = 'widget_text'
   AND option_value LIKE '%Copyright 2025%';

-- Contrôle après :
--   SELECT (CHAR_LENGTH(option_value) - CHAR_LENGTH(REPLACE(option_value, 'Copyright 2026', '')))
--            / CHAR_LENGTH('Copyright 2026') AS nb_2026,
--          (CHAR_LENGTH(option_value) - CHAR_LENGTH(REPLACE(option_value, 'Copyright 2025', '')))
--            / CHAR_LENGTH('Copyright 2025') AS nb_2025
--     FROM kdg_options
--    WHERE option_name = 'widget_text';
--   -- attendu : nb_2026 = 2, nb_2025 = 0
