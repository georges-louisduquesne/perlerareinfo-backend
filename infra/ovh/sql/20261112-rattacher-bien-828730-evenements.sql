-- Rattache le bien property_contact 828730
-- (14 Rue de Crussol, 75011, 495 000 €, contact 60861 VERDUYN 3)
-- aux évènements 513068 (OFFRE) et 513525 (RV VISITE CLIENT).
--
-- Avant (SELECT du 12 novembre 2026) :
--   les deux lignes avaient E_PC_Id NULL et E_PropertyId NULL,
--   E_RefBien = 27055 (même adresse), E_RefContact = 60861.
-- La visite 513062 du même contact était déjà liée ainsi :
--   E_PC_Id = 828730, E_PropertyId = 891ac700-ced7-11f1-b587-21992d485e72.
--
-- Appliqué le 12 novembre 2026 : 2 lignes.
-- E_RefBien, le texte, le type, la date et le contact n'ont pas été modifiés.

UPDATE evenements
SET E_PC_Id = 828730,
    E_PropertyId = '891ac700-ced7-11f1-b587-21992d485e72'
WHERE E_RefEvenement IN (513068, 513525)
  AND E_RefContact = 60861
  AND E_RefBien = 27055
  AND (E_PC_Id IS NULL OR E_PC_Id = 828730)
  AND (E_PropertyId IS NULL OR E_PropertyId = '891ac700-ced7-11f1-b587-21992d485e72');

-- Retour arrière (remet les deux colonnes à NULL, état lu avant l'écriture) :
-- UPDATE evenements
-- SET E_PC_Id = NULL,
--     E_PropertyId = NULL
-- WHERE E_RefEvenement IN (513068, 513525)
--   AND E_PC_Id = 828730
--   AND E_PropertyId = '891ac700-ced7-11f1-b587-21992d485e72';
