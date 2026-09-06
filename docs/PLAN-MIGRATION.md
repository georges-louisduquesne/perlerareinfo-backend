# Plan de migration API — suivi

Source : [PROPOSITION-API-BACKEND.md](./PROPOSITION-API-BACKEND.md) + [AUDIT-API-BACKEND-2026-08.md](./AUDIT-API-BACKEND-2026-08.md).  
Contrainte : **mêmes endpoints, mêmes signatures** ; le front Angular ne bouge pas.  
Prod : non touchée jusqu’à créneau validé.

Légende : `pending` · `in_progress` · `local_done` · `blocked` (créneau client)

## Semaine 1 — Fermer les portes

| # | Livrable | Statut | Notes contrat |
|---|----------|--------|----------------|
| 1 | Masquer Swagger + outils test / maintenance | local_done | Swagger uniquement Dev / PR_LOCAL_SAFE |
| 2 | Auth obligatoire zones trop ouvertes | local_done | `[Authorize]` PropertyContacts ; Health/Test Admin |
| 3 | Restreindre Db + File | local_done | Db → Admin ; File : JSON identique, path canonique |
| 4 | Erreurs sans stack / secret | local_done | File / Test / Db : message générique |
| 5 | TLS sortant (plus de trust-all) | local_done | Callbacks retirés Exchange + old site |
| 6 | Filtres SQL paramétrés / échappés | local_done | Quote/ident allowlist ; syntaxe filtres inchangée |
| 7 | Rotation identifiants | blocked | Créneau 1–2 h client |
| 8 | Recette CRM démo | pending | Après palier local |

## Semaine 2 — Socle

| # | Livrable | Statut | Notes contrat |
|---|----------|--------|----------------|
| 9 | Dépôt source maintenable | local_done | Décompilé + build net8 |
| 10 | Secrets hors git, build reproductible | local_done | `.gitignore` appsettings réels |
| 11 | .NET 8 LTS | local_done | Target `net8.0` local |
| 12 | Hash mots de passe (bascule douce) | pending | `User/authenticate` inchangé |
| 13 | JWT plus courts / cadrés | blocked | Invalide les sessions → créneau |
| 14 | Scheduler `try/finally` + pas de gel | local_done | `_executing` toujours relâché |
| 15 | Droits plus raisonnables | pending | Grep front avant tout 403 nouveau |
| 16 | Plafond pagination + liveness réel | pending | Ne pas casser `take` actuel sans check front |
| 17 | Logs + runbook | pending | Doc seulement |

## Recette anti-régression (après chaque palier)

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test tests/ApiPerleRare.Tests/ApiPerleRare.Tests.csproj
dotnet build api-perle-rare-decompiled/ApiPerleRare.csproj
curl -sS http://localhost:5080/swagger/index.html   # 200 en local only
# authenticate + GET ContactsRecherches / TypesTaches avec Bearer (compte démo)
```

Comparer : même HTTP status métier, mêmes clés JSON. Pas de 401 sur une route que le front appelle déjà avec token.
