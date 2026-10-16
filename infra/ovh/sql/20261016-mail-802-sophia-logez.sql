-- Modèle 802, même logique que le 801 (Id 48547) : mise en relation financement.
-- Destinataire ajouté à l'envoi : logez.sophia@orange.fr (aussi forcé côté front).
-- Téléphone repris de l'annuaire partenaires (Sophia Logez, 06 47 87 74 89).
-- À blanc (16 oct. 2026) : aucune ligne Name 802 ni To logez.sophia@orange.fr.
-- Appliqué : Id 48554. Aucune ligne préalable, pas de table de sauvegarde.
-- Le 800 et le 801 ne sont pas modifiés.

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
  '802 - Mise en relation avec Sophia Logez',
  NOW(),
  NOW(),
  'Perle Rare : mise en relation',
  '<p class="MsoNormal"><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">Bonjour Sophia,<br><br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">Pourrais-tu s&rsquo;il te pla&icirc;t contacter %C_Prenom% %C_NomFamille% avec qui j&rsquo;ai &eacute;chang&eacute; sur sa recherche d&rsquo;appartement, afin d&rsquo;affiner son financement ?<br><br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">Merci beaucoup.<br><br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">%C_Prenom% %C_NomFamille%<br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">%C_TelPersonnel1%<br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">%C_Mel1%<br><br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">Voici &eacute;galement les coordonn&eacute;es de Sophia LOGEZ :<br><br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">06 47 87 74 89<br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;"><a href="mailto:logez.sophia@orange.fr"><span style="color: windowtext; text-decoration: none; text-underline: none;">logez.sophia@orange.fr</span></a></span></p>',
  2,
  0,
  0,
  0,
  0,
  0,
  '',
  'logez.sophia@orange.fr',
  'support@perle-rare.com',
  0,
  0
FROM DUAL
WHERE NOT EXISTS (
  SELECT 1 FROM mails WHERE Name = '802 - Mise en relation avec Sophia Logez'
);

-- Retour arrière :
-- DELETE FROM mails
-- WHERE Name = '802 - Mise en relation avec Sophia Logez'
--   AND State = 2
--   AND `To` = 'logez.sophia@orange.fr';
