-- Retour arriere Lot 3 (sans DROP/TRUNCATE destructif) :
-- Les decisions enregistrees dans property_doublon_review peuvent etre neutralisees sans supprimer la table :
-- Pour verifier avant action :
SELECT PDR_Decision, COUNT(*) AS nb FROM property_doublon_review GROUP BY PDR_Decision;

-- Si l'utilisateur demande explicitement de vider les decisions :
-- DELETE FROM property_doublon_review WHERE PDR_Decision IN ('confirmed', 'rejected');
