-- Retour arrière : Catherine Blanchard redevient BLANCHARD, comme avant le 24 novembre 2026.
-- Valeurs lues avant l'écriture : CP_RefConseiller 164, CP_Prenom Catherine,
-- CP_NomFamille BLANCHARD, CP_Libelle Madame Catherine BLANCHARD.

UPDATE conseillers_personnels
SET CP_NomFamille = 'BLANCHARD',
    CP_Libelle = 'Madame Catherine BLANCHARD'
WHERE CP_RefConseiller = 164
  AND CP_Prenom = 'Catherine'
  AND CP_NomFamille = 'Blanchard'
  AND CP_Libelle = 'Madame Catherine Blanchard';
