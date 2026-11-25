-- Georges-Louis Duquesne, conseiller 0.
-- Ajoute le paragraphe déjà mis en ligne sur les deux fiches,
-- pour qu'une republication le conserve.
-- Le texte visible ne contient aucun fait nouveau.

UPDATE conseillers_personnels
SET CP_parcours_fr = CONCAT(
        CP_parcours_fr,
        CHAR(10), CHAR(10),
        'Depuis la co-fondation de Perle Rare en octobre 2005, Georges-Louis Duquesne suit les finances, les missions de recherche et les fonctions support. Il s’appuie sur vingt années de banque et de financement immobilier des particuliers, et sur sa connaissance de Paris, de son histoire et de ses quartiers.'
    ),
    CP_parcours_en = CONCAT(
        CP_parcours_en,
        CHAR(10), CHAR(10),
        'Since co-founding Perle Rare in October 2005, Georges-Louis Duquesne has overseen the firm’s finances, search operations and support services. He draws on twenty years in banking and personal mortgage finance, and on his knowledge of Paris and its neighbourhoods.'
    )
WHERE CP_RefConseiller = 0
  AND CP_Prenom = 'Georges-Louis'
  AND CP_NomFamille = 'Duquesne'
  AND CP_parcours_fr LIKE '%Il est le garant du bon déroulement des missions de recherche.'
  AND CP_parcours_en LIKE '%He makes sure that our assignments are a success.'
  AND CP_parcours_fr NOT LIKE '%Depuis la co-fondation de Perle Rare en octobre 2005%'
  AND CP_parcours_en NOT LIKE '%Since co-founding Perle Rare in October 2005%';
