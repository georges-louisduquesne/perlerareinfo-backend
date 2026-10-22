-- Le modèle 804 (Id 48621, Jérôme Bosser) prend le numéro 803.
-- À blanc (22 oct. 2026) : Id 48621, State 2, Name « 804 - Mise en relation avec Jérôme Bosser (JB Diffusion) »,
-- To jerome.bosser@jb-diffusion.fr. Le 803 d'Arthur Bruyère (Id 48594) reste en State 1, retiré.
-- Appliqué : Id 48621 renommé en « 803 - Mise en relation avec Jérôme Bosser (JB Diffusion) ».

UPDATE mails
SET Name = '803 - Mise en relation avec Jérôme Bosser (JB Diffusion)',
    Updatedon = NOW()
WHERE Id = 48621
  AND Name = '804 - Mise en relation avec Jérôme Bosser (JB Diffusion)'
  AND State = 2
  AND `To` = 'jerome.bosser@jb-diffusion.fr';

-- Retour arrière :
-- UPDATE mails
-- SET Name = '804 - Mise en relation avec Jérôme Bosser (JB Diffusion)',
--     Updatedon = NOW()
-- WHERE Id = 48621
--   AND Name = '803 - Mise en relation avec Jérôme Bosser (JB Diffusion)'
--   AND State = 2
--   AND `To` = 'jerome.bosser@jb-diffusion.fr';
