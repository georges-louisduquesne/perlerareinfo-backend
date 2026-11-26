-- Retour arrière de 20261126-reseaux-sociaux.sql
-- Les sollicitations et modèles créés après la mise en place sont perdus.
-- Les quotas posés sur les conseillers sont retirés. La sauvegarde
-- conseillers_personnels_bak_20261126_quota_reseaux ne contient que la liste
-- des conseillers d'avant l'ajout (les colonnes n'existaient pas).

ALTER TABLE conseillers_personnels
  DROP COLUMN CP_Solde_Quota,
  DROP COLUMN CP_Quota_Reseaux;

DROP TABLE IF EXISTS sollicitations_reseaux;
DROP TABLE IF EXISTS publications_reseaux;
DROP TABLE IF EXISTS modeles_reseaux;
