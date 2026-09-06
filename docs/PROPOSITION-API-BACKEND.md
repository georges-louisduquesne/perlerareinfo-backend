# Proposition — Remise à niveau de l’API métier

**Client :** Perle Rare  
**Objet :** sécuriser et fiabiliser l’API `api.perle-rare.info` qui alimente le CRM  
**Date :** 24 août 2026  
**Référence :** Plan 1 front Angular (proposition du 1er juillet 2026)  
**Taux :** 450 € HT / jour (identique au Plan 1)  
**Offre :** forfait **10 jours** — **4 500 € HT**

---

Madame, Monsieur,

Le CRM Angular évolue (Plan 1). L’API qui le sert, elle, n’a pas suivi : pas de sources versionnées proprement, un socle .NET hors support, et plusieurs portes ouvertes sur des fonctions de maintenance.

L’application **fonctionne** au quotidien. Cette mission ne change pas le métier (contacts, annonces, mails, Yanport). Elle vise un backend **plus sûr, plus lisible et plus simple à faire évoluer**, dans le même esprit que le travail déjà engagé sur le front.

Un diagnostic a déjà été réalisé (offert). Il permet d’aller **directement** aux correctifs, sans phase d’étude supplémentaire.

---

## 1. Ce que nous avons constaté

| Sujet | Situation actuelle | Conséquence pour vous |
|---|---|---|
| Accès | Certaines fonctions de test / maintenance restent joignables | Surface inutilement exposée |
| Comptes | Mots de passe et sessions trop permissifs | Risque en cas de fuite ou de compte partagé |
| Secrets | Identifiants dans les fichiers de déploiement | À renouveler, puis à sortir du dépôt |
| Technique | .NET 7 n’est plus maintenu par Microsoft (depuis mai 2024) | Plus de correctifs de sécurité éditeur |
| Organisation | Publish de binaires, pas de vrai projet source | Toute évolution est plus lente et plus risquée |

**En une phrase :** le CRM peut rester en production, mais l’API n’est pas dans un état serein pour la suite (mobile, nouvelles pages, reprise). Un sprint court suffit à rétablir un socle sain, **sans** réécrire le produit.

---

## 2. Offre

| | |
|---|---|
| **Prestation** | Remise à niveau complète de l’API (sécurité + socle) |
| **Durée** | **10 jours ouvrés** (2 semaines) |
| **Montant** | **4 500 € HT** (TVA 20 % en sus) |
| **Audit déjà réalisé** | 2 jours — **offert** |

Un seul forfait, tout inclus dans le périmètre ci-dessous. Recette sur l’environnement de démonstration. **La production n’est pas touchée** sans créneau validé avec vous.

---

## 3. Ce qui est inclus (les 10 jours)

Tout le périmètre initialement prévu en deux vagues est regroupé ici, de façon **ciblée** : on corrige et on organise, on ne refond pas le CRM.

### Semaine 1 — Fermer les portes et protéger les données (5 j)

| | Livrable | Résultat pour vous |
|---|---|---|
| 1 | Masquer la documentation technique et les outils de test / maintenance | Plus d’exposition publique de la « carte » interne de l’API |
| 2 | Authentification obligatoire sur les zones trop ouvertes | Lecture / écriture métier réservées aux comptes légitimes |
| 3 | Restreindre l’accès générique aux données et aux fichiers | Plus de « passe-partout » une fois connecté |
| 4 | Réponses d’erreur propres, sans détail technique ni secret | Moins de fuite d’information |
| 5 | Durcir les échanges sortants (certificats) | Moins de risque d’interception messagerie / partenaires |
| 6 | Recherches et filtres : requêtes construites de façon sûre | Base protégée contre les injections |
| 7 | Accompagner la **rotation des identifiants** (API, base, messagerie, partenaires) | Anciens accès considérés comme exposés → renouvelés |
| 8 | Recette CRM démo (connexion, listes, fichiers, mails si applicable) | L’usage quotidien continue de fonctionner |

**Votre rôle, semaine 1 :** un créneau d’**1 à 2 heures** pour valider la rotation des accès, et une personne qui peut cliquer dans le CRM (démo).

### Semaine 2 — Reprendre la main et moderniser le socle (5 j)

| | Livrable | Résultat pour vous |
|---|---|---|
| 9 | Reconstituer un **dépôt source** maintenable, aligné sur le binaire réellement en service | On ne travaille plus « à l’aveugle » sur des binaires |
| 10 | Build reproductible, secrets hors git, déploiement source → livrable | Plus de push de binaires comme seul mode de livraison |
| 11 | Passage à **.NET 8 LTS** | Correctifs de sécurité Microsoft à nouveau possibles |
| 12 | Hachage moderne des mots de passe (bascule douce à la reconnexion) | Une fuite de base ne donne plus les mots de passe en clair |
| 13 | Jetons d’accès plus courts et mieux cadrés | Sessions moins « éternelles » |
| 14 | Tâches planifiées fiabilisées (plus de gel silencieux) | Imports / mails moins fragiles |
| 15 | Droits plus raisonnables (un conseiller n’a plus un accès trop large par défaut) | Moins d’effet « tout le monde voit tout » |
| 16 | Limites de volume (pagination) et sondes de santé réelles | L’API et la base sont protégées des listes illimitées |
| 17 | Journaux exploitables + **runbook** de reprise | Vous savez comment relancer, déployer, et qui a les clés |

**Recette semaine 2 :** mêmes parcours CRM qu’aujourd’hui + checklist sécurité + mode opératoire de build / déploiement documenté.

---

## 4. Calendrier

| Semaine | Contenu |
|---|---|
| **S1** | Sécurité visible, rotation des accès, recette démo |
| **S2** | Sources, .NET 8, mots de passe, droits, limites, runbook, recette |

Démarrage sous **5 jours ouvrés** après bon de commande.  
Les deux semaines s’enchaînent (un seul engagement).

---

## 5. Garanties

- **Aucun changement production** tant que vous n’avez pas validé un créneau.
- Recette d’abord sur la **démo** (même principe que le front `/demo/`).
- Empreinte de la production vérifiée **avant et après** tout déploiement API.
- Le front Angular actuel **continue de fonctionner** (pas de changement de contrat métier).
- Livrables écrits : runbook, checklist de recette, registre des accès renouvelés (sans les mots de passe).

| Point de vigilance | Comment on le traite |
|---|---|
| Sources d’origine incomplètes | On fige le binaire en service, on reconstitue un git propre |
| Rotation des accès | Créneau avec vous + plan de retour arrière |
| Régression CRM | Recette démo à chaque palier ; on ne change pas les règles métier |

---

## 6. Hors forfait (si besoin plus tard)

Ces sujets restent possibles, **sur devis séparé**, une fois le socle en place :

| Option | Intérêt |
|---|---|
| Worker dédié (tâches lourdes hors site web) | Si Yanport / mails montent en charge |
| Batterie de tests automatisés | Filet à chaque livraison |
| Audit de sécurité externe (pentest) | Preuve tierce après cette mission |
| Connexion messagerie moderne (OAuth) | Sortir des mots de passe Exchange en base |

**Non inclus :** nouvelle API from scratch, migration cloud, refonte Yanport ou Exchange, évolution des écrans Angular (déjà couverte par le Plan 1).

---

## 7. Récapitulatif

| Désignation | Jours | Montant HT |
|---|---:|---:|
| Audit diagnostic (août 2026) | 2 | **Offert** |
| Remise à niveau API — sécurité + socle | **10** | **4 500 €** |
| **Total** | **10** (+ 2 offerts) | **4 500 €** |

TVA 20 % en sus, soit **5 400 € TTC**.

**Règlement :** 40 % à la commande, 60 % à la recette démo (fin de S2).

---

## 8. Décision demandée

Nous vous proposons de valider ce **forfait de 10 jours — 4 500 € HT**, pour traiter ensemble la mise en sécurité et la remise en ordre du socle API.

Dès votre accord écrit, nous bloquons les deux semaines et convenons du créneau de rotation des accès.

---

Cordialement,

*Document commercial — sans détail d’exploitation. Le diagnostic technique reste interne, sous NDA.*
