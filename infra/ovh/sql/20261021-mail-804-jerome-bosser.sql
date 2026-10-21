-- Retire le modèle 803 (Arthur Bruyère / GarantMe, Id 48594) et ajoute le 804.
-- Arthur Bruyère ne doit pas avoir de modèle de mise en relation.
-- State 1 est l'état des modèles retirés (ex. Id 15, 23, 31) : la ligne reste, elle n'est plus proposée.
-- Le 804 reprend le 802 (Id 48554) pour le 8e partenaire : Jérôme Bosser, JB Diffusion, petits travaux.
-- Destinataire : jerome.bosser@jb-diffusion.fr (aussi forcé côté front). Téléphone annuaire : 06 60 93 72 79.
-- À blanc (21 oct. 2026) : une seule ligne State 2, Id 48594, Name 803, To support@garantme.fr.
-- Aucune ligne Name 804. Les 800, 801 et 802 ne sont pas modifiés.
-- Appliqué : 803 (Id 48594) passé en State 1. 804 inséré, Id 48621.

UPDATE mails
SET State = 1,
    Updatedon = NOW()
WHERE Id = 48594
  AND Name = '803 - Mise en relation avec Arthur Bruyère (GarantMe)'
  AND State = 2
  AND `To` = 'support@garantme.fr';

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
  '804 - Mise en relation avec Jérôme Bosser (JB Diffusion)',
  NOW(),
  NOW(),
  'Perle Rare : mise en relation',
  '<p class="MsoNormal"><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">Bonjour J&eacute;r&ocirc;me,<br><br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">Pourrais-tu s&rsquo;il te pla&icirc;t contacter %C_Prenom% %C_NomFamille% avec qui j&rsquo;ai &eacute;chang&eacute; sur sa recherche d&rsquo;appartement, afin d&rsquo;affiner son projet de petits travaux ?<br><br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">Merci beaucoup.<br><br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">%C_Prenom% %C_NomFamille%<br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">%C_TelPersonnel1%<br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">%C_Mel1%<br><br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">Voici &eacute;galement les coordonn&eacute;es de J&eacute;r&ocirc;me BOSSER :<br><br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;">06 60 93 72 79<br></span><span style="font-family: Helvetica-Light, sans-serif; font-size: 11pt;"><a href="mailto:jerome.bosser@jb-diffusion.fr"><span style="color: windowtext; text-decoration: none; text-underline: none;">jerome.bosser@jb-diffusion.fr</span></a></span></p>',
  2,
  0,
  0,
  0,
  0,
  0,
  '',
  'jerome.bosser@jb-diffusion.fr',
  'support@perle-rare.com',
  0,
  0
FROM DUAL
WHERE NOT EXISTS (
  SELECT 1 FROM mails WHERE Name = '804 - Mise en relation avec Jérôme Bosser (JB Diffusion)'
);

-- Retour arrière :
-- UPDATE mails
-- SET State = 2, Updatedon = NOW()
-- WHERE Id = 48594
--   AND Name = '803 - Mise en relation avec Arthur Bruyère (GarantMe)'
--   AND State = 1;
-- DELETE FROM mails
-- WHERE Name = '804 - Mise en relation avec Jérôme Bosser (JB Diffusion)'
--   AND State = 2
--   AND `To` = 'jerome.bosser@jb-diffusion.fr';
