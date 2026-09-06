# OVH — Perle Rare VPS (backend)

Connexion et exploration **en lecture seule** du serveur qui héberge `api.perle-rare.info`.

## Serveur

| Champ | Valeur |
|-------|--------|
| **IP** | `142.4.216.57` |
| **Hostname** | `ns5000052.ip-142-4-216.net` |
| **Région** | `ca-east-bhs` (Brossard, Canada) |
| **OS** | Debian 11 (bullseye) |
| **SSH** | port **2477** (pas 22) |
| **Utilisateur** | `administrateur` |
| **Clé SSH** | `git.key` à la racine du dépôt (non versionnée) |

Inventaire : [`servers.json`](./servers.json)  
Snapshot : [`inventory/server-snapshot.md`](./inventory/server-snapshot.md)

## Connexion rapide

```bash
./infra/ovh/scripts/ssh.sh
ssh -F infra/ovh/ssh/config perlerare-ovh
```

Variables optionnelles : `SSH_KEY`, `OVH_HOST`, `OVH_PORT`, `OVH_USER`.

## Explorer (lecture seule)

```bash
./infra/ovh/scripts/explore.sh
```

## Ce qui tourne (aperçu)

| Service | Détail |
|---------|--------|
| **Apache** | vhost `api.perle-rare.info` → proxy HTTPS 443 |
| **Kestrel** | binaire `/var/www/api.perle-rare.info/ApiPerleRare` sur `127.0.0.1:5000` |
| **MariaDB** | port **17075** (user Cursor : SELECT only) |
| **Git** | bare repo `api.perle-rare.info.git` |

```
/var/www/api.perle-rare.info/     # API .NET live
/home/administrateur/api.perle-rare.info.git
```

## Cursor ↔ MariaDB (read-only)

Guide : [`cursor-client/README.md`](./cursor-client/README.md)

- User : `cursor_client` (SELECT only on `perle-rareinfo`)
- Port : `17075`
- MCP : `@benborla29/mcp-server-mysql`

## Structure

```
infra/ovh/
├── servers.json
├── cursor-client/
├── inventory/server-snapshot.md
├── scripts/          # ssh.sh, explore.sh, connect.sh
├── secrets/          # gitignored
└── ssh/config
```

## Règle d’or

Aucun write, restart, rsync sortant, ni hook git. Ce dépôt sert à travailler **en local**.
