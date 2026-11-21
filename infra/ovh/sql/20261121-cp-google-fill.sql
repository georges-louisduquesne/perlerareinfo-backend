-- Alimente CP_Google pour 12 conseillers actifs dont la fiche Google est confirmée.
-- N’écrit que si le champ est encore NULL. Aucune autre ligne n’est modifiée.
-- Les URL sont en ASCII (latin1). Contrôle avant le 2026-11-21 : ces 12 CP_Google sont NULL.
--
-- Retour arrière : 20261121-cp-google-fill-rollback.sql.

UPDATE conseillers_personnels
   SET CP_Google = CASE CP_RefConseiller
     WHEN 81  THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Laurence+Berry,+38+Rue+Dauphine,+Paris'
     WHEN 127 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Constance+Perrin,+4+Rue+Bellanger,+92200+Neuilly-sur-Seine'
     WHEN 162 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+St%C3%A9phane+Trichard,+69+Rue+Sedaine,+75011+Paris'
     WHEN 239 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Corinne+Lelarge,+21+Rue+Verniquet,+75017+Paris'
     WHEN 265 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Anne+Davoli,+99+Rue+Jean-Pierre+Timbaud,+Paris'
     WHEN 274 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Sophie+McCaffry,+55+Rue+du+Cherche-Midi,+75006+Paris'
     WHEN 280 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Cl%C3%A9mence+Kamienny,+54+Rue+Lamartine,+75009+Paris'
     WHEN 290 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Elsa+Pons,+14+Rue+Bidel,+92600+Asni%C3%A8res-sur-Seine'
     WHEN 322 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Laurence+Pinaire,+172+Boulevard+Haussmann,+75008+Paris'
     WHEN 339 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Lionel+Jamet,+38+Rue+Madame,+75006+Paris'
     WHEN 347 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Roxane+Bach%C3%A9,+22+Rue+du+Ch%C3%A2teau+d%27Eau,+75010+Paris'
     WHEN 357 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Isabelle+Chetochine,+85+Avenue+Victor+Hugo,+92100+Boulogne-Billancourt'
   END
 WHERE CP_Google IS NULL
   AND (
        (CP_RefConseiller = 81  AND CP_Prenom = 'Laurence'      AND CP_NomFamille = 'Berry')
     OR (CP_RefConseiller = 127 AND CP_Prenom = 'Constance'     AND CP_NomFamille = 'Perrin')
     OR (CP_RefConseiller = 162 AND CP_Prenom = 'Stéphane'      AND CP_NomFamille = 'Trichard')
     OR (CP_RefConseiller = 239 AND CP_Prenom = 'Corinne Noëlle' AND CP_NomFamille = 'Lelarge')
     OR (CP_RefConseiller = 265 AND CP_Prenom = 'Anne'          AND CP_NomFamille = 'Davoli')
     OR (CP_RefConseiller = 274 AND CP_Prenom = 'Sophie'        AND CP_NomFamille = 'McCaffry')
     OR (CP_RefConseiller = 280 AND CP_Prenom = 'Clémence'      AND CP_NomFamille = 'Kamienny')
     OR (CP_RefConseiller = 290 AND CP_Prenom = 'Elsa'          AND CP_NomFamille = 'Pons')
     OR (CP_RefConseiller = 322 AND CP_Prenom = 'Laurence'      AND CP_NomFamille = 'Pinaire')
     OR (CP_RefConseiller = 339 AND CP_Prenom = 'Lionel'        AND CP_NomFamille = 'Jamet')
     OR (CP_RefConseiller = 347 AND CP_Prenom = 'Roxane'        AND CP_NomFamille = 'Baché')
     OR (CP_RefConseiller = 357 AND CP_Prenom = 'Isabelle'      AND TRIM(CP_NomFamille) = 'Chetochine')
   );

-- Contrôle après :
--   SELECT CP_RefConseiller, CP_Prenom, CP_NomFamille, CP_Google
--     FROM conseillers_personnels
--    WHERE CP_RefConseiller IN (81, 127, 162, 239, 265, 274, 280, 290, 322, 339, 347, 357);
--   -- 12 lignes, aucune CP_Google NULL
--   SELECT COUNT(*) FROM conseillers_personnels WHERE CP_Google IS NOT NULL;
--   -- 12
