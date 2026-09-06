# Agent notes — pearlrare-backend

Suivre la règle Cursor **alwaysApply** :

`.cursor/rules/perlerare-backend-workflow.mdc`

En résumé : rappeler commit/push ; **jamais** écrire sur le VPS ni déployer l’API prod sans ordre explicite ; MariaDB = SELECT only ; secrets hors git.
