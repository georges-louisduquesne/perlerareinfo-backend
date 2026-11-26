-- Complète E_PropertyId sur les évènements actifs qui partagent un contact
-- et un bien avec exactement un property réel (36 caractères, hors texte
-- « null »). Inventaire du 2026-11-26 : 141 lignes.
--   130 sans fiche property_contact : le trigger laisse E_PC_Id NULL.
--   11 avec deux fiches : le trigger pose la même fiche que l'évènement
--   frère déjà lié (51313 → 128303, 59572 → 843555, 60097 → 555062).
-- Sept lignes ont E_RefInterD renseigné et E_RefInterI vide. Le trigger
-- evenements_before_update y écrit I_RefGroupe :
--   423677, 424328, 424413, 424414 → 57
--   479289 → 243
--   483823, 490453 → 139
-- Le contact 61583 / bien 26616 est déjà traité.
-- À exécuter uniquement sur ordre écrit. Ne pas relancer si
-- evenements_bak_property_vides_20261126 existe déjà.
-- Retour arrière : 20261126-evenements-rattrapage-property-vides-rollback.sql

CREATE TABLE evenements_bak_property_vides_20261126 AS
SELECT c.E_RefEvenement,
       c.E_TypeEvenement,
       c.E_Statut,
       c.E_RefContact,
       c.E_RefBien,
       c.E_PC_Id,
       c.E_PropertyId,
       c.E_RefInterI,
       c.E_RefInterD,
       d.property_id AS donor_property_id,
       (SELECT pc.PC_Id
          FROM property_contact pc
         WHERE pc.PC_PropertyId = d.property_id
           AND pc.PC_RefContact = c.E_RefContact
         LIMIT 1) AS expected_pc_id,
       CASE
         WHEN c.E_RefInterD IS NOT NULL THEN (
              SELECT i.I_RefGroupe
                FROM intermediaires_directs i
               WHERE i.I_RefIntermediaire = c.E_RefInterD
         )
         ELSE c.E_RefInterI
       END AS expected_inter_i
  FROM evenements c
  JOIN (
        SELECT E_RefContact,
               E_RefBien,
               MIN(E_PropertyId) AS property_id
          FROM evenements
         WHERE E_Statut = 1
           AND E_RefContact IS NOT NULL AND E_RefContact <> 0
           AND E_RefBien IS NOT NULL AND E_RefBien <> 0
           AND E_PropertyId IS NOT NULL AND E_PropertyId <> ''
           AND E_PropertyId <> 'null'
           AND CHAR_LENGTH(E_PropertyId) = 36
         GROUP BY E_RefContact, E_RefBien
        HAVING COUNT(DISTINCT E_PropertyId) = 1
       ) d
    ON d.E_RefContact = c.E_RefContact
   AND d.E_RefBien = c.E_RefBien
 WHERE c.E_Statut = 1
   AND (c.E_PropertyId IS NULL OR c.E_PropertyId = '' OR c.E_PropertyId = 'null')
   AND NOT (c.E_RefContact = 61583 AND c.E_RefBien = 26616);

ALTER TABLE evenements_bak_property_vides_20261126
  ADD PRIMARY KEY (E_RefEvenement);

UPDATE evenements e
  JOIN evenements_bak_property_vides_20261126 b
    ON b.E_RefEvenement = e.E_RefEvenement
   SET e.E_PropertyId = b.donor_property_id
 WHERE e.E_Statut = 1
   AND e.E_RefContact = b.E_RefContact
   AND e.E_RefBien = b.E_RefBien
   AND (e.E_PropertyId IS NULL OR e.E_PropertyId = '' OR e.E_PropertyId = 'null');

SELECT COUNT(*) AS lignes_sauvegardees
  FROM evenements_bak_property_vides_20261126;

SELECT e.E_RefEvenement, e.E_PC_Id, e.E_PropertyId, e.E_RefInterI,
       b.expected_pc_id, b.donor_property_id, b.expected_inter_i
  FROM evenements e
  JOIN evenements_bak_property_vides_20261126 b
    ON b.E_RefEvenement = e.E_RefEvenement
 WHERE e.E_PropertyId <> b.donor_property_id
    OR NOT (e.E_PC_Id = b.expected_pc_id
            OR (e.E_PC_Id IS NULL AND b.expected_pc_id IS NULL))
    OR NOT (e.E_RefInterI = b.expected_inter_i
            OR (e.E_RefInterI IS NULL AND b.expected_inter_i IS NULL));
