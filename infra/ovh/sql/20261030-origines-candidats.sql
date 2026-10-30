-- Origines des candidats + champ vide sur conseillers_personnels.
-- Contrôle avant (2026-10-30) : table absente, colonne absente, 361 lignes.
-- Les triggers (ville, adresse, téléphone) ne lisent pas CP_Origine.
-- Jeu de caractères de la table conseillers : latin1 / latin1_swedish_ci.
--
-- Retour arrière, seulement tant que CP_Origine est encore entièrement vide :
--   infra/ovh/sql/20261030-origines-candidats-rollback.sql

CREATE TABLE IF NOT EXISTS origines_candidats (
  OC_Nom varchar(50) NOT NULL,
  PRIMARY KEY (OC_Nom)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_swedish_ci;

INSERT IGNORE INTO origines_candidats (OC_Nom) VALUES
  ('Hugo Bolzinger'),
  ('LinkedIn'),
  ('Contact personnel'),
  ('Autre');

ALTER TABLE conseillers_personnels
  ADD COLUMN CP_Origine varchar(50)
    CHARACTER SET latin1
    COLLATE latin1_swedish_ci
    NULL
    DEFAULT NULL,
  ALGORITHM=INSTANT;

ALTER TABLE conseillers_personnels
  ADD CONSTRAINT FK_conseillers_personnels_origines_candidats
    FOREIGN KEY (CP_Origine) REFERENCES origines_candidats (OC_Nom);
