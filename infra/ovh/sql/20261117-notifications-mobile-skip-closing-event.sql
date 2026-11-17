-- Coupe les notifications mobiles « nouveau bien » et « modif prix »
-- dès qu’un RV COMPROMIS ou un RV ACTE AUTHENT. est actif.
-- L’API qui envoie les mobiles ne lit que N_State = -3. L’état 3 veut déjà
-- dire « envoyée ». L’état 4 (libre au 2026-11-17) veut dire « non envoyée ».
-- Contrôle avant application :
--   SELECT N_State, COUNT(*) FROM notifications GROUP BY N_State;
--   SELECT n.N_Id FROM notifications n
--   WHERE n.N_State IN (-2, -3)
--     AND EXISTS (
--       SELECT 1 FROM evenements e
--       WHERE e.E_RefContact = n.N_RefContact
--         AND e.E_Statut = 1
--         AND e.E_TypeEvenement IN ('RV COMPROMIS', 'RV ACTE AUTHENT.', 'RV ACTE AUTHENT')
--     );
-- Le 2026-11-17 : aucun état 4, aucune ligne -2/-3 en attente avec un RV bloquant.

DROP TRIGGER IF EXISTS notifications_bi_skip_closing_rv;

DELIMITER $$

CREATE TRIGGER notifications_bi_skip_closing_rv
BEFORE INSERT ON notifications
FOR EACH ROW
BEGIN
  IF NEW.N_State IN (-2, -3)
     AND NEW.N_Type IN ('NOUVEAU BIEN', 'MODIF PRIX')
     AND EXISTS (
       SELECT 1
       FROM evenements e
       WHERE e.E_RefContact = NEW.N_RefContact
         AND e.E_Statut = 1
         AND e.E_TypeEvenement IN ('RV COMPROMIS', 'RV ACTE AUTHENT.', 'RV ACTE AUTHENT')
     )
  THEN
    SET NEW.N_State = 4;
  END IF;
END$$

DELIMITER ;
