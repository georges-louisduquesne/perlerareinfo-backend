# Architecture locale (sans prod)

La prod sert toujours l’ancien binaire. Ici on **extrait** une logique propre **derrière** les mêmes controllers HTTP.

```
Controllers/                 → adaptateurs HTTP (routes + JSON figés)
  QueryEntitiesController    → GET listes catalogue (select/where/orderby/skip/take)
Application/                 → cas d’usage SOLID (un dossier = une capacité)
  Abstractions/              → IApplicationDbContext (port EF)
  Authentication/            → login, SwitchDispo, SwitchFilter
  Files/                     → upload / download (+ IFileStorage)
  Catalog/                   → requêtes listes EF (EntityQuery)
  Events/                    → home listes + OffresEnCours + Encaissements
  Tasks/                     → listes / counts tâches (enrichissement PS)
  Contacts/                  → ContactsRecherche Ex / Accueil / CRUD métier
  Annonces/                  → listes, RecupInfos*, ViderInfos
  Exchange/                  → mail + calendrier EWS (mailbox lookup)
  Search/                    → recherche globale CRM
Infrastructure/
  Files/LocalFileStorage     → disque
  Mail/LocalhostMailService  → SMTP localhost (ILocalhostMailService)
  Yanport/*                  → HTTP Yanport (IYanportService)
Services/                    → adaptateurs restants (Exchange EWS, User, Search SQL…)
```

Règles :

- Un controller ne contient plus le mapping métier : il délègue à un use case.
- Les DTO HTTP ne bougent pas (formes JSON / routes gelées).
- Les use cases dépendent de **ports** (`IApplicationDbContext`, `IFileStorage`, `IExchangeService`, `ILocalhostMailService`, `IYanportService`, `ISearchService`) — pas de `System.IO` / SMTP / Yanport HTTP dans le controller.
- Nouveau module = use case + port + tests, **sans** changer l’URL.

CRUD mince (Put/Post/Delete Get-by-id) peut rester sur `ApplicationDbContext` dans le controller tant que le front n’est pas impacté ; les listes métier sont dans Application/.

Prod : on ne bascule que sur créneau explicite.
