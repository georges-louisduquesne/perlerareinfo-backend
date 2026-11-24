-- Retour arrière pour 20261124-cp-google-fill.sql.
-- Remet la valeur constatée avant l’alimentation : NULL, ou chaîne vide pour Charles Champin (191).
-- Ne touche une ligne que si elle porte encore exactement l’URL écrite.

UPDATE conseillers_personnels
   SET CP_Google = CASE CP_RefConseiller
     WHEN 191 THEN ''
     ELSE NULL
   END
 WHERE (CP_RefConseiller = 0   AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+immobilier+-+Georges-Louis+Duquesne&query_place_id=ChIJY678BcNl5kcR6d9XCCNmCqM')
    OR (CP_RefConseiller = 3   AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+chasseur+immobilier+-+C%C3%A9line+Thieriet&query_place_id=ChIJC3ritcTRp6MRcrG5xTLjqHM')
    OR (CP_RefConseiller = 70  AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+chasseur+immobilier+-+B%C3%A9n%C3%A9dicte+Peynet&query_place_id=ChIJbQKacKM8ikgRDfgYm_Mgudc')
    OR (CP_RefConseiller = 117 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Pascale+Saint-Jean&query_place_id=ChIJX_OZNA1v5kcR272nqtip3nQ')
    OR (CP_RefConseiller = 128 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Caroline+Charvet&query_place_id=ChIJBTvHT7Nv5kcRY_uS8zbXHvQ')
    OR (CP_RefConseiller = 140 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Isabelle+Lerre&query_place_id=ChIJC-KOPp9v5kcRFcRbzTlN8f0')
    OR (CP_RefConseiller = 164 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Catherine+Blanchard&query_place_id=ChIJ15iSHWpuQUQRlOoeIY2Y2Zo')
    OR (CP_RefConseiller = 171 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Margot+Saidy&query_place_id=ChIJg6PHPBdx5kcRffAQ61QZkiI')
    OR (CP_RefConseiller = 178 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+chasseur+immobilier+-+Alexandre+Gagnat&query_place_id=ChIJ5YFGj4hv5kcRaIVyzsX-41g')
    OR (CP_RefConseiller = 191 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Charles+Champin&query_place_id=ChIJDdk4vDtaVYgREga0oDN8f3E')
    OR (CP_RefConseiller = 193 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Caroline+Rapp+Tr%C3%A9visan&query_place_id=ChIJH6JU2N20WgMR5S1SBFGNPis')
    OR (CP_RefConseiller = 256 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+immobilier+-+Aude+Chamboredon&query_place_id=ChIJId_WxvxZ5m4R9nmGGiJRFjg')
    OR (CP_RefConseiller = 282 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Dessislava+Pavlova&query_place_id=ChIJ5SruXO5v5kcRYNftN8m_92c')
    OR (CP_RefConseiller = 305 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Chlo%C3%A9+Poissonnet&query_place_id=ChIJxcGGTWFt5kcReVkOBDuGj5k')
    OR (CP_RefConseiller = 343 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+chasseur+immobilier+-+Marta+Clery&query_place_id=ChIJnT4j1j35QW0RuPyCmeoh0JE')
    OR (CP_RefConseiller = 356 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+chasseur+immobilier+-+Florence+Roux+de+B%C3%A9zieux&query_place_id=ChIJK7ekGytv5kcRhSewRCpDyPc');
