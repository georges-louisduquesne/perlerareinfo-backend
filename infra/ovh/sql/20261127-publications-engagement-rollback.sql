-- Retour arrière de 20261127-publications-engagement.sql
-- La copie publications_reseaux_bak_20261127_engagement conserve les lignes d'avant l'ajout.

ALTER TABLE publications_reseaux
  DROP COLUMN PUB_Likeurs,
  DROP COLUMN PUB_Commentaires,
  DROP COLUMN PUB_Likes;
