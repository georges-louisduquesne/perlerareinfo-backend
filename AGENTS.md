# Agent notes — pearlrare-backend

Règles Cursor **alwaysApply** :

1. `.cursor/rules/perlerare-client-workflow.mdc` — toujours `main` ; sync **back + front** (sibling) en début de session ; commit/push avec **git local** (jamais auteur injecté) ; après palier demander puis **push `origin main`** ; API / MariaDB / VPS seulement sur ordre explicite.
2. `.cursor/rules/perlerare-backend-workflow.mdc` — contrat JSON / routes gelé (CRM Angular drop-in).

Secrets hors git (`git.key`, `appsettings*.json` réels). `cursor_client` = SELECT only tant qu’on n’a pas d’ordre SQL écrit.
