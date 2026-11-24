-- Complète E_PropertyId sur les évènements RV VISITE SEUL, RV VISITE CLIENT, OFFRE,
-- OFFRE REFUSEE, OFFRE ACCEPTEE, RV COMPROMIS, RV ACTE AUTHENT., RV ACTE AUTHENT
-- et CR VISITE
-- dont le contact et le bien sont renseignés, E_PC_Id et E_PropertyId sont vides,
-- et le même contact + le même bien ont déjà un seul couple (E_PropertyId, E_PC_Id)
-- repris par une seule fiche property_contact de ce contact.
-- Inventaire du 2026-11-24 : 363 lignes. Le script recalcule le jeu à l'exécution.
-- Le trigger evenements_before_update ignore un E_PC_Id écrit à la main et le
-- recalcule depuis property_contact dès que E_PropertyId est renseigné. Il
-- recalcule aussi E_RefInterI si E_RefInterD est renseigné.
-- À exécuter uniquement sur ordre écrit, avec le client mysql, sans --force.
-- Retour arrière : infra/ovh/sql/20261124-evenements-rattrapage-pc-property-rollback.sql
-- Ne pas relancer si evenements_bak_pc_property_20261124 existe déjà.

DROP PROCEDURE IF EXISTS pr_assert_rattrapage_pc_property_20261124;

DELIMITER $$
CREATE PROCEDURE pr_assert_rattrapage_pc_property_20261124()
BEGIN
  DECLARE n INT DEFAULT 0;
  SELECT COUNT(*) INTO n
    FROM evenements_bak_pc_property_20261124 b
    JOIN evenements e ON e.E_RefEvenement = b.E_RefEvenement
   WHERE e.E_PC_Id IS NULL OR e.E_PC_Id = 0
      OR e.E_PropertyId IS NULL OR e.E_PropertyId = ''
      OR e.E_PropertyId <> b.donor_property_id
      OR e.E_PC_Id <> b.donor_pc_id;
  IF n > 0 THEN
    ROLLBACK;
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'Controle rattrapage en echec : transaction annulee';
  END IF;
END$$
DELIMITER ;

-- Jeu recalculé (table de session, aucune ligne métier modifiée).
DROP TEMPORARY TABLE IF EXISTS tmp_evenements_cibles_20261124;

CREATE TEMPORARY TABLE tmp_evenements_cibles_20261124 AS
SELECT c.E_RefEvenement,
       c.E_TypeEvenement,
       c.E_Statut,
       c.E_RefContact,
       c.E_RefBien,
       c.E_PC_Id,
       c.E_PropertyId,
       c.E_RefInterI,
       d.E_PropertyId AS donor_property_id,
       d.donor_pc_id
  FROM evenements c
  JOIN (
        SELECT E_RefBien,
               E_RefContact,
               MIN(E_PropertyId) AS E_PropertyId,
               MIN(E_PC_Id) AS donor_pc_id
          FROM evenements
         WHERE E_RefBien IS NOT NULL AND E_RefBien <> 0
           AND E_RefContact IS NOT NULL AND E_RefContact <> 0
           AND E_PC_Id IS NOT NULL AND E_PC_Id <> 0
           AND E_PropertyId IS NOT NULL AND E_PropertyId <> ''
         GROUP BY E_RefBien, E_RefContact
        HAVING COUNT(DISTINCT E_PropertyId) = 1
           AND COUNT(DISTINCT E_PC_Id) = 1
       ) d
    ON d.E_RefBien = c.E_RefBien
   AND d.E_RefContact = c.E_RefContact
  JOIN (
        SELECT PC_PropertyId, PC_RefContact, MIN(PC_Id) AS PC_Id
          FROM property_contact
         GROUP BY PC_PropertyId, PC_RefContact
        HAVING COUNT(*) = 1
       ) pc
    ON pc.PC_PropertyId = d.E_PropertyId
   AND pc.PC_RefContact = c.E_RefContact
   AND pc.PC_Id = d.donor_pc_id
 WHERE c.E_TypeEvenement IN (
         'RV VISITE SEUL',
         'RV VISITE CLIENT',
         'OFFRE',
         'OFFRE REFUSEE',
         'OFFRE ACCEPTEE',
         'RV COMPROMIS',
         'RV ACTE AUTHENT.',
         'RV ACTE AUTHENT',
         'CR VISITE'
       )
   AND c.E_RefContact IS NOT NULL AND c.E_RefContact <> 0
   AND c.E_RefBien IS NOT NULL AND c.E_RefBien <> 0
   AND (c.E_PC_Id IS NULL OR c.E_PC_Id = 0)
   AND (c.E_PropertyId IS NULL OR c.E_PropertyId = '');

-- Contrôle avant : lignes qui seront mises à jour.
SELECT COUNT(*) AS a_mettre_a_jour FROM tmp_evenements_cibles_20261124;

SELECT E_RefEvenement, E_TypeEvenement, E_Statut, E_RefContact, E_RefBien,
       E_PC_Id, E_PropertyId, donor_property_id, donor_pc_id
  FROM tmp_evenements_cibles_20261124
 ORDER BY E_RefEvenement;

-- Contrôle avant : donneur unique, mais fiche absente, multiple, ou différente.
-- Ces lignes ne sont pas mises à jour (inventaire 2026-11-24 : 22).
SELECT c.E_RefEvenement, c.E_TypeEvenement, c.E_Statut, c.E_RefContact, c.E_RefBien,
       d.E_PropertyId AS donor_property_id, d.donor_pc_id,
       (SELECT COUNT(*)
          FROM property_contact pc
         WHERE pc.PC_PropertyId = d.E_PropertyId
           AND pc.PC_RefContact = c.E_RefContact) AS nb_fiches,
       CASE
         WHEN (SELECT COUNT(*)
                 FROM property_contact pc
                WHERE pc.PC_PropertyId = d.E_PropertyId
                  AND pc.PC_RefContact = c.E_RefContact) = 0
           THEN 'sans fiche property_contact'
         WHEN (SELECT COUNT(*)
                 FROM property_contact pc
                WHERE pc.PC_PropertyId = d.E_PropertyId
                  AND pc.PC_RefContact = c.E_RefContact) > 1
           THEN 'plusieurs fiches property_contact'
         ELSE 'fiche differente du donneur'
       END AS motif
  FROM evenements c
  JOIN (
        SELECT E_RefBien,
               E_RefContact,
               MIN(E_PropertyId) AS E_PropertyId,
               MIN(E_PC_Id) AS donor_pc_id
          FROM evenements
         WHERE E_RefBien IS NOT NULL AND E_RefBien <> 0
           AND E_RefContact IS NOT NULL AND E_RefContact <> 0
           AND E_PC_Id IS NOT NULL AND E_PC_Id <> 0
           AND E_PropertyId IS NOT NULL AND E_PropertyId <> ''
         GROUP BY E_RefBien, E_RefContact
        HAVING COUNT(DISTINCT E_PropertyId) = 1
           AND COUNT(DISTINCT E_PC_Id) = 1
       ) d
    ON d.E_RefBien = c.E_RefBien
   AND d.E_RefContact = c.E_RefContact
 WHERE c.E_TypeEvenement IN (
         'RV VISITE SEUL',
         'RV VISITE CLIENT',
         'OFFRE',
         'OFFRE REFUSEE',
         'OFFRE ACCEPTEE',
         'RV COMPROMIS',
         'RV ACTE AUTHENT.',
         'RV ACTE AUTHENT',
         'CR VISITE'
       )
   AND c.E_RefContact IS NOT NULL AND c.E_RefContact <> 0
   AND c.E_RefBien IS NOT NULL AND c.E_RefBien <> 0
   AND (c.E_PC_Id IS NULL OR c.E_PC_Id = 0)
   AND (c.E_PropertyId IS NULL OR c.E_PropertyId = '')
   AND NOT EXISTS (
         SELECT 1
           FROM property_contact pc
          WHERE pc.PC_PropertyId = d.E_PropertyId
            AND pc.PC_RefContact = c.E_RefContact
         HAVING COUNT(*) = 1
            AND MIN(pc.PC_Id) = d.donor_pc_id
       )
 ORDER BY c.E_RefEvenement;

-- Contrôle avant : plusieurs E_PropertyId ou plusieurs E_PC_Id pour le même
-- contact et le même bien. Attendu vide. Ces lignes ne sont pas mises à jour.
SELECT c.E_RefEvenement, c.E_RefContact, c.E_RefBien,
       COUNT(DISTINCT d.E_PropertyId) AS nb_property,
       COUNT(DISTINCT d.E_PC_Id) AS nb_pc
  FROM evenements c
  JOIN evenements d
    ON d.E_RefBien = c.E_RefBien
   AND d.E_RefContact = c.E_RefContact
   AND d.E_PC_Id IS NOT NULL AND d.E_PC_Id <> 0
   AND d.E_PropertyId IS NOT NULL AND d.E_PropertyId <> ''
 WHERE c.E_TypeEvenement IN (
         'RV VISITE SEUL',
         'RV VISITE CLIENT',
         'OFFRE',
         'OFFRE REFUSEE',
         'OFFRE ACCEPTEE',
         'RV COMPROMIS',
         'RV ACTE AUTHENT.',
         'RV ACTE AUTHENT',
         'CR VISITE'
       )
   AND c.E_RefContact IS NOT NULL AND c.E_RefContact <> 0
   AND c.E_RefBien IS NOT NULL AND c.E_RefBien <> 0
   AND (c.E_PC_Id IS NULL OR c.E_PC_Id = 0)
   AND (c.E_PropertyId IS NULL OR c.E_PropertyId = '')
 GROUP BY c.E_RefEvenement, c.E_RefContact, c.E_RefBien
HAVING COUNT(DISTINCT d.E_PropertyId) > 1
    OR COUNT(DISTINCT d.E_PC_Id) > 1;

-- Échoue si la sauvegarde existe déjà : ne pas écraser un passage précédent.
CREATE TABLE evenements_bak_pc_property_20261124 AS
SELECT E_RefEvenement, E_TypeEvenement, E_Statut, E_RefContact, E_RefBien,
       E_PC_Id, E_PropertyId, E_RefInterI, donor_property_id, donor_pc_id
  FROM tmp_evenements_cibles_20261124;

ALTER TABLE evenements_bak_pc_property_20261124
  ADD PRIMARY KEY (E_RefEvenement);

SELECT COUNT(*) AS lignes_sauvegardees FROM evenements_bak_pc_property_20261124;

START TRANSACTION;

UPDATE evenements e
  JOIN evenements_bak_pc_property_20261124 b
    ON b.E_RefEvenement = e.E_RefEvenement
   SET e.E_PropertyId = b.donor_property_id
 WHERE (e.E_PC_Id IS NULL OR e.E_PC_Id = 0)
   AND (e.E_PropertyId IS NULL OR e.E_PropertyId = '')
   AND e.E_RefContact = b.E_RefContact
   AND e.E_RefBien = b.E_RefBien;

-- Doit être vide. Sinon la procédure annule la transaction.
SELECT e.E_RefEvenement, e.E_PC_Id, e.E_PropertyId,
       b.donor_pc_id, b.donor_property_id
  FROM evenements e
  JOIN evenements_bak_pc_property_20261124 b
    ON b.E_RefEvenement = e.E_RefEvenement
 WHERE e.E_PC_Id IS NULL OR e.E_PC_Id = 0
    OR e.E_PropertyId IS NULL OR e.E_PropertyId = ''
    OR e.E_PropertyId <> b.donor_property_id
    OR e.E_PC_Id <> b.donor_pc_id;

-- Effet de bord du trigger sur E_RefInterI. Attendu vide au 2026-11-24.
SELECT e.E_RefEvenement, e.E_RefInterI, b.E_RefInterI AS ref_inter_i_avant
  FROM evenements e
  JOIN evenements_bak_pc_property_20261124 b
    ON b.E_RefEvenement = e.E_RefEvenement
 WHERE NOT (e.E_RefInterI <=> b.E_RefInterI);

CALL pr_assert_rattrapage_pc_property_20261124();

COMMIT;

DROP PROCEDURE IF EXISTS pr_assert_rattrapage_pc_property_20261124;
DROP TEMPORARY TABLE IF EXISTS tmp_evenements_cibles_20261124;
