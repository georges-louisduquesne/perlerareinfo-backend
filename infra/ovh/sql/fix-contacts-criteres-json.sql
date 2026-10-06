-- Critères pièces / chambres / étage enregistrés en JSON par le nouveau front avant le correctif
-- 7311299 / ca00a4b : l'import Yanport de l'API historique lève « Exception filtre contact ».
-- Remet le format PHP sérialisé de l'ancien CRM ('' pour une liste vide).
-- Inventaire 2026-10-06 (SELECT) : 7 contacts — 30354, 47538, 59584, 60526, 62342, 62534, 62963.
-- Idempotent : chaque UPDATE ne touche la ligne que si elle contient encore la valeur JSON relevée.
-- À exécuter uniquement sur ordre écrit (écriture sur la base prod partagée).

CREATE TABLE IF NOT EXISTS contacts_recherche_bak_json_20261006 AS
SELECT C_RefContact, C_NbPieces, C_NbChambres, C_Etage
FROM contacts_recherche
WHERE C_RefContact IN (30354, 47538, 59584, 60526, 62342, 62534, 62963);

START TRANSACTION;

UPDATE contacts_recherche SET C_NbPieces = '', C_NbChambres = '', C_Etage = ''
WHERE C_RefContact IN (30354, 59584, 62342, 62534, 62963)
  AND C_NbPieces = '[]' AND C_NbChambres = '[]' AND C_Etage = '[]';

UPDATE contacts_recherche SET C_NbPieces = 'a:1:{i:0;s:1:"4";}', C_NbChambres = '', C_Etage = ''
WHERE C_RefContact = 47538
  AND C_NbPieces = '["4"]' AND C_NbChambres = '[]' AND C_Etage = '[]';

UPDATE contacts_recherche
SET C_NbPieces = 'a:3:{i:0;s:1:"3";i:1;s:1:"4";i:2;s:1:"5";}',
    C_NbChambres = 'a:3:{i:0;s:1:"2";i:1;s:1:"3";i:2;s:1:"4";}',
    C_Etage = ''
WHERE C_RefContact = 60526
  AND C_NbPieces = '["4","5","3"]' AND C_NbChambres = '["3","4","2"]' AND C_Etage = '[]';

-- Contrôle : 0 attendu avant COMMIT.
SELECT COUNT(*) AS encore_json FROM contacts_recherche
WHERE TRIM(C_NbPieces) LIKE '[%' OR TRIM(C_NbChambres) LIKE '[%' OR TRIM(C_Etage) LIKE '[%';

COMMIT;

-- Retour arrière :
-- UPDATE contacts_recherche c JOIN contacts_recherche_bak_json_20261006 b USING (C_RefContact)
-- SET c.C_NbPieces = b.C_NbPieces, c.C_NbChambres = b.C_NbChambres, c.C_Etage = b.C_Etage;
