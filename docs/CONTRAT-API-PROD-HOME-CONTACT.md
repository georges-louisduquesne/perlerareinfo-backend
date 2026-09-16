# Contrat API prod — Accueil + fiche contact

Source de vérité pour coller aux **données affichées en prod**, pas pour « améliorer » le métier.

| Référence | Valeur |
|---|---|
| Front prod (bundle) | `https://perle-rare.info/main-es2015.b52d0cbdf1ca134bd653.js` |
| API prod | `https://api.perle-rare.info/api/` |
| Snapshot décompilé | git `679be58` (DLL d’origine **sans** `EncaissementsEnCours`) |
| Serializer | camelCase ASP.NET, `WhenWritingNull` |
| `take=0` | pas de `LIMIT` (listes Accueil 2–9 sauf tab 6) |

**Règle :** si le CRM Angular lit un champ → le garder (même vide). Ne pas renommer, wrapper, snake_case. Les améliorations client viennent **après** iso-prod.

Serializer JSON : propriétés C# `EDate` → `eDate`. Préfixes `_` Accueil inchangés (`_nbTaches`, `_avecTaches`, …).

---

## 1. Accueil — routes figées (front prod = `itemsRecherches`)

Filtre négociateur : `User/SwitchFilter` → session `filter`. Accueil contacts / événements = 6 rôles sur le contact. Tâches Accueil = `T_Qui == login`.

| Tab | Libellé bandeau | HTTP | Query front | Forme JSON |
|---|---|---|---|---|
| 0 | Mes N prospects actifs | `GET ContactsRecherches/Count` puis `GET ContactsRecherches/Accueil` | `where=CStatut eq 'PROSPECT ACTIF'` ; Accueil : `orderby`, `take`, `skip`, `withTaches=1` (pas `withAnnonces`) | Count = **nombre** ; Accueil = **tableau** |
| 1 | Mes N clients actifs | idem | `where=CStatut eq 'CLIENT ACTIF'` ; Accueil : `withAnnonces=1&withTaches=1` | idem |
| 2 | alertes prospect | `GET Taches/Prospects` | *(aucune)* | `{ items, total }` |
| 3 | alertes client | `GET Taches/Clients` | *(aucune)* | `{ items, total }` |
| 4 | rdv prospect | `GET Evenements/Prospects` | *(aucune)* | `{ items, total }` |
| 5 | visites prévues | `GET Evenements/RdvClients` | *(aucune)* | `{ items, total }` |
| 6 | dernières visites | `GET Evenements/ClientsLast` | `orderby=eDate desc&take=10` | `{ items, total }` |
| 7 | transactions en cours | `GET Evenements/Transactions` | `where=eTypeEvenement <> 'OFFRE' AND eTypeEvenement <> 'REP. OFFRE'` | `{ items, total }` |
| 8 | offres en cours | `GET Evenements/OffresEnCours` | *(aucune)* | `{ items, total }` |
| 9 | attente d’encaissement | `GET Evenements/EncaissementsEnCours` | *(aucune)* | `{ items, total }` |

Front tabs 2–9 : `count = items.length` (ignore `total`). Tabs 0–1 : count = entier `Count`.

### 1.1 Filtres métier (679be58, recopier tels quels)

| Tab | Date | Types / statut | Extra |
|---|---|---|---|
| 2 | `TDateRealisation < demain` | `TEtat == ""` ; PROSPECT ACTIF/MORT | Filter → `TQui` |
| 3 | `TDateRealisation <= today` | `TEtat == ""` ; CLIENT ACTIF/MORT | Filter → `TQui` |
| 4 | `EDate >= today` | genre **PROSPECTION** ; `EStatut=1` ; PROSPECT ACTIF/MORT | Filter → 6 rôles |
| 5 | `EDate >= today` | genre **MISSION** ; CLIENT ACTIF/MORT | Filter → 6 rôles |
| 6 | `EDate < today` | `TeRefTypeEvenement` 5 ou 6 ; CLIENT ACTIF/MORT | `take=10` |
| 7 | `EDate >= today` | genre **TRANSACTION**, catégorie ≠ OFFRE / REP. OFFRE ; CLIENT ACTIF/MORT | `where` front redondant |
| 8 | (pas de borne date globale) | SQL groupé OFFRE / ACCEPTEE / REFUSEE + types AGENCE ; **CLIENT ACTIF seulement** ; `E_RefBien IS NOT NULL` ; logique `OffreRes.IsGood` | Filter → 6 rôles sur le résultat |
| 9 | voir §1.2 | — | — |

`ApplyDefaultFilter` Accueil (tabs 0–1), **avant** le `where` OData :

`CApporteur` ∪ `C2emeApporteur` ∪ `CNegociateur` ∪ `C2emeNegociateur` ∪ `CNomFamilleConseiller` ∪ `C2emeConseiller` == login.

Stats Accueil (si `withTaches` / `withAnnonces`) : tâches non `fait` groupées par contact ; Filter tâches → `T_Qui`. Annonces : `property_contact` + `P_DateFin IS NULL` + `PC_Actif=1`.

### 1.2 Tab 9 — `EncaissementsEnCours` (absent de `679be58`)

La DLL snapshot **n’a pas** cette route. Le **front prod l’appelle déjà**. Recette visuelle (2026-09-16) : 1 ligne **GORGÉ 3** / PSAINTJEAN / 26 950 € / `RV ACTE AUTHENT.` 15/09 16:00 / « Date butoir le 22/10 » — contact `C_FactureHon=2799` (facture déjà numérotée).

Filtre aligné sur **l’affichage live** (1 ligne GORGÉ 3, même sans filtre négociateur) :

- `EStatut=1`, `EDate <= today`
- type **`RV ACTE AUTHENT.`** uniquement (les `RV COMPROMIS` restent sur l’onglet « transactions en cours »)
- `CStatut == CLIENT ACTIF` (pas CLIENT MORT)
- `CMttHono != null` et `!= 0`
- **1 ligne / contact** = dernier acte (`EDate` desc, `ERefEvenement` desc)
- Filter → 6 rôles contact

Ne **pas** lister les compromis : ça donne 12 lignes agence vs 1 en prod. Ne **pas** filtrer sur `CFactureHon` / `CFacturePs` nuls : ça sort GORGÉ 3 (`C_FactureHon=2799`).

### 1.3 DTO Accueil — clés JSON (camelCase)

**`ContactsRechercheAccueil`** (tableau) : champs `ContactsRecherche` + photos `*\_PS` / `*\_I` + `_nbBiensLus`, `_nbBiensDemiEtoile`, `_nbBiensEtoile`, `_nbBiensNonLues`, `_biensNonLuesDate`, `_avecTaches`, `_nbAnnoncesNonLues`, `_annoncesNonLuesDate`, `_nbTaches`.

**Événements client** (`items[]`) — gelés depuis 679be58 :

`eRefEvenement`, `eDate`, `eTypeEvenement`, `eTexte`, `eRefContact`, `cNomFamille`, `cNomFamilleConseiller`, `cMttHono`, `bCp`, `bAdresse`, `bRef`, `eConseiller_PS`

Additifs migrés (le front peut les ignorer) : `eDateCreation`, `cStatut`. **Ne pas retirer** les clés 679be58.

**Prospects événements :** `eRefEvenement`, `eDate`, `eTypeEvenement`, `cNomFamille`, `eTexte`, `eNomContact`, `eRefContact`, `cNegociateur`, `eConseiller_PS` (+ additifs `eDateCreation`, `cStatut`).

**ClientsLast :** DTO client + `ciPrenom`, `ciNom`, `ciRef`, `iNomIntermediaire`.

**Tâches Accueil :** `tRef`, `tDateRealisation`, `cNomFamille`, `tType`, `tCom`, `tRefContact`, `tQui`, `tQui_PS`, `cNegociateur`, `cNegociateur_PS`.

---

## 2. Fiche contact — routes figées

### 2.1 Chargement / sauvegarde

| Quand | HTTP | Query / corps | Forme |
|---|---|---|---|
| Fiche | `GET ContactsRecherches/{id}` | — | **objet** `ContactsRecherche` |
| Max mandat | `GET ContactsRecherches?select=CNumeroMandat&orderby=CNumeroMandat desc&take=1` | (idem HON/PS) | tableau / items selon helper |
| Autosave | `PUT ContactsRecherches/{id}` | entité complète | 204 / objet inchangé côté métier |
| Formulaire | `GET Formulaires/{id}` | si `cRefFormulaire` | objet |
| Catalogues | `GET Origines`, `ConseillersPersonnels?where=…`, `TypesMails`, `Templates`, `TypesTaches`, `TypesEvenements`, `CodesPostaux`, `Quartiers`, `TagsAnnonces` | — | tableaux / `{ items }` selon route |
| Géo | `GET search/{text}` | — | |

`GET {id}` : `ConvertPhpSerializedToJson` sur `cLocalisation`, `cSurface`, `cBudget`, `cBudgetC`, `cTags`, `cAnciennete`. Photos `*\_PS` = listes Accueil, **pas** le GET by id.

### 2.2 Onglets fiche

| Onglet | HTTP | Query | Forme |
|---|---|---|---|
| Événements | `GET Evenements/Contact` | `where=eRefContact eq {id}` | **tableau** `ContactEvenements[]` (**pas** `{ items }`) |
| Flags bien / agenda | `GET Evenements` | `where=eRefContact eq {id}&select=eRefEvenement,ePropertyId,eRefAnnAgc&take=10000` | tableau `EvenementsEx` |
| Tâches | `GET Taches` | `where=tRefContact eq {id}` | `{ items, total }` ou tableau selon route liste |
| Agences | `GET IntermediairesDirects/Top30Agences/{contactId}` | table `annonces_refcontact_{id}` (remplie au **apply** RecupInfos v1) | |

**`ContactEvenements` gelé :** `eRefContact`, `eRefEvenement`, `eDate`, `eTypeEvenement`, `eCr`, `eMail`, `eRefBien`, `eRefInterI`, `eRefInterD`, `eTexte`, `bRef`, `bCp`, `bAdresse`, `i2RefIntermIndirect`, `i2NomIntermIndirect`, `i2Icon`, `iRefIntermediaire`, `iNomIntermediaire`, `qSigneInterD`, `cRefCtcInter`, `cNomIntermediaire`, `cNom`, `cPrenom`, `qSigneCtcInter`.

Pas de `ePropertyId` / `ePcId` sur cette vue → 2ᵉ GET `Evenements` pour les drapeaux.

### 2.3 Recherche volumes (RecupInfos)

| | v1 `POST AnnoncesGlobales/RecupInfosAnnonces` | v2 `POST AnnoncesGlobales/RecupInfosAnnonces2` |
|---|---|---|
| Table | `annonces_globales` (`AG_*`) | `property` (`P_*`) |
| Facettes JSON | `{ field, value, nb }[]` dans `counts` | **même forme** |
| Champs `field` | `TypeTransaction`, `TypeBien`, `Cp`, `Quartiers`, `NbPieces`, `NbChambres`, `Surface`, `Prix`, `PrixSurface`, `Etage`, `EstDernierEtage`, `EstExclusif`, `BaissePrix`, `Anciennete`, `Tag` | identiques |
| Apply | TRUNCATE+INSERT `annonces_refcontact_{id}` | sync `property_contact` |
| Front actuel | fallback si v2 vide | **toujours appelé** |

Réponse : `{ counts: [{ field, value, nb }], error? }`. Ne pas wrapper.

Prod live RecupInfos (ancienne API) compte **AG**. `annonces_globales` est vide côté Yanport actuel → v2 sur `property`. Écarts volumes Odile (ex. 95/47 vs 176/29) = **source table**, pas un bug de noms JSON. Ne pas « réparer » en changeant `field` / `nb`.

Règles v2 déjà alignées CRM :

- `P_State=1`, `P_DateFin` NULL ou `0000-00-00`
- `DateDebut` manquant = **0 jours**
- curseur ouvert `max=0` → `>= min`, pas `BETWEEN 0 AND min`
- `COUNT(DISTINCT P_PropertyId)`

### 2.4 Clés fiche contact (GET/PUT)

Identité : `cNomFamille`, `cPrenom`, `cAdresse`, `cCodePostal`, `cVille`, `cPaysRegion`, téléphones, `cMel1/2`, `cStatut`, `cRemarques`.

Mandat / facture : `cLibelleClient`, `cMandat`, `cNumeroMandat`, `cDate`, `cDateFin`, `cDateCreation`, `cRecherche`, `cQrecherche`, `cContratsSignes`, **`cFactureHon`**, **`cFacturePs`**, **`cMttHono`**, `cFraisPercus`, `cRemise`.

Équipe : `cOrigine`, `cApporteur`, `c2emeApporteur`, `cPourcentageApp`, `cNegociateur`, `c2emeNegociateur`, `cPourcentageNeg`, `cNomFamilleConseiller`, `c2emeConseiller`, `cPourcentageCons`.

Critères sérialisés : `cLocalisation` `{ CP, quartiers }`, `cIdTypeMission`, `cTypeTransaction`, `cTypeBien`, `cSurface`, `cBudget`, `cBudgetC`, `cNbPieces`, `cNbChambres`, `cEtage`, `cAnciennete`, `cTags`, `cDernEtage`, `cExclusivite`, `cEvoPrix`.

---

## 3. Recette iso-prod (données, pas le chrome UI)

Même utilisateur, Filter ON, comparer **lignes / compteurs / HON. ttc**, pas le thème démo.

1. Accueil tabs 0–8 : mêmes familles de lignes que prod (dates / types ci-dessus).
2. Tab 9 : **une** ligne **GORGÉ 3** (acte authentique), pas les 12 compromis agence ni 998 RV sans honoraires.
3. Fiche Odile (`C_RefContact=61302`) : GET by id + événements + RecupInfos v2 (volumes property ; AG prod peut rester plus haut tant que `annonces_globales` n’est pas alimenté).
4. `GET Evenements/Contact?where=eRefContact eq {id}` → **200** + tableau (pas 400 OData).

Hors contrat API (front démo) : colonnes Accueil tab 9 prod = Date / Client / **Conseiller** / adresse / Commentaires / Hon. ttc. Le CRM démo a un layout photo différent — ça n’autorise pas à changer le JSON.

---

## 4. Écarts connus (ne pas « corriger » en cassant le contrat)

| Sujet | Statut | Ne pas faire |
|---|---|---|
| RecupInfos AG vs `property` | v2 volontaire : AG vide | renommer facettes, rebasculer v2 sur AG vide |
| Tab 9 absent de `679be58` | reconstruit sur l’affichage live | revenir au filtre facture NULL |
| `eDateCreation` / `cStatut` additifs | OK (additif) | les retirer si un front les lit déjà |
| Bundle prod `b52d0cbd…` | plus vieux que le front source (RecupInfos / Quartiers / meta-moteur) | changer l’API pour un front non déployé |
| `take=0` Accueil | illimité | plafonner sans note client |

Améliorations (pagination, hash, JWT, UX) : **après** iso-prod, ordre client écrit.
