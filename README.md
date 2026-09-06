# Perle Rare · API

Backend métier `api.perle-rare.info` — ASP.NET Core / .NET 7, EF Core + MariaDB.

Ce dépôt reprend la partie backend qui vivait dans `pearlrare-front` (infra OVH, audit, sources décompilées, publish de référence). **La production n’est pas touchée** : setup local + lectures VPS uniquement.

## Stack

| Couche | Détail |
|--------|--------|
| Runtime live | .NET 7 / Kestrel `127.0.0.1:5000` derrière Apache 443 |
| ORM | EF Core 7 + Pomelo MySQL |
| Auth | JWT Bearer |
| Base | MariaDB `perle-rareinfo` (port public **17075**, user Cursor = SELECT only) |
| Front | Angular → `https://api.perle-rare.info/api/` |

## Structure

```
├── api-perle-rare-decompiled/   # C# reconstitué (ILSpy) — base de travail
├── api-perle-rare-publish/      # publish git (binaires, gitignored)
├── api-perle-rare-live/         # empreinte DLL live optionnelle (gitignored)
├── docs/                        # audit + proposition API
├── infra/ovh/                   # SSH, inventaire, MCP MariaDB
├── git.key                      # clé SSH locale (gitignored)
└── .cursor/                     # règle agent + MCP (mcp.json gitignored)
```

## Lancer en local (sans jobs prod)

Le SDK **.NET 8** est dans `~/.dotnet` (le `dotnet` système en x64 3.1/6.0 ne suffit pas).

```bash
export PATH="$HOME/.dotnet:$PATH"
./scripts/run-local.sh
```

Puis ouvrir [http://localhost:5080/swagger](http://localhost:5080/swagger).

Mode `PR_LOCAL_SAFE=1` : pas de tâches planifiées (Yanport / mails / UPDATE), pas de redirection HTTPS.  
La base utilisée est MariaDB **lecture seule** (`cursor_client`). Les POST/PUT échoueront côté droits SQL — c’est voulu.

Tests (contrat front + helpers sécu) :

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test tests/ApiPerleRare.Tests/ApiPerleRare.Tests.csproj
```

Smoke manuel : Swagger local [http://localhost:5080/swagger](http://localhost:5080/swagger). `GET /api/Test/info` est désormais **401** sans token Admin.

## Prérequis locaux

- Clé `git.key` à la racine (déjà copiée depuis le front, non versionnée)
- OpenSSH
- Optionnel : SDK **.NET 7** ou **8** pour compiler (machine actuelle : SDKs 3.1 / 6.0 seulement)
- Optionnel : Node 20+ pour le MCP MariaDB Cursor

## Connexion VPS (lecture seule)

```bash
# Shell
./infra/ovh/scripts/ssh.sh

# Ou config OpenSSH
ssh -F infra/ovh/ssh/config perlerare-ovh

# Audit rapide (ls / ps / df — aucun write)
./infra/ovh/scripts/explore.sh
```

| Champ | Valeur |
|-------|--------|
| IP | `142.4.216.57` |
| SSH | port **2477**, user `administrateur` |
| API | `https://api.perle-rare.info` |
| Deploy live | `/var/www/api.perle-rare.info` |
| Git bare | `/home/administrateur/api.perle-rare.info.git` |

Variables optionnelles : `SSH_KEY`, `OVH_HOST`, `OVH_PORT`, `OVH_USER`.

## Smoke HTTP (sans auth)

```bash
curl -sS https://api.perle-rare.info/api/Test/info
# attendu : DBName=perle-rareinfo
```

Swagger public (à fermer plus tard) : `https://api.perle-rare.info/swagger`

## MariaDB lecture seule

User `cursor_client` — **SELECT only** sur `perle-rareinfo`.

1. Mot de passe dans `infra/ovh/secrets/cursor-client.env` (local)
2. MCP Cursor : `.cursor/mcp.json` (déjà copié, gitignored)
3. Redémarrer Cursor, puis : « liste les tables de perle-rareinfo »

Guide : `infra/ovh/cursor-client/README.md`

## Config API locale (à créer, ne pas committer)

Les `appsettings*.json` du publish contiennent des secrets. Pour un run local, copier l’exemple :

```bash
cp api-perle-rare-decompiled/appsettings.example.json \
   api-perle-rare-decompiled/appsettings.Development.json
```

Clés attendues (valeurs hors git) :

- `AppSettings:Secret` — JWT
- `AppSettings:YanportToken`
- `AppSettings:OldWebSiteFolder`
- `ConnectionStrings:PerleRareDB`
- `ConnectionStrings:Exchange`

## Dérive de version (important)

| Artefact | État constaté (2026-09-06, lecture) |
|----------|--------------------------------------|
| Git bare `HEAD` | `fe9c3cf` (2024-12-24) — même publish que `api-perle-rare-publish/` (~1,0 Mo) |
| DLL live | `/var/www/api.perle-rare.info/ApiPerleRare.dll` ~1,2 Mo, mtime **1 sept. 2026** |
| Processus | `www-data` exécute le binaire self-contained, Apache proxy → `:5000` |

Le C# décompilé reflète le publish git, **pas** forcément le binaire live. Un second pass sur la DLL live reste à faire avant tout hotfix prod.

## Dépôt Git

Le dépôt est **local** (`main`). Quand le remote existera :

```bash
git remote add origin git@github.com:USER/pearlrare-backend.git
git push -u origin main
```

Remplacer l’URL par celle de GitHub / GitLab. Aucun remote n’est configuré pour l’instant.

## Ce que ce setup ne fait pas

- Aucun write SSH, aucun restart Kestrel/Apache
- Aucun deploy API
- Aucune rotation de secrets
- Pas encore de build local (SDK 7/8 manquant sur cette machine)

## Docs

- [Audit API août 2026](docs/AUDIT-API-BACKEND-2026-08.md)
- [Proposition remise à niveau](docs/PROPOSITION-API-BACKEND.md)
- [OVH / VPS](infra/ovh/README.md)
