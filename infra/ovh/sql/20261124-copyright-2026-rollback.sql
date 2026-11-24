-- Remet le copyright du pied de page perle-rare.com à 2025.
-- Base : perle-rarecom-v2. Restaure l'option widget_text sauvegardée
-- par 20261124-copyright-2026.sql.

UPDATE kdg_options AS actuel
  JOIN kdg_options_bak_20261124_copyright AS sauve
    ON actuel.option_id = sauve.option_id
   AND actuel.option_name = sauve.option_name
   SET actuel.option_value = sauve.option_value
 WHERE actuel.option_name = 'widget_text';
