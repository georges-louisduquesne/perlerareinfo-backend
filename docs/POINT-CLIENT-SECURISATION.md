# Point client — sécurisation de la connexion API

**Date du point :** à renseigner  
**Contexte :** API migrée en local + démo isolée ; **production non touchée** tant qu’un créneau n’est pas validé.  
**Démo à jour :** `https://dev.perle-rare.info/api-demo/` (Swagger : `…/api-demo/swagger/index.html`)  
**Front Angular :** inchangé (contrat API gelé).

---

## 1. Message d’ouverture (30 s)

> On a sécurisé et reconstruit l’API en local, et on a une démo isolée pour tester.  
> La **prod** (`https://api.perle-rare.info`) tourne toujours sur l’ancien binaire.  
> Pour **fermer** la sécurisation de la connexion, il faut maintenant des **décisions + un créneau** avec vous : rotation des secrets et bascule contrôlée.

---

## 2. Ce qui est déjà fait (sans toucher la prod)

| Thème | Statut | Effet pour le client |
|---|---|---|
| Swagger / outils test / Health / Db | Fait en code local (+ démo) | Plus de « carte d’attaque » publique **une fois déployé en prod** |
| Auth sur zones trop ouvertes | Fait | Lecture / écriture réservées aux comptes légitimes |
| Fichiers (path traversal) + erreurs génériques | Fait | Moins de fuite d’info / d’accès disque |
| TLS sortant (plus de trust-all) | Fait | Messagerie / partenaires plus sains |
| Filtres SQL sécurisés | Fait | Même syntaxe front, injections réduites |
| Sources maintenables + .NET 8 + SOLID | Fait | On peut livrer et corriger sans binaire « magique » |
| API démo isolée sur le VPS | Live | Recette sans risque prod (DB lecture seule) |
| Perf lectures (Accueil, listes) | Live sur démo | Moins de charge ; le front charge encore « tout » d’un coup |

**Important :** ces durcissements **ne sont pas encore sur la prod**. La prod a encore Swagger / Test ouverts tant qu’on n’a pas basculé.

---

## 3. À trancher avec le client aujourd’hui

### A. Créneau de **rotation des secrets** (bloquant — plan #7)

À renouveler (considérés exposés car présents dans d’anciens publishes / configs) :

| Secret | Impact si on tourne | Qui valide |
|---|---|---|
| Secret JWT (clé de signature des tokens) | **Toutes les sessions CRM se déconnectent** | Client + tech |
| Mot de passe MariaDB applicatif | Redémarrage API avec nouvelle chaîne | Client + tech / hébergeur |
| Comptes Exchange / mails (si encore en clair côté config) | Relancer mails / RDV calendrier | Client |
| Token Yanport | Imports annonces | Client |
| Autres clés dans configs historiques | Selon inventaire | Client |

**Demander :**
- [ ] Créneau **1–2 h** (idéalement hors pic métier)
- [ ] Personne joignable pour valider « CRM reconnecte OK »
- [ ] Plan de retour arrière accepté (garder l’ancien secret JWT 15–30 min si besoin)

### B. **JWT plus courts** (bloquant — plan #13)

Aujourd’hui : jetons très longs (~7 jours).  
Proposition : TTL plus court (ex. 8–24 h) + même login CRM.

**Demander :**
- [ ] TTL acceptable (ex. fin de journée / 24 h / 7 j inchangé pour l’instant)
- [ ] OK pour déconnexion forcée au moment du déploiement (lié à la rotation secret)

### C. **Hash des mots de passe** (plan #12 — pas urgent le jour J, mais à caler)

Bascule **douce** : à la prochaine connexion réussie, le clair est remplacé par un hash. Les comptes non reconnectés restent en clair jusqu’à leur 1re connexion.

**Demander :**
- [ ] OK pour activer après (ou pendant) le créneau secrets
- [ ] Communication interne éventuelle (« reconnectez-vous une fois »)

### D. **Bascule production API** (cutover)

Ordre recommandé :
1. Recette démo validée (login + home + 2–3 écrans critiques)
2. Créneau secrets + JWT
3. Déploiement prod du nouveau binaire (empreinte avant/après)
4. Checklist : login, home, fichiers, mails si utilisés

**Demander :**
- [ ] Date / heure de cutover souhaitée
- [ ] Qui teste en live (1 admin + 1 négociateur)
- [ ] Fenêtre max d’indisponibilité acceptable (cible : quelques minutes)

### E. **Perf CRM** (hors « secrets », mais utile au point)

Le front charge encore **toute** la liste prospects (`take = count`).  
Même API rapide + VPS = souvent 1–3 s depuis un Mac distant.

**Demander :**
- [ ] Priorité courte : garder comportement actuel
- [ ] Ou chantier front : pagination Accueil (changement front, hors gel API strict)

---

## 4. Ce qui reste ouvert côté tech (pas besoin de décision immédiate)

| # | Sujet | Prérequis |
|---|---|---|
| 8 | Finir recette CRM sur démo | Compte test + feedback bugs |
| 15 | Droits plus fins (moins de « tout voir ») | Ne pas casser le front → analyse d’abord |
| 16 | Plafond pagination serveur | Accord si on limite `take` |
| 17 | Runbook ops final | Après cutover |

---

## 5. Checklist créneau « sécurisation connexion » (à cocher le jour J)

**Avant**
- [ ] Backup / note de l’empreinte prod (DLL + PID)
- [ ] Secrets nouveaux générés (coffre, pas mail en clair)
- [ ] Démo déjà validée sur le même build

**Pendant (ordre)**
- [ ] Annoncer déconnexion CRM imminente
- [ ] Rotation JWT secret → redémarrage API
- [ ] Rotation DB / Exchange / Yanport selon plan
- [ ] Login admin OK
- [ ] Login négociateur OK
- [ ] Home + 1 fiche contact + 1 fichier
- [ ] Swagger prod **fermé** ; `/api/Test/info` **401/404**

**Après**
- [ ] Empreinte prod inchangée hors livrable attendu
- [ ] Registre des rotations (quoi / quand / qui — **sans** écrire les mots de passe)
- [ ] Si hash mdp activé : 1ère reconnexion OK

---

## 6. Phrases utiles si le client hésite

| Objection | Réponse |
|---|---|
| « On ne peut pas déconnecter tout le monde » | On choisit un créneau calme ; rotation JWT = 1 reconnexion ; on peut faire JWT court **après** ou en 2 temps. |
| « La démo est lente » | Réseau Mac→VPS + liste complète prospects ; la prod locale au même DC restera plus fluide ; pagination front = option. |
| « On déploie sans tourner les secrets » | Déconseillé : d’anciens publishes contenaient encore des secrets → rotation = cœur de la sécurisation connexion. |
| « Le front doit-il changer ? » | Non pour le cutover sécurité. Oui seulement si on veut paginer Accueil pour la perf ressentie. |

---

## 7. Décisions à ramener du point (remplir)

| Décision | Choix client | Date |
|---|---|---|
| Créneau rotation secrets | | |
| TTL JWT | | |
| Hash mdp (quand) | | |
| Date cutover prod API | | |
| Pagination front Accueil | oui / non / plus tard | |
| Compte(s) de recette démo | | |

---

## 8. Liens

| Env | URL |
|---|---|
| API **prod** (ne pas casser) | `https://api.perle-rare.info` |
| API **démo** (tests) | `https://dev.perle-rare.info/api-demo/` |
| Swagger démo | `https://dev.perle-rare.info/api-demo/swagger/index.html` |
| Front démo (encore sur API prod) | `https://dev.perle-rare.info/demo/` |

Suivi interne : `docs/PLAN-MIGRATION.md` · audit : `docs/AUDIT-API-BACKEND-2026-08.md`.
