# Architecture locale (sans prod)

La prod sert toujours l’ancien binaire. Ici on **extrait** une logique propre **derrière** les mêmes controllers HTTP.

```
Controllers/          → adaptateurs HTTP (routes + JSON figés)
Application/          → cas d’usage SOLID (un dossier = une capacité)
  Authentication/     → premier module
Services/ + Models/   → infra / EF historiques (ports progressivement)
```

Règles :

- Un controller ne contient plus le mapping métier : il délègue à un use case.
- Les DTO HTTP (`UserModel`, `AuthenticateModel`) ne bougent pas.
- Les entités EF (`ConseillersPersonnels`) restent dans la couche infra ; le front ne les voit pas.
- Nouveau module = use case + mapper + tests, **sans** changer l’URL.

Prod : on ne bascule que sur créneau explicite.
