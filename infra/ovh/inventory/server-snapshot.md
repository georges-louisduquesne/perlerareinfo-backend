# Server snapshot — 2026-07-20

Explored via SSH (`administrateur@142.4.216.57:2477`).

## System

| Item | Value |
|------|--------|
| Hostname | `ns5000052.ip-142-4-216.net` |
| Kernel | Linux 5.10.0-28-amd64 (Debian) |
| OS | Debian GNU/Linux 11 (bullseye) |
| RAM | ~23 GiB (heavy use, swap full) |
| Disk `/` | 20G — **86% used** |
| Disk `/home` | 91G — 59% used |

## Web stack

- **Apache 2.4** — active
- **nginx** — inactive
- **Docker** — not running
- **.NET / Kestrel** — API on HTTPS (443), not visible in `ps` at probe time (likely behind reverse proxy)

### Apache vhosts enabled

- `dev.perle-rare.info`
- `prod.perle-rare.info`
- `api.perle-rare.info`
- `old.perle-rare.info` (SSL)
- `perle-rare.info`
- `perle-rare.com` (+ SSL)
- `phpmyadmin` (SSL)

## `/var/www`

| Path | Role |
|------|------|
| `dev.perle-rare.info/` | Front Angular — dev |
| `prod.perle-rare.info/` | Front Angular — prod |
| `api.perle-rare.info/` | API .NET |
| `api.perle-rare.info-old-*` | API backups (Jun 2026) |
| `perle-rare` → `/home/web_perlerare_info/public_html` | Legacy site |
| `perle-rare.com` → `/home/web_perlerare_com/public_html/` | Legacy .com |
| `phpmyadmin` | DB admin |
| `webmail` | Horde webmail |

## `/home` users & dirs

- `administrateur` — deploy user, git repos, crons, backups
- `web_perlerare_info` / `web_perlerare_com` — legacy web roots
- `mysql` — database data
- `git` — additional git repos
- `vmail` — mail storage

## Git bare repos (`/home/administrateur`)

- `dev.perle-rare.info.git` — front repo (this project)
- `api.perle-rare.info.git` — API repo

## DNS (all → 142.4.216.57)

- `perle-rare.info`
- `api.perle-rare.info`
- `dev.perle-rare.info`
- `old.perle-rare.info`

## TLS

Let's Encrypt cert on `api.perle-rare.info` covering `*.perle-rare.info` SANs (api, dev, old, www, phpmyadmin, webmail, com).
