# Agent notes — pearlrare-backend

Règles Cursor **alwaysApply** :

1. `.cursor/rules/perlerare-client-workflow.mdc` — à **chaque** demande de travail : sync **back + front** sur `main` avant de coder ; jamais de branche ; à chaque avancée tests verts puis **commit + push `origin main`** automatiquement (identité Git locale, jamais d’auteur injecté) ; **`go demo`** = démo, **`go prod`** = prod propre (API v2 puis front, contrôles, rollback si échec). Chaque correctif / fonction arrive avec ses **tests xUnit** (bug → test qui le reproduit d’abord) ; hook `pre-push` (`git config core.hooksPath .githooks`) et scripts de déploiement refusent tests rouges, version non poussée ou non passée en démo.
2. `.cursor/rules/perlerare-backend-workflow.mdc` — contrat JSON / routes gelé (CRM Angular drop-in).

Secrets hors git (`git.key`, `appsettings*.json` réels). Ancienne API, écritures MariaDB et secrets : seulement sur ordre écrit explicite.
