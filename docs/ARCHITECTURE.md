# Architecture locale (sans prod)

La prod sert toujours l’ancien binaire. Ici on **extrait** une logique propre **derrière** les mêmes controllers HTTP.

```
Controllers/              → adaptateurs HTTP (routes + JSON figés)
  QueryEntitiesController → GET listes catalogue (select/where/orderby/skip/take)
Application/              → cas d’usage SOLID (un dossier = une capacité)
  Authentication/         → login, SwitchDispo, SwitchFilter
  Files/                  → upload / download (contrat isSuccess / errors / fullPath)
  Catalog/                → requêtes listes EF (EntityQuery)
  Events/                 → EncaissementsEnCours (home tab 9)
Infrastructure/
  Files/LocalFileStorage  → disque (port IFileStorage)
Services/ + Models/       → infra / EF historiques (ports progressivement)
```

Règles :

- Un controller ne contient plus le mapping métier : il délègue à un use case.
- Les DTO HTTP (`UserModel`, `AuthenticateModel`, `FileResponse`) ne bougent pas.
- Les entités EF restent dans la couche infra ; le front ne les voit pas.
- Le stockage fichiers passe par `IFileStorage` (pas de `System.IO` dans le use case).
- Nouveau module = use case + mapper/port + tests, **sans** changer l’URL.

Prod : on ne bascule que sur créneau explicite.
