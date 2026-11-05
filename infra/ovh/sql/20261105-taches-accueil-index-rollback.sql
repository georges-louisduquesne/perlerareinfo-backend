-- Rollback index accueil alertes taches
DROP INDEX IX_taches_accueil_echeance ON taches;
DROP INDEX IX_taches_accueil_qui_echeance ON taches;
