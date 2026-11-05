-- Accélère GET Taches/Prospects et Taches/Clients (accueil onglets 2 et 3).
-- Avant : scan ~264k lignes taches (T_Etat + T_Date_Realisation sans index).
-- Contrôle :
--   EXPLAIN SELECT t.T_Ref FROM taches t
--   WHERE t.T_Etat = '' AND t.T_Date_Realisation < DATE(NOW()) + INTERVAL 1 DAY LIMIT 5;

CREATE INDEX IX_taches_accueil_echeance ON taches (T_Etat, T_Date_Realisation, T_Ref_Contact);
CREATE INDEX IX_taches_accueil_qui_echeance ON taches (T_Qui, T_Etat, T_Date_Realisation);
