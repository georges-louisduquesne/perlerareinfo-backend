# Audit API backend — ApiPerleRare

**Date :** 17 août 2026  
**Périmètre :** code C# décompilé (`api-perle-rare-decompiled/`) + publish git (`api-perle-rare-publish/`) + contrôle croisé live (`https://api.perle-rare.info`)  
**Stack :** ASP.NET Core / .NET 7, EF Core 7 + Pomelo MySQL, JWT Bearer, Swashbuckle, Exchange EWS, Yanport, tâches planifiées  
**Méthode :** revue statique du décompilé (ILSpy 9 + PDB), inventaire Swagger live, smoke HTTP limité (`/api/Test/info`), comparaison taille DLL git vs `/var/www/api.perle-rare.info`. Complété par quatre revues parallèles (sécurité, données, cartographie, architecture).

> **Important — dérive de version.**  
> Le dépôt bare `api.perle-rare.info.git` (HEAD `fe9c3cf`) contient un publish **plus ancien** que le binaire déployé (DLL live ~1,21 Mo du 23/07/2026 vs ~1,05 Mo dans le git). Le Swagger live expose des routes absentes du décompilé (`/api/Messaging/TestNotify`, `/api/Health/Version|Status|TestEmails|CheckDateFin/{mdp}`).  
> Les constats ci-dessous restent valides pour le cœur du produit ; les items marqués **LIVE** sont confirmés sur la prod actuelle. Un second pass sur la DLL live est recommandé dès qu’on peut la rappatrier.

---

## 1. Synthèse executive

| Indicateur | Valeur |
|---|---|
| Fichiers `.cs` décompilés | ~280 |
| Contrôleurs HTTP | 33 classes + DTOs annexes |
| Opérations Swagger live | **185** sur **133** paths |
| Sévérité Critical | **9** |
| High | **12** |
| Medium | **10** |
| Low / dette | **8+** |
| Verdict | **Non prêt pour un audit de conformité / reprise sereine** sans correctifs P0 |

L’API est un monolithe métier riche (contacts, annonces, Exchange, Yanport, health/repair) avec une **surface d’attaque trop large** : endpoints de maintenance non authentifiés, CRUD générique, mots de passe en clair, Swagger public, et secrets dans les fichiers de déploiement.

### Score par domaine (0–5)

| Domaine | Score | Commentaire |
|---|---:|---|
| Authentification | 1/5 | Plaintext, JWT permissif, pas de rate-limit |
| Autorisation | 1/5 | Controllers métier publics ; `DbController` trop puissant |
| Secrets / config | 1/5 | Secret JWT, DB, Exchange, Yanport dans `appsettings*` versionnés côté publish |
| Injection / données | 1/5 | SQLi certaine sur filtres annonces + interpolations Health/PropertyContacts |
| Fichiers / réseau | 1/5 | Path traversal upload ; SSRF images ; TLS trust-all |
| Observabilité | 2/5 | Logs partiels, stack traces renvoyées au client |
| Maintenabilité | 2/5 | Pas de sources git, .NET 7 EOL, jobs lourds |
| Exploitabilité live | 4/5 | Swagger + Test/info + Health ouverts |

---

## 2. Inventaire technique

### Architecture

```
Program.cs  → host web + CLI (-list / -execute tasks)
Startup.cs  → DI, JWT, CORS, Swagger, Session, hosted services
Controllers → surface HTTP /api/*
Services    → User, Search, Audit, Exchange, Yanport, WebSite, queues
Models      → ApplicationDbContext (EF Core MySQL)
ScheduledTasks / Tasks → jobs périodiques (timer 30s)
Predicates / Orderbys / RecupInfos → DSL filtres dynamiques
```

### Dépendances notables (publish)

| Package | Version | Note |
|---|---|---|
| Target framework | **net7.0** | EOL depuis **14 mai 2024** ([annonce Microsoft](https://devblogs.microsoft.com/dotnet/dotnet-7-end-of-support/)) |
| EF Core | 7.0.13 | Aligné runtime 7 |
| Pomelo.EFCore.MySql | 7.0.0 | |
| MySqlConnector | 2.2.5 | |
| JWT Bearer | 7.0.13 | |
| System.IdentityModel.Tokens.Jwt | **6.24.0** | &lt; 6.34.0 → zone CVE-2024-21319 (DoS JWE) |
| Swashbuckle | 6.5.0 | UI activée en prod |
| Exchange Web Services | 1.1.3 | |
| FirebaseAdmin | 3.1.0 | Présent dans deps (usage à confirmer sur build live) |

### Config (noms de clés uniquement — valeurs non reproduites)

Présentes dans `appsettings.json` / `Production` :

- `AppSettings:Secret` (clé JWT)
- `AppSettings:YanportToken`
- `AppSettings:OldWebSiteFolder`
- `ConnectionStrings:PerleRareDB`
- `ConnectionStrings:Exchange` / `Exchange_old`
- `AllowedHosts`

Ces fichiers font partie du dépôt de **publish** déployé. À traiter comme **compromis jusqu’à rotation**.

---

## 3. Constats Critiques (P0 — traiter immédiatement)

### C1 — Mots de passe stockés et comparés en clair

**Preuve :** `UserService.Authenticate` compare `password != user.CpMotDePasse`.  
`ApplicationDbContext` mappe `CpMotDePasse` en `varchar(15)`.  
Des helpers PBKDF2 existent (`CreatePasswordHash` / `VerifyPasswordHash`) mais **ne sont jamais utilisés**.  
Le modèle expose aussi `CpMelMotDePasse` (mot de passe messagerie) sans `[JsonIgnore]`.

**Impact :** fuite DB = prise de compte CRM + messagerie Exchange des conseillers.  
**Action :** migrer vers hash (Argon2id / PBKDF2/HMAC-SHA256 moderne), masquer les champs sensibles en JSON, forcer reset, ne plus renvoyer les passwords dans les GET admin.

### C2 — `PropertyContactsController` sans authentification

**Preuve :** classe sans `[Authorize]` — GET/PUT/DELETE sur `/api/PropertyContacts` et `/api/contact/{id}/properties`.  
Confirmé dans le Swagger live (pas de `securitySchemes`).

**Impact :** lecture / modification / suppression de liaisons contact↔biens **sans token**.  
**Action :** `[Authorize]` immédiat + contrôle d’ownership / rôle ; audit des appels anonymes dans les logs Apache.

### C3 — `HealthController` : maintenance destructive publique

**Preuve :** aucun `[Authorize]`. Endpoints décompilés :

| Route | Risque |
|---|---|
| `GET .../AnnoncesRefcontacts?action=fix` | UPDATE/DELETE massifs |
| `GET .../Evenements?action=fix` | UPDATE SQL |
| `GET .../Migrate` | UPDATE `property` |
| `GET .../NormalizePhoneNumbers` | UPDATE massif |
| `GET .../FixEncodings` | UPDATE massif |
| `GET .../ContactUrlSearches/{id}` | fuite données |

**LIVE (Swagger, absents du décompilé git) :**  
`/api/Health/Version`, `/Status`, `/TestEmails`, `/CheckDateFin/{mdp}`.

**Impact :** altération de données métier / DoS DB sans compte.  
**Action :** couper immédiatement (Apache deny / retirer controllers) ; n’autoriser qu’en admin + réseau interne ; séparer outils de repair hors API publique.

### C4 — `DbController` : ORM générique pour tout utilisateur authentifié

**Preuve :** `[Authorize]` seul (pas Admin).  
`GET/PUT/POST/DELETE api/Db/{model}/{id}` résout **n’importe quelle entité EF** par nom court, écrit via reflection, renvoie `ex.ToString()`.

**Impact :** un conseiller authentifié peut lire/modifier/supprimer users, mots de passe, audits, etc.  
**Action :** désactiver en prod, ou restreindre à Admin + allowlist de modèles/champs + DTO.

### C5 — Upload / download fichiers : path traversal

**Preuve :** `FileController` combine `OldWebSiteFolder` + `FileName` utilisateur.  
Seule extension bloquée : `.php`. Pas de normalisation / `Path.GetFullPath` sous racine.  
Réponses d’erreur = stack complète. Chemin absolu renvoyé dans `FullPath`.

**Impact :** écriture/lecture hors dossier prévu (configs, clés, autres sites).  
**Action :** canonicaliser le chemin, refuse `..`, allowlist extensions + taille, stocker hors webroot, ne jamais renvoyer le path OS.

### C6 — Secrets de production dans le publish git

**Preuve :** clés `Secret`, `YanportToken`, `PerleRareDB`, `Exchange*` présentes dans `appsettings*.json` du dépôt de déploiement.

**Impact :** quiconque a accès SSH/git publish possède la clé JWT et la DB.  
**Action :** rotation immédiate (JWT secret, DB password, Exchange, Yanport) ; sortir les secrets vers variables d’environnement / fichier hors git ; purger l’historique du bare repo si possible.

### C7 — Injection SQL certaine dans les filtres d’annonces

**Preuve :** `AnnoncesGlobalesController.MakePredicate` concatène des valeurs clientes dans du SQL :
- `with field=value` → `{otherField}={otherValue}` sans quotes
- `>=…` collé tel quel
- branche par défaut : `fn + " = '" + value + "'"` **sans échappement d’apostrophe**

Le SQL est ensuite exécuté sur `annonces_globales` (recherche / RecupInfos).

**Impact :** utilisateur authentifié (et éventuellement anonyme via d’autres chemins) peut altérer la requête, lire d’autres tables si multi-statements, ou DoS la DB.  
**Action :** paramètres SQL + allowlist champs/opérateurs ; supprimer la syntaxe libre `with field=value`.

### C8 — Mot de passe mailbox hardcodé dans le code

**Preuve :** `NotificationContainer.SendNotification` passe un mot de passe en dur à `SendMail` (expéditeur `alerte@perle-rare.com`). Valeur **non reproduite** ici.

**Impact :** secret dans le binaire / PDB / git publish ; révocation impossible sans rebuild.  
**Action :** rotation immédiate du compte mailbox ; secret hors code (config / coffre).

### C9 — Validation TLS désactivée (process-wide)

**Preuve :** `ServicePointManager.ServerCertificateValidationCallback = (…) => true` dans `PRExchangeService` et `WebSiteService`. Callback **global** au process (Exchange, Yanport, old site).

**Impact :** MITM → vol identifiants mail / jetons.  
**Action P0 :** supprimer les callbacks (hotfix).

---

## 4. Constats High (P1 — 48 h à 1 semaine)

### H1 — Swagger UI public en production **LIVE**

`Startup` active `UseSwagger` / `UseSwaggerUI` hors condition d’environnement et redirige `/` → `swagger`.  
Swagger JSON live récupérable sans auth (~185 opérations) = carte d’attaque complète.

**Action :** Swagger uniquement en Development ; ou Basic Auth IP allowlist.

### H2 — Endpoints de test anonymes **LIVE**

`GET /api/Test/info` → réponse observée : `DBName=perle-rareinfo`  
`GET /api/Test/clearcache` → purge cache recherche anonymement.

**Action :** supprimer ou protéger Admin ; ne jamais exposer le nom de DB.

### H3 — JWT trop permissif

- `ValidateIssuer = false`, `ValidateAudience = false`
- `RequireHttpsMetadata = false`
- durée **7 jours**
- policies `Admin`/`GetAll`… basées sur **Claim type `"Admin"`**, alors que le token ajoute des **roles** (`ClaimTypes.Role`) — incohérence possible selon endpoints

**Action :** issuer/audience, secret long aléatoire rotaté, TTL court + refresh, aligner policies sur roles, HTTPS metadata en prod.

### H4 — Fuite d’informations via exceptions

Nombreux `return BadRequest(ex.ToString())` / `Ok(ex.ToString())` (`DbController`, `PropertyContacts`, `ConseillersPersonnels`, `Test`, `Health`…).

**Action :** middleware d’erreurs générique ; logs serveur structurés uniquement.

### H5 — SQL dynamique interpolé (hors C7)

Exemples restants après C7 :

- `HealthController` : `UPDATE ... SET T_PropertyId='{t.PId}'`
- `UserService.SwitchDispo` : `WHERE CP_RefConseiller={refConseiller}` (int, moindre risque)
- `Extensions.DoesTableExist` : interpolation `tableName` / `Database`
- `RecupInfoSearcher` / `DataReaderHelper` : WHERE via DSL → SQL
- tables dynamiques `annonces_refcontact_{id}`

Le DSL `Predicates.StringValue` échappe `'` → `''` (correct pour littéraux).

**Action :** paramètres MySQL partout ; allowlist stricte des noms de tables/colonnes.

### H6 — SSRF images + fuite token Yanport

`NotificationContainer.TryGetImage` télécharge la première URL de `PImages` sans allowlist.  
`YanportScheduledTask` lit `UrlSearch.UsUrl` en base puis `YanportService.Get` y ajoute le **Bearer Yanport**. Un `UrlSearch` modifié (via `DbController`) peut exfiltrer le token ou viser le réseau interne.

**Action :** allowlist `api.yanport.com` + domaines images ; bloquer IP privées ; timeout / taille max.

### H7 — Relais mail / usurpation

`POST /api/Exchange/SendEmail` : tout user authentifié fournit `To`, HTML, `SenderEmail`. Hors `@perle-rare.com` → SMTP localhost. Pas de quota.

**Action :** expéditeur = identité JWT ; allowlist destinataires ; quotas ; rôle dédié.

### H8 — Runtime .NET 7 hors support

Plus de correctifs de sécurité Microsoft depuis mai 2024.

**Action :** plan de migration **.NET 8 LTS** (ou 10 LTS) + bump packages IdentityModel ≥ 6.34 / stack alignée.

### H9 — IDOR / autorisation trop grossière

Politiques `Admin`/`GetAll`/… dans `Startup` **jamais utilisées** (elles exigent un claim type `"Admin"` alors que le JWT pose un **role**).  
CRUD `ContactIntermediaires`, `Biens`, `Taches`, `Evenements`, `ContactsRecherches` : `[Authorize]` seul + `FindAsync(id)` / `EntityState.Modified` → un conseiller peut lire/écrire les objets d’un autre.

**Action :** authZ par ressource ; DTO patch ; filtre conseiller centralisé.

### H10 — Scheduler fragile (blocage + pas de lock)

`TimedHostedService` : timer 30 s, flag `_executing` **sans `finally`**. Une exception au `SaveChanges` initial peut **geler tous les jobs** jusqu’au restart. Pas de verrou distribué → multi-instance = double import Yanport / double mails.

**Action :** `try/finally` ; une seule instance jusqu’à un lock DB ; `PeriodicTimer` + cancel.

### H11 — Pas de transactions multi-tables

Aucun `BeginTransaction` trouvé. PUT PropertyContacts = EF puis SQL brut ; RecupInfos = `TRUNCATE` puis INSERT ligne à ligne (erreurs avalées).

**Action :** une connexion + transaction ; staging puis swap.

### H12 — Pagination `take=0` = illimité + N+1 Accueil

`EFHelper` : `take=0` n’applique pas de `Take`. Accueil contacts : 1+N requêtes + `.Result`. Timeout SQL 180 s.

**Action :** défaut 50 / max 500 ; agrégations groupées ; `await` uniquement.

---

## 5. Constats Medium (P2)

| ID | Constat | Action |
|---|---|---|
| M1 | CORS credentials + origines multiples (dev, prod, Heroku, FlutterFlow) | Restreindre aux fronts réellement utilisés ; retirer Heroku mort |
| M2 | Session ASP.NET 1 jour + JWT 7 jours, cache mémoire (pas multi-instance) | Clarifier modèle auth ; cookies Secure/SameSite |
| M3 | `ConseillersPersonnels` GET admin renvoie clair + mdp mail | DTO sans secrets |
| M4 | `AutoLogin` = `ddMM` + `Random().Next()` tronqué 15 | Token cryptographique stocké hashé |
| M5 | Pas de rate-limit / lockout sur `/authenticate` | Throttle + audit échecs |
| M6 | Jobs toutes les 30 s ; queue mémoire **sans producteur** | Scheduler cron/locks ; supprimer queue morte |
| M7 | `Messaging/TestNotify` **LIVE** (hors décompilé) | Auditer / supprimer endpoint de test notif |
| M8 | `SearchService` : liste statique + `lock(this)` (service scoped) | Lock statique / config immutable |
| M9 | Entités EF = DTO, quasi pas de validation d’attributs | DTO + DataAnnotations / FluentValidation |
| M10 | Migrations ad hoc (`MigrationScheduledTask` / `admin_versions`) | EF Migrations / job de deploy unique |

---

## 6. Low / qualité / exploitation (P3)

- Code mort : hash password non branché ; `Update` user → `throw new Exception("A finir")`
- Machine de dev hardcodée : `Startup.IsDevMachine` (`jbhuber` / `PORT0623001`)
- Peu d’`ILogger` côté controllers ; beaucoup de `Console.WriteLine`
- Pas de suite de tests dans le publish
- `CommandTimeout(180)` + requêtes massives Health → risque saturation MySQL (déjà observé côté VPS : pression RAM)
- PDB, scaffolding Roslyn/EF Tools et `.git` dans le publish (~150 Mo) — artefact trop large
- Déploiement via `git push` de **binaires** (pas de pipeline source → build → artifact signé)
- `HealthController` n’est **pas** un liveness/readiness ASP.NET ; `web.config` coupe même stdout
- 9 `.Wait()`/`.Result` (EWS, images) ; aucun `CancellationToken` dans les controllers

---

## 7. Points positifs (à conserver)

1. Majorité des controllers métier portent `[Authorize]` ; plusieurs zones admin utilisent `Roles = "Admin"`.
2. Couche `EFHelper` + parser de prédicats évite une partie du SQL string pour les listes EF.
3. Upload refuse explicitement `.php` (insuffisant, mais intention défensive).
4. Pipeline auth standard ASP.NET (`UseAuthentication` / `UseAuthorization`).
5. Hosted services + queue pour travaux asynchrones : bonne intention d’architecture.
6. `NormalizePhoneNumbers` utilise des paramètres SQL (`@annonceurs`, `@id`) — pattern à généraliser.

---

## 8. Plan d’action priorisé

### P0 — aujourd’hui / 48 h (sécurité urgente)

1. **Couper** sans attendre le code source :
   - `HealthController` (ou deny Apache `/api/Health`)
   - `TestController`
   - `PropertyContacts` anonymes (forcer auth au reverse-proxy si besoin)
   - Swagger UI / `swagger.json` hors admin
2. **Restreindre** `DbController` et `File/Upload|Download` (deny ou Admin only).
3. **Rotation** de tous les secrets listés §2 (JWT, DB, Exchange, Yanport) **et** du mot de passe mail hardcodé dans `NotificationContainer`.
4. Hotfix TLS : retirer les `ServerCertificateValidationCallback` permissifs.
5. Vérifier logs Apache pour appels anonymes récents sur Health / PropertyContacts / Test / Db / File.
6. Snapshot DB avant toute autre manip. Une seule instance API tant que le scheduler n’a pas de lock.

### P1 — semaine 1

7. Patch code (même si on reste sur décompilé / hotfix publish) :
   - `[Authorize]` partout + policies
   - DTO sans mots de passe
   - Erreurs génériques
   - Canonicalisation fichiers
   - Paramètres SQL (`MakePredicate`, PropertyContacts, Health)
   - `try/finally` scheduler
   - plafond `take`
8. Réduire TTL JWT ; invalider tous les tokens (rotation secret = OK).
9. Documenter et figer **la DLL réellement déployée** dans un dossier audit versionné en interne (hors git public).

### P2 — 2–4 semaines

10. Hash passwords + migration utilisateurs.
11. Monter sur **.NET 8 LTS**, mettre à jour IdentityModel / EF / Pomelo.
12. Sortir secrets de git ; CI : build from source → artifact → deploy.
13. Rate-limit auth ; monitoring (CPU/RAM API, slow queries).
14. Séparer jobs (worker dédié) du process Kestrel web.

### P3 — 1–2 mois

15. Récupérer sources originales ou industrialiser le décompilé comme base de maintenance.
16. Tests d’intégration authZ (matrice rôle × endpoint).
17. Revue IDOR systématique (filtres conseiller / contact) + quotas SendEmail.
18. Remplacer EWS password-per-user par OAuth moderne si possible.
19. Threat model + pentest externe ciblé API.

---

## 9. Matrice « quoi faire en premier » (impact × effort)

| Priorité | Action | Effort | Impact |
|---|---|---|---|
| P0 | Deny `/api/Health`, `/api/Test`, Swagger | Faible (Apache) | Très haut |
| P0 | Auth obligatoire PropertyContacts + File + Db | Moyen | Très haut |
| P0 | Rotation secrets | Faible | Très haut |
| P1 | Masquer passwords JSON + erreurs | Moyen | Haut |
| P1 | Fix path traversal fichiers | Moyen | Haut |
| P0 | Retirer bypass TLS | Faible | Très haut |
| P1 | Paramétrer SQL (`MakePredicate` + interpolations) | Moyen | Haut |
| P1 | Lock scheduler + plafond pagination | Moyen | Haut |
| P2 | Hash mots de passe | Élevé | Critique long terme |
| P2 | Migration .NET 8 | Élevé | Haut |
| P3 | Sources + CI + tests | Élevé | Structurant |

---

## 10. Checklist de validation post-correctifs

- [ ] `curl -I https://api.perle-rare.info/swagger/index.html` → 401/404  
- [ ] `curl https://api.perle-rare.info/api/Test/info` → 401/404  
- [ ] `curl https://api.perle-rare.info/api/Health/Migrate` → 401/404  
- [ ] `curl https://api.perle-rare.info/api/PropertyContacts` → 401  
- [ ] Upload `../` ou extension `.config` → refusé  
- [ ] GET conseiller ne contient plus `cpMotDePasse` / `cpMelMotDePasse`  
- [ ] Nouveau secret JWT invalide les anciens tokens  
- [ ] Connexion DB / Exchange / Yanport OK après rotation  

---

## 11. Annexes

### A. Controllers sans `[Authorize]` de classe (décompilé)

Réels (hors DTO) :

- `HealthController`
- `PropertyContactsController`
- `TestController`
- `UserController` (OK : auth méthode par méthode)
- `ExchangeController` (OK : auth méthode par méthode)
- `FormulairesController` (OK : auth méthode par méthode)

### B. Endpoints sensibles confirmés live (Swagger)

- `/api/Health/*` (11 routes dont repair)
- `/api/Test/info`, `/api/Test/clearcache`
- `/api/Db/{model}` CRUD
- `/api/File/Upload`, `/api/File/Download`
- `/api/PropertyContacts*`
- `/api/Messaging/TestNotify` (build live uniquement)

### C. Limites de cet audit

- Statique + smoke minimal ; pas d’exploitation offensive.
- `YanportScheduledTask` (~1200 lignes) revu sur les patterns URL/token/SQL, pas ligne à ligne métier.
- Build live ≠ git publish : certains findings live-only non sourcés en C#.
- Config values et mot de passe hardcodé volontairement absents de ce document.
- Inventaire complémentaire : ~140 actions HTTP décompilées, 71 `DbSet`, 5 jobs timer, 4 tâches CLI.

---

**Prochaine étape recommandée :** appliquer le **P0 Apache/deny** aujourd’hui, puis hotfix auth sur PropertyContacts / Health / File / Db, puis rotation des secrets — avant tout travail fonctionnel côté API.
