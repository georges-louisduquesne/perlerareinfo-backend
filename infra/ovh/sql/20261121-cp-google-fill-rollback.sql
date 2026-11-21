-- Retour arrière pour 20261121-cp-google-fill.sql.
-- Remet CP_Google à NULL, valeur constatée avant l’alimentation.
-- Ne touche une ligne que si elle porte encore exactement l’URL écrite.

UPDATE conseillers_personnels
   SET CP_Google = NULL
 WHERE (CP_RefConseiller = 81  AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Laurence+Berry,+38+Rue+Dauphine,+Paris')
    OR (CP_RefConseiller = 127 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Constance+Perrin,+4+Rue+Bellanger,+92200+Neuilly-sur-Seine')
    OR (CP_RefConseiller = 162 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+St%C3%A9phane+Trichard,+69+Rue+Sedaine,+75011+Paris')
    OR (CP_RefConseiller = 239 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Corinne+Lelarge,+21+Rue+Verniquet,+75017+Paris')
    OR (CP_RefConseiller = 265 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Anne+Davoli,+99+Rue+Jean-Pierre+Timbaud,+Paris')
    OR (CP_RefConseiller = 274 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Sophie+McCaffry,+55+Rue+du+Cherche-Midi,+75006+Paris')
    OR (CP_RefConseiller = 280 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Cl%C3%A9mence+Kamienny,+54+Rue+Lamartine,+75009+Paris')
    OR (CP_RefConseiller = 290 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Elsa+Pons,+14+Rue+Bidel,+92600+Asni%C3%A8res-sur-Seine')
    OR (CP_RefConseiller = 322 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Laurence+Pinaire,+172+Boulevard+Haussmann,+75008+Paris')
    OR (CP_RefConseiller = 339 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Lionel+Jamet,+38+Rue+Madame,+75006+Paris')
    OR (CP_RefConseiller = 347 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Roxane+Bach%C3%A9,+22+Rue+du+Ch%C3%A2teau+d%27Eau,+75010+Paris')
    OR (CP_RefConseiller = 357 AND CP_Google = 'https://www.google.com/maps/search/?api=1&query=Perle+Rare+-+Chasseur+Immobilier+-+Isabelle+Chetochine,+85+Avenue+Victor+Hugo,+92100+Boulogne-Billancourt');
