-- Modèle 801, même logique que le 800 (Id 33979) : mise en relation financement.
-- Destinataire ajouté à l'envoi : pierre@apm-group.fr (aussi forcé côté front).
-- À blanc (15 oct. 2026) : SELECT COUNT(*) FROM mails WHERE Name LIKE '801%' → 0.
-- Appliqué : Id 48547. Aucune ligne préalable, pas de table de sauvegarde.
-- Le 800 n'est pas modifié.

INSERT INTO mails (
  Name,
  Createdon,
  Updatedon,
  Subject,
  Contents,
  State,
  Createdby,
  Updatedby,
  Template1,
  Template2,
  Template3,
  Cc,
  `To`,
  Cci,
  Importancehaute,
  Envoyeur
)
SELECT
  '801 - Mise en relation avec Pierre-Henri (APM Group)',
  NOW(),
  NOW(),
  'Perle Rare : mise en relation',
  '<p class="MsoNormal"><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">Bonjour Pierre-Henri,<br><br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">Pourrais-tu s&rsquo;il te pla&icirc;t contacter %C_Prenom% %C_NomFamille% avec qui j&rsquo;ai &eacute;chang&eacute; sur sa recherche d&rsquo;appartement, afin d&rsquo;affiner son projet d&rsquo;am&eacute;nagement ?<br><br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">Merci beaucoup.<br><br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">%C_Prenom% %C_NomFamille%<br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">%C_TelPersonnel1%<br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">%C_Mel1%<br><br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">Voici &eacute;galement les coordonn&eacute;es de Pierre-Henri MICHEL chez APM GROUP :<br><br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">06 67 82 74 74<br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;"><a href="mailto:pierre@apm-group.fr"><span style="color: windowtext; text-decoration: none; text-underline: none;">pierre@apm-group.fr</span></a></span></p>',
  2,
  0,
  0,
  0,
  0,
  0,
  '',
  'pierre@apm-group.fr',
  'support@perle-rare.com',
  0,
  0
FROM DUAL
WHERE NOT EXISTS (
  SELECT 1 FROM mails WHERE Name = '801 - Mise en relation avec Pierre-Henri (APM Group)'
);

-- Retour arrière :
-- DELETE FROM mails
-- WHERE Name = '801 - Mise en relation avec Pierre-Henri (APM Group)'
--   AND State = 2
--   AND `To` = 'pierre@apm-group.fr';
