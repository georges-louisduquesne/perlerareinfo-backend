-- Mesures de temps de chargement de l’accueil Angular (cockpit admin).
-- Création seule : pas de DROP.

CREATE TABLE IF NOT EXISTS home_page_loads (
  HPL_Ref bigint NOT NULL AUTO_INCREMENT,
  HPL_RefConseiller int(10) unsigned NOT NULL,
  HPL_DurationMs int unsigned NOT NULL,
  HPL_MeasuredAt datetime NOT NULL,
  PRIMARY KEY (HPL_Ref),
  KEY IX_home_page_loads_measured (HPL_MeasuredAt),
  KEY IX_home_page_loads_conseiller (HPL_RefConseiller)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
