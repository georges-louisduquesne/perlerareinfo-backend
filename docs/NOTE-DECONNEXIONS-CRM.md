# Diagnostic — déconnexions CRM « intempestives »

Contexte : JWT API passé de ~7 j à **48 h** en code (`TokenLifetimeInMinutes = 2880`).  
Prod non basculée tant que cutover non décidé.

Le 48 h est **glissant** : `GET /api/User/refresh` (Bearer encore valide) renvoie un nouveau jeton 48 h.  
Le CRM démo appelle ce refresh à l’ouverture, toutes les 6 h, et au retour sur l’onglet s’il reste < 24 h.  
Conséquence métier : mot de passe seulement après **48 h sans ouvrir le CRM**, pas tous les deux jours.

## Causes probables (ordre de check)

1. **TTL JWT expiré** — après 48 h (ou 7 j sur l’ancien binaire), le Bearer n’est plus valide → 401.
2. **Front** — interceptor 401 → redirect login (comportement attendu CRM).
3. **Cache / onglets** — ancien token en `localStorage` / autre onglet après redeploy ou rotation secret.
4. **Rotation secret JWT** — invalide **toutes** les sessions d’un coup (créneau cutover uniquement).
5. **Extensions navigateur** / mode privé / purge cookies (moins fréquent si Bearer header).

## Soft-hash mdp

Le hash à la connexion **ne déconnecte pas**. Il réécrit seulement `CP_MotDePasse` après login réussi.  
Les sessions en cours restent valides jusqu’à expiration JWT.

## Piste de recette

```bash
# Décoder exp du token (sans le coller en clair dans un ticket)
# Comparer exp - iat ≈ 2880 minutes après bascule code
```

Si déco avant 48 h : regarder logs API 401, horloge machine client, et si un autre deploy a tourné le secret.
