-- Modèle d'e-mail candidat (State = 2), popin « Envoyer un e-mail » d'une fiche candidat.
-- Name    : 901 Accusé de réception Candidature directe
-- Subject : Votre candidature chez Perle Rare
-- La signature n'est pas dans Contents : la popin de visualisation l'ajoute
-- depuis CP_MelSignature du conseiller connecté.
-- À blanc (17 nov. 2026) : aucune ligne Name LIKE '901%'.
-- Série 901–998 : réservée aux candidats (le 999 reste le libre des contacts).

INSERT INTO mails (Name, Createdon, Updatedon, Subject, Contents, State, Createdby, Importancehaute)
SELECT
  '901 Accusé de réception Candidature directe',
  NOW(),
  NOW(),
  'Votre candidature chez Perle Rare',
  '<span style="font-size: 10pt;">Bonjour,<br /><br />Je vous remercie pour l&rsquo;int&eacute;r&ecirc;t que vous portez &agrave; Perle Rare.<br /><br />Je viens de prendre connaissance de votre candidature et serais ravie de pouvoir &eacute;changer avec vous afin d&rsquo;en apprendre davantage sur votre parcours, vos exp&eacute;riences et les motivations qui vous am&egrave;nent &agrave; envisager cette nouvelle perspective professionnelle.<br /><br />Pourriez-vous, s&rsquo;il vous pla&icirc;t, me communiquer vos disponibilit&eacute;s pour convenir d&rsquo;un &eacute;change t&eacute;l&eacute;phonique au cours de la semaine ?<br /><br />Dans l&rsquo;attente de votre retour, je vous souhaite une excellente journ&eacute;e.<br /><br />Bien cordialement,<br /><br /></span>',
  2,
  0,
  0
FROM DUAL
WHERE NOT EXISTS (
  SELECT 1 FROM mails WHERE State = 2 AND Name = '901 Accusé de réception Candidature directe'
);

-- Retour arrière :
-- DELETE FROM mails
-- WHERE State = 2
--   AND Name = '901 Accusé de réception Candidature directe'
--   AND Subject = 'Votre candidature chez Perle Rare';
