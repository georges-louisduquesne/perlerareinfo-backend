-- Modèles d'e-mail candidat (State = 2), appelés par la popin « je veux envoyer un e-mail ».
-- Le sujet et le corps se renseignent ici, pas dans le code :
--   C01 - Accusé de réception
--   C99 - Libre
-- Appliqué : Id 48850 « C01 - Accusé de réception », Id 48851 « C99 - Libre ».
-- Sujet et corps vides : à remplir dans `mails`.Subject et `mails`.Contents.

INSERT INTO mails (Name, Createdon, Updatedon, Subject, Contents, State, Createdby, Importancehaute)
SELECT 'C01 - Accusé de réception', NOW(), NOW(), '', '', 2, 0, 0
FROM DUAL
WHERE NOT EXISTS (
  SELECT 1 FROM mails WHERE State = 2 AND Name LIKE 'C01%'
);

INSERT INTO mails (Name, Createdon, Updatedon, Subject, Contents, State, Createdby, Importancehaute)
SELECT 'C99 - Libre', NOW(), NOW(), '', '', 2, 0, 0
FROM DUAL
WHERE NOT EXISTS (
  SELECT 1 FROM mails WHERE State = 2 AND Name LIKE 'C99%'
);

-- Retour arrière (seulement tant que le sujet et le corps sont encore vides) :
-- DELETE FROM mails
-- WHERE State = 2
--   AND Name IN ('C01 - Accusé de réception', 'C99 - Libre')
--   AND Subject = ''
--   AND (Contents = '' OR Contents IS NULL);
