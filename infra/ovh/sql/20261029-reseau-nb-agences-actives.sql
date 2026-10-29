-- I2_Nb_dagences_sur_zone = nombre d'agences actives liées (I_Actif = 1, I_RefGroupe)
-- pour chaque intermediaires_indirects dont I2_Categorie = « Réseau d'agences ».
--
-- À blanc le 29 oct. 2026 : 196 réseaux, 47 écarts (le stocké est presque toujours
-- en dessous du réel : LAFORET 65/73, CENTURY 21 81/87, …). Les autres catégories
-- ont déjà 0 et ne sont pas touchées.
-- Appliqué le 29 oct. 2026 : 195 réseaux encore dans la catégorie, 49 valeurs
-- réécrites, 0 écart restant (LAFORET 73, CENTURY 21 87). Copie :
-- intermediaires_indirects_nb_agences_bak_20261029.
-- Essai annulé ensuite : création +1, passage inactif -1, réactivation +1,
-- déplacement d'un réseau à l'autre, et une sauvegarde à 999 réécrite au décompte réel.
--
-- Ensuite le compteur suit les agences, pour tous les écrivains (API, import, SQL) :
--   +1  agence active liée créée, réactivée, ou rattachée à ce réseau ;
--   -1  agence liée désactivée, détachée, déplacée vers un autre réseau, ou supprimée.
-- Une sauvegarde de fiche réseau envoie toute la ligne, donc l'ancien nombre.
-- Le trigger BEFORE UPDATE réécrit alors le compteur avec le décompte réel,
-- ce qui empêche cette sauvegarde d'annuler un +1 / -1 déjà fait.
--
-- Compte cursor_client : pas de DDL. À lancer en root MariaDB sur le VPS.
-- Retour arrière en bas : d'abord les triggers, ensuite la table de sauvegarde.
-- Sans cet ordre, le BEFORE UPDATE remettrait le décompte réel pendant la restauration.

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS intermediaires_indirects_nb_agences_bak_20261029 AS
SELECT I2_RefInterm_indirect,
       I2_NomInterm_indirect,
       I2_Categorie,
       I2_Nb_dagences_sur_zone
FROM intermediaires_indirects
WHERE I2_Categorie = 'Réseau d''agences';

UPDATE intermediaires_indirects r
SET r.I2_Nb_dagences_sur_zone = (
  SELECT COUNT(*)
  FROM intermediaires_directs a
  WHERE a.I_RefGroupe = r.I2_RefInterm_indirect
    AND a.I_Actif = 1
)
WHERE r.I2_Categorie = 'Réseau d''agences';

DROP TRIGGER IF EXISTS intermediaires_directs_reseau_nb_ai;
DROP TRIGGER IF EXISTS intermediaires_directs_reseau_nb_au;
DROP TRIGGER IF EXISTS intermediaires_directs_reseau_nb_ad;
DROP TRIGGER IF EXISTS intermediaires_indirects_reseau_nb_bi;
DROP TRIGGER IF EXISTS intermediaires_indirects_reseau_nb_bu;

DELIMITER $$

CREATE TRIGGER intermediaires_directs_reseau_nb_ai
AFTER INSERT ON intermediaires_directs
FOR EACH ROW
BEGIN
  IF NEW.I_Actif = 1 AND NEW.I_RefGroupe IS NOT NULL AND NEW.I_RefGroupe <> 0 THEN
    UPDATE intermediaires_indirects
    SET I2_Nb_dagences_sur_zone = IFNULL(I2_Nb_dagences_sur_zone, 0) + 1
    WHERE I2_RefInterm_indirect = NEW.I_RefGroupe
      AND I2_Categorie = 'Réseau d''agences';
  END IF;
END$$

CREATE TRIGGER intermediaires_directs_reseau_nb_au
AFTER UPDATE ON intermediaires_directs
FOR EACH ROW
BEGIN
  IF OLD.I_Actif = 1
     AND OLD.I_RefGroupe IS NOT NULL
     AND OLD.I_RefGroupe <> 0
     AND (NEW.I_Actif <> 1 OR NOT (OLD.I_RefGroupe <=> NEW.I_RefGroupe)) THEN
    UPDATE intermediaires_indirects
    SET I2_Nb_dagences_sur_zone = GREATEST(IFNULL(I2_Nb_dagences_sur_zone, 0) - 1, 0)
    WHERE I2_RefInterm_indirect = OLD.I_RefGroupe
      AND I2_Categorie = 'Réseau d''agences';
  END IF;

  IF NEW.I_Actif = 1
     AND NEW.I_RefGroupe IS NOT NULL
     AND NEW.I_RefGroupe <> 0
     AND (OLD.I_Actif <> 1 OR NOT (OLD.I_RefGroupe <=> NEW.I_RefGroupe)) THEN
    UPDATE intermediaires_indirects
    SET I2_Nb_dagences_sur_zone = IFNULL(I2_Nb_dagences_sur_zone, 0) + 1
    WHERE I2_RefInterm_indirect = NEW.I_RefGroupe
      AND I2_Categorie = 'Réseau d''agences';
  END IF;
END$$

CREATE TRIGGER intermediaires_directs_reseau_nb_ad
AFTER DELETE ON intermediaires_directs
FOR EACH ROW
BEGIN
  IF OLD.I_Actif = 1 AND OLD.I_RefGroupe IS NOT NULL AND OLD.I_RefGroupe <> 0 THEN
    UPDATE intermediaires_indirects
    SET I2_Nb_dagences_sur_zone = GREATEST(IFNULL(I2_Nb_dagences_sur_zone, 0) - 1, 0)
    WHERE I2_RefInterm_indirect = OLD.I_RefGroupe
      AND I2_Categorie = 'Réseau d''agences';
  END IF;
END$$

CREATE TRIGGER intermediaires_indirects_reseau_nb_bi
BEFORE INSERT ON intermediaires_indirects
FOR EACH ROW
BEGIN
  IF NEW.I2_Categorie = 'Réseau d''agences' THEN
    IF NEW.I2_RefInterm_indirect IS NULL OR NEW.I2_RefInterm_indirect = 0 THEN
      SET NEW.I2_Nb_dagences_sur_zone = 0;
    ELSE
      SET NEW.I2_Nb_dagences_sur_zone = (
        SELECT COUNT(*)
        FROM intermediaires_directs a
        WHERE a.I_RefGroupe = NEW.I2_RefInterm_indirect
          AND a.I_Actif = 1
      );
    END IF;
  END IF;
END$$

CREATE TRIGGER intermediaires_indirects_reseau_nb_bu
BEFORE UPDATE ON intermediaires_indirects
FOR EACH ROW
BEGIN
  IF NEW.I2_Categorie = 'Réseau d''agences'
     AND NEW.I2_RefInterm_indirect IS NOT NULL
     AND NEW.I2_RefInterm_indirect <> 0 THEN
    SET NEW.I2_Nb_dagences_sur_zone = (
      SELECT COUNT(*)
      FROM intermediaires_directs a
      WHERE a.I_RefGroupe = NEW.I2_RefInterm_indirect
        AND a.I_Actif = 1
    );
  END IF;
END$$

DELIMITER ;

-- Contrôle : 0 écart attendu.
SELECT COUNT(*) AS reseaux,
       SUM(r.I2_Nb_dagences_sur_zone <> (
         SELECT COUNT(*)
         FROM intermediaires_directs a
         WHERE a.I_RefGroupe = r.I2_RefInterm_indirect
           AND a.I_Actif = 1
       )) AS ecarts
FROM intermediaires_indirects r
WHERE r.I2_Categorie = 'Réseau d''agences';

-- Retour arrière (ne pas lancer avec le script) :
-- DROP TRIGGER IF EXISTS intermediaires_directs_reseau_nb_ai;
-- DROP TRIGGER IF EXISTS intermediaires_directs_reseau_nb_au;
-- DROP TRIGGER IF EXISTS intermediaires_directs_reseau_nb_ad;
-- DROP TRIGGER IF EXISTS intermediaires_indirects_reseau_nb_bi;
-- DROP TRIGGER IF EXISTS intermediaires_indirects_reseau_nb_bu;
-- UPDATE intermediaires_indirects r
-- INNER JOIN intermediaires_indirects_nb_agences_bak_20261029 b
--   ON b.I2_RefInterm_indirect = r.I2_RefInterm_indirect
-- SET r.I2_Nb_dagences_sur_zone = b.I2_Nb_dagences_sur_zone;
-- DROP TABLE intermediaires_indirects_nb_agences_bak_20261029;
