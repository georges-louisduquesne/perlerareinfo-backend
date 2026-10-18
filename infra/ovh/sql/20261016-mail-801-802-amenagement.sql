-- 801 (Id 48547) et 802 (Id 48554) : « affiner son financement »
-- devient « affiner son projet d'aménagement ».
-- À blanc (16 oct. 2026) : les deux lignes contiennent encore « affiner son financement », une seule fois.
-- Le compte applicatif ne peut pas CREATE TABLE : le retour arrière est le REPLACE inverse,
-- sûr parce que la phrase nouvelle n'existe nulle part ailleurs dans ces deux corps.
-- Le 800 n'est pas modifié.

UPDATE mails
SET Contents = REPLACE(
      Contents,
      'affiner son financement',
      'affiner son projet d&rsquo;am&eacute;nagement'
    ),
    Updatedon = NOW()
WHERE Id IN (48547, 48554)
  AND INSTR(Contents, 'affiner son financement') > 0;

-- Retour arrière :
-- UPDATE mails
-- SET Contents = REPLACE(
--       Contents,
--       'affiner son projet d&rsquo;am&eacute;nagement',
--       'affiner son financement'
--     ),
--     Updatedon = NOW()
-- WHERE Id IN (48547, 48554)
--   AND INSTR(Contents, 'affiner son projet d&rsquo;am&eacute;nagement') > 0;
