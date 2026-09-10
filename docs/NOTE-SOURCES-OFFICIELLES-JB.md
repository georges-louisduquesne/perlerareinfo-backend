# Sources reçues de JB Huber (sept. 2026)

Archive : `docs/huberje-perle-rare-90a926ab218a.zip`  
Dézippé (**non versionné**) : `sources-jb-huber/`

## Verdict

**Ce n’est pas ApiPerleRare.**  
Solution `PerleRare.sln` — .NET Framework **4.8** (batch, jobs, mail/PDF via ashx).  
Pas d’ajustement de remplacement de notre API décompilée.

| | Zip JB | Notre code actuel |
|---|---|---|
| Produit | Outils / cron / GenFile SendMail | CRM REST `api.perle-rare.info` |
| Stack | .NET Framework 4.8 | ASP.NET Core net8 |
| Controllers CRM | non | ~57 (User, Contacts, Biens…) |

## Utile en référence (ne pas merger tel quel)

- `PerleRare.Services/Formula/` (+ `value.pg`) — grammaire lisible des filtres
- `PerleRare.DbAccess/` — mapping MySQL partiel + générateur
- `PerleRare.Tasks/` / NotificationManagers — logique jobs
- `PerleRare.Services/MailService.cs` — EWS plus lisible que le décompilé

## Contient des secrets

`App.config`, `Web.config`, `Yanport/Program.cs` → **ne pas committer**.  
Zip + dossier `official-sources/` sont dans `.gitignore`.

## Suite

Relancer JB pour **ApiPerleRare** : `docs/MAIL-JB-HUBER-API-PERLERARE.txt`.  
Continuer la migration sur `api-perle-rare-decompiled/`.
