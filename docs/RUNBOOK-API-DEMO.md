# Runbook — API démo (isolée)

**URL :** https://dev.perle-rare.info/api-demo/  
**Swagger :** https://dev.perle-rare.info/api-demo/swagger/index.html  
**Processus :** `127.0.0.1:5080` · unit `api-demo.perle-rare.info.service`  
**Fichiers :** `/home/administrateur/api-client-demo` (disque `/home`, pas `/`)

Le front démo `https://dev.perle-rare.info/demo/` **n’est pas branché** sur cette API. Il continue d’appeler `https://api.perle-rare.info`.

## Ce qui est isolé

| | Prod | Démo |
|---|---|---|
| Dossier | `/var/www/api.perle-rare.info` | `/home/administrateur/api-client-demo` |
| Port | `127.0.0.1:5000` | `127.0.0.1:5080` |
| systemd | `api.perle-rare.info.service` | `api-demo.perle-rare.info.service` |
| Jobs Yanport / mails | oui | **off** (`PR_LOCAL_SAFE=1`) |
| MariaDB | user applicatif | `cursor_client` **SELECT only** |
| Fichiers upload | site legacy | `api-client-demo/uploads` |
| JWT | secret prod | secret local dummy |

## Déployer

```bash
bash scripts/deploy-api-demo.sh
```

Le script refuse d’écrire hors `api-client-demo`, vérifie l’espace (`/home` > 500 Mo, `/` > 200 Mo), republie en **self-contained** (pas d’install .NET 8 sur le VPS), et contrôle que le SHA de la DLL prod + le PID Kestrel prod n’ont pas bougé.

## Appeler depuis ici

```bash
# contrat login (400 attendu)
curl -sS -H 'Content-Type: application/json' \
  -d '{"login":"x","password":"y"}' \
  https://dev.perle-rare.info/api-demo/api/User/authenticate

# login réel puis GET
TOKEN=$(curl -sS -H 'Content-Type: application/json' \
  -d '{"login":"VOTRE_LOGIN","password":"VOTRE_MDP"}' \
  https://dev.perle-rare.info/api-demo/api/User/authenticate \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")

curl -sS -H "Authorization: Bearer $TOKEN" \
  'https://dev.perle-rare.info/api-demo/api/TypesTaches?take=5'
```

Les écritures métier (POST/PUT, SwitchDispo) échoueront côté SQL : compte SELECT only. C’est voulu.

## Ne pas faire

- `systemctl restart api.perle-rare.info`
- rsync vers `/var/www/api.perle-rare.info`
- pointer le front `/demo/` vers `/api-demo/`
- installer un runtime .NET sur `/` (disque à 86 %)
