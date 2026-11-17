-- Annule la coupe automatique des notifications mobiles sur RV de clôture.
-- Aucune ligne existante n’a été réécrite le 2026-11-17 (file -2/-3 vide).

DROP TRIGGER IF EXISTS notifications_bi_skip_closing_rv;
