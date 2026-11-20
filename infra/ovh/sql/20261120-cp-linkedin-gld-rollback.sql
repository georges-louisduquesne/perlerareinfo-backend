-- Retour arrière pour 20261120-cp-linkedin-gld.sql.
-- Remet CP_LinkedIn à NULL, valeur constatée avant l’alimentation.
-- Ne touche la ligne que si elle porte encore exactement l’URL écrite.

UPDATE conseillers_personnels
   SET CP_LinkedIn = NULL
 WHERE CP_RefConseiller = 0
   AND CP_Prenom = 'Georges-Louis'
   AND CP_NomFamille = 'Duquesne'
   AND CP_LinkedIn = 'https://www.linkedin.com/in/glduquesne/';
