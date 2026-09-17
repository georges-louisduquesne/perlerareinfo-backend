# Compte MariaDB pour modifications (ALTER / soft-hash / démo DELETE ciblé)

## Situation actuelle

| Compte | Usage | Droits |
|--------|--------|--------|
| `cursor_client` | MCP Cursor + API locale | **SELECT only** sur `perle-rareinfo` |
| `pr_api_demo` | API démo uniquement | **SELECT** sur `perle-rareinfo.*` + **DELETE** sur `property_contact` seulement |
| `root` (vu dans appsettings API prod) | API prod live | Full — à remplacer au créneau secrets |

Ne **pas** élargir `cursor_client`. Le MCP reste lecture seule.

`administrateur` a `sudo` et `/etc/mysql/debian.cnf` existe → on peut créer un user dédié **sans** coller le mot de passe root dans git.

MariaDB ne peut pas limiter un `DELETE` au `WHERE PC_RefContact = …`. La clôture est double :

1. GRANT : `DELETE` uniquement sur `property_contact` (pas les biens, pas les contacts).
2. API : n’accepte que `where=PC_RefContact=<id>` puis SQL paramétré (voir `PropertyContactBulkDelete`).

Mot de passe démo : `infra/ovh/secrets/api-demo-db.env` (gitignored). Template SQL : [pr-api-demo-user.sql](./pr-api-demo-user.sql).

## Objectif

Un user **étroit** pour :
1. `ALTER` `CP_MotDePasse` → `VARCHAR(255)` ([widen-cp-mot-de-passe.sql](./widen-cp-mot-de-passe.sql))
2. Plus tard : `UPDATE` des hash à la connexion (soit via l’API prod avec un user app, soit ce même user si on le branche)

## Création recommandée (à lancer sur le VPS en root MySQL)

À faire **manuellement** (créneau / accord client), mdp fort hors git :

```sql
-- Remplacer CHANGE_ME par un secret fort (coffre, pas mail).
CREATE USER IF NOT EXISTS 'pr_migrate'@'localhost' IDENTIFIED BY 'CHANGE_ME';
CREATE USER IF NOT EXISTS 'pr_migrate'@'127.0.0.1' IDENTIFIED BY 'CHANGE_ME';

GRANT SELECT, ALTER
  ON `perle-rareinfo`.`conseillers_personnels`
  TO 'pr_migrate'@'localhost', 'pr_migrate'@'127.0.0.1';

-- Si soft-hash doit écrire via ce compte (sinon laisser l’API prod / futur user app) :
GRANT UPDATE (`CP_MotDePasse`, `CP_AutoLogin`)
  ON `perle-rareinfo`.`conseillers_personnels`
  TO 'pr_migrate'@'localhost', 'pr_migrate'@'127.0.0.1';

FLUSH PRIVILEGES;
```

Puis :

```bash
mysql -u pr_migrate -p perle-rareinfo < docs/sql/widen-cp-mot-de-passe.sql
```

Vérif :

```sql
SHOW COLUMNS FROM conseillers_personnels LIKE 'CP_MotDePasse';
-- Type attendu : varchar(255)
```

## Chemins possibles

1. **Minimal (recommandé maintenant)** : créer `pr_migrate`, appliquer seulement l’`ALTER`, garder la démo en SELECT-only. Soft-hash effectif = quand une API **writable** (prod) tourne avec le nouveau code.
2. **User app propre** (créneau secrets) : remplacer `root` dans la connection string prod par un user `pr_api` (SELECT/INSERT/UPDATE/DELETE métier, pas DROP), et y inclure `ALTER` une fois ou via `pr_migrate`.
3. **Ne pas** donner `ALTER` ni `DELETE` à `cursor_client` (casse le modèle lecture seule Cursor).

## Ne pas faire

- Committer mdp / connection string réelle
- Élarsir les grants de `cursor_client`
- GRANT `DELETE` sur une autre table que `property_contact` pour `pr_api_demo`
- Appliquer l’`ALTER` sans backup / note d’empreinte si on est juste avant cutover
