-- Retire le paragraphe ajouté le 25 novembre 2026 au parcours de Georges-Louis.
-- Ne retire que ce paragraphe, et seulement s'il est encore à la fin.

UPDATE conseillers_personnels
SET CP_parcours_fr = LEFT(
        CP_parcours_fr,
        CHAR_LENGTH(CP_parcours_fr) - CHAR_LENGTH(CONCAT(
            CHAR(10), CHAR(10),
            'Depuis la co-fondation de Perle Rare en octobre 2005, Georges-Louis Duquesne suit les finances, les missions de recherche et les fonctions support. Il s’appuie sur vingt années de banque et de financement immobilier des particuliers, et sur sa connaissance de Paris, de son histoire et de ses quartiers.'
        ))
    ),
    CP_parcours_en = LEFT(
        CP_parcours_en,
        CHAR_LENGTH(CP_parcours_en) - CHAR_LENGTH(CONCAT(
            CHAR(10), CHAR(10),
            'Since co-founding Perle Rare in October 2005, Georges-Louis Duquesne has overseen the firm’s finances, search operations and support services. He draws on twenty years in banking and personal mortgage finance, and on his knowledge of Paris and its neighbourhoods.'
        ))
    )
WHERE CP_RefConseiller = 0
  AND CP_Prenom = 'Georges-Louis'
  AND CP_NomFamille = 'Duquesne'
  AND CP_parcours_fr LIKE CONCAT(
        '%', CHAR(10), CHAR(10),
        'Depuis la co-fondation de Perle Rare en octobre 2005, Georges-Louis Duquesne suit les finances, les missions de recherche et les fonctions support. Il s’appuie sur vingt années de banque et de financement immobilier des particuliers, et sur sa connaissance de Paris, de son histoire et de ses quartiers.'
    )
  AND CP_parcours_en LIKE CONCAT(
        '%', CHAR(10), CHAR(10),
        'Since co-founding Perle Rare in October 2005, Georges-Louis Duquesne has overseen the firm’s finances, search operations and support services. He draws on twenty years in banking and personal mortgage finance, and on his knowledge of Paris and its neighbourhoods.'
    );
