-- Retour arrière pour 20261120-cp-linkedin-amelie-celine.sql.
-- Remet CP_LinkedIn à NULL, valeur constatée avant l’alimentation.
-- Ne touche une ligne que si elle porte encore exactement l’URL écrite.

UPDATE conseillers_personnels
   SET CP_LinkedIn = NULL
 WHERE CP_RefConseiller = 3
   AND CP_Prenom = 'Céline'
   AND CP_NomFamille = 'Thieriet'
   AND CP_LinkedIn = 'https://www.linkedin.com/in/celine-thieriet-61b1b71/';

UPDATE conseillers_personnels
   SET CP_LinkedIn = NULL
 WHERE CP_RefConseiller = 102
   AND CP_Prenom = 'Amélie'
   AND CP_NomFamille = 'Cailliez'
   AND CP_LinkedIn = 'https://www.linkedin.com/in/am%C3%A9lie-cailliez-867426170/';
