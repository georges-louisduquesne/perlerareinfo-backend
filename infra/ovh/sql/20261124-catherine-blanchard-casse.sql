-- Catherine Blanchard : le nom s'écrit Blanchard.
-- Contrôle déjà fait : une ligne, CP_RefConseiller 164,
-- CP_NomFamille = BLANCHARD, CP_Libelle = Madame Catherine BLANCHARD.
-- La session SQL ne peut pas créer de table de sauvegarde.
-- Les valeurs d'avant sont dans le script de retour arrière.

UPDATE conseillers_personnels
SET CP_NomFamille = 'Blanchard',
    CP_Libelle = 'Madame Catherine Blanchard'
WHERE CP_RefConseiller = 164
  AND CP_Prenom = 'Catherine'
  AND CP_NomFamille = 'BLANCHARD'
  AND CP_Libelle = 'Madame Catherine BLANCHARD';
