-- Remet la note du badge Google du pied de page à 4.7.
-- Restaure l'option sauvegardée par 20261124-footer-note-google-48.sql.

UPDATE kdg_options AS o
  JOIN kdg_options_bak_20261124_google_note AS b
    ON b.option_id = o.option_id
   SET o.option_value = b.option_value
 WHERE o.option_name = 'trustindex-google-page-details';
