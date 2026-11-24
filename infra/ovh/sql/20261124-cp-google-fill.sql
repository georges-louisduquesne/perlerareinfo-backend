-- Alimente CP_Google pour 16 conseillers actifs dont la fiche Google est confirmée.
-- N’écrit que si le champ est encore NULL ou vide. Aucune autre ligne n’est modifiée.
-- Les URL sont en ASCII (latin1) et ouvrent la fiche via query_place_id.
-- Contrôle avant le 2026-11-24 : 15 CP_Google NULL, Charles Champin (191) chaîne vide.
--
-- Retour arrière : 20261124-cp-google-fill-rollback.sql
-- (remet NULL, et la chaîne vide pour Charles Champin).

UPDATE conseillers_personnels
   SET CP_Google = CASE CP_RefConseiller
     WHEN 0   THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+immobilier+-+Georges-Louis+Duquesne&query_place_id=ChIJY678BcNl5kcR6d9XCCNmCqM'
     WHEN 3   THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+chasseur+immobilier+-+C%C3%A9line+Thieriet&query_place_id=ChIJC3ritcTRp6MRcrG5xTLjqHM'
     WHEN 70  THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+chasseur+immobilier+-+B%C3%A9n%C3%A9dicte+Peynet&query_place_id=ChIJbQKacKM8ikgRDfgYm_Mgudc'
     WHEN 117 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Pascale+Saint-Jean&query_place_id=ChIJX_OZNA1v5kcR272nqtip3nQ'
     WHEN 128 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Caroline+Charvet&query_place_id=ChIJBTvHT7Nv5kcRY_uS8zbXHvQ'
     WHEN 140 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Isabelle+Lerre&query_place_id=ChIJC-KOPp9v5kcRFcRbzTlN8f0'
     WHEN 164 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Catherine+Blanchard&query_place_id=ChIJ15iSHWpuQUQRlOoeIY2Y2Zo'
     WHEN 171 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Margot+Saidy&query_place_id=ChIJg6PHPBdx5kcRffAQ61QZkiI'
     WHEN 178 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+chasseur+immobilier+-+Alexandre+Gagnat&query_place_id=ChIJ5YFGj4hv5kcRaIVyzsX-41g'
     WHEN 191 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Charles+Champin&query_place_id=ChIJDdk4vDtaVYgREga0oDN8f3E'
     WHEN 193 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Caroline+Rapp+Tr%C3%A9visan&query_place_id=ChIJH6JU2N20WgMR5S1SBFGNPis'
     WHEN 256 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+immobilier+-+Aude+Chamboredon&query_place_id=ChIJId_WxvxZ5m4R9nmGGiJRFjg'
     WHEN 282 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Dessislava+Pavlova&query_place_id=ChIJ5SruXO5v5kcRYNftN8m_92c'
     WHEN 305 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Chlo%C3%A9+Poissonnet&query_place_id=ChIJxcGGTWFt5kcReVkOBDuGj5k'
     WHEN 343 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+chasseur+immobilier+-+Marta+Clery&query_place_id=ChIJnT4j1j35QW0RuPyCmeoh0JE'
     WHEN 356 THEN 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+chasseur+immobilier+-+Florence+Roux+de+B%C3%A9zieux&query_place_id=ChIJK7ekGytv5kcRhSewRCpDyPc'
   END
 WHERE (CP_Google IS NULL OR CP_Google = '')
   AND (
        (CP_RefConseiller = 0   AND CP_Prenom = 'Georges-Louis'    AND CP_NomFamille = 'Duquesne')
     OR (CP_RefConseiller = 3   AND CP_Prenom = 'Céline'           AND CP_NomFamille = 'Thieriet')
     OR (CP_RefConseiller = 70  AND CP_Prenom = 'Bénédicte'        AND CP_NomFamille = 'Peynet')
     OR (CP_RefConseiller = 117 AND CP_Prenom = 'Pascale'          AND CP_NomFamille = 'Saint-Jean')
     OR (CP_RefConseiller = 128 AND CP_Prenom = 'Caroline'         AND CP_NomFamille = 'Charvet')
     OR (CP_RefConseiller = 140 AND CP_Prenom = 'Isabelle'         AND CP_NomFamille = 'Lerre')
     OR (CP_RefConseiller = 164 AND CP_Prenom = 'Catherine'        AND CP_NomFamille = 'Blanchard')
     OR (CP_RefConseiller = 171 AND CP_Prenom = 'Margot'           AND CP_NomFamille = 'Saidy')
     OR (CP_RefConseiller = 178 AND CP_Prenom = 'Alexandre'        AND CP_NomFamille = 'Gagnat')
     OR (CP_RefConseiller = 191 AND CP_Prenom = 'Charles'          AND CP_NomFamille = 'Champin')
     OR (CP_RefConseiller = 193 AND CP_Prenom = 'Caroline'         AND CP_NomFamille = 'Rapp Trévisan')
     OR (CP_RefConseiller = 256 AND CP_Prenom = 'Aude'             AND CP_NomFamille = 'Chamboredon')
     OR (CP_RefConseiller = 282 AND CP_Prenom = 'Dessislava'       AND CP_NomFamille = 'Pavlova')
     OR (CP_RefConseiller = 305 AND CP_Prenom = 'Chloé'            AND CP_NomFamille = 'Poissonnet')
     OR (CP_RefConseiller = 343 AND CP_Prenom = 'Marta'            AND CP_NomFamille = 'Cléry')
     OR (CP_RefConseiller = 356 AND CP_Prenom = 'Florence'         AND CP_NomFamille = 'Roux de Bézieux')
   );

-- Contrôle après :
--   SELECT CP_RefConseiller, CP_Prenom, CP_NomFamille, CHAR_LENGTH(CP_Google)
--     FROM conseillers_personnels
--    WHERE CP_RefConseiller IN (0, 3, 70, 117, 128, 140, 164, 171, 178, 191, 193, 256, 282, 305, 343, 356);
--   -- 16 lignes, CP_Google renseigné, longueur < 512
