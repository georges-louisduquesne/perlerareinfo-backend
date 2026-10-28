-- Formulaires candidats reçus sur contact@perle-rare.com
-- (sujet « Nouveau formulaire de recrutement via réseaux sociaux »).
-- Création seule : pas de DROP. Retour arrière des 2 mails rattrapés :
-- 20261027-formulaires-candidats-rollback.sql

CREATE TABLE IF NOT EXISTS formulaires_candidats (
  FC_Ref int(11) NOT NULL AUTO_INCREMENT,
  FC_MessageId varchar(255) NOT NULL,
  FC_Date datetime NOT NULL,
  FC_From varchar(255) NOT NULL DEFAULT '',
  FC_Subject varchar(255) NOT NULL DEFAULT '',
  FC_Prenom varchar(80) NOT NULL DEFAULT '',
  FC_Nom varchar(80) NOT NULL DEFAULT '',
  FC_Tel varchar(40) NOT NULL DEFAULT '',
  FC_Email varchar(150) NOT NULL DEFAULT '',
  FC_Ville varchar(150) NOT NULL DEFAULT '',
  FC_Linkedin varchar(300) NOT NULL DEFAULT '',
  FC_Pourquoi text NOT NULL,
  FC_Delai varchar(20) NOT NULL DEFAULT '',
  FC_Corps mediumtext NOT NULL,
  FC_Statut smallint(6) NOT NULL DEFAULT 1,
  FC_CauseRejet varchar(20) DEFAULT NULL,
  FC_RefAdmin int(10) unsigned DEFAULT NULL,
  FC_RefConseiller int(10) unsigned DEFAULT NULL,
  FC_DateTraitement datetime DEFAULT NULL,
  PRIMARY KEY (FC_Ref),
  UNIQUE KEY UK_formulaires_candidats_message (FC_MessageId),
  KEY IX_formulaires_candidats_statut (FC_Statut)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
