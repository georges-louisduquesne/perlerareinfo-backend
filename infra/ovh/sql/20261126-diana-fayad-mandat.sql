-- Madame Diana FAYAD, contact 62788.
-- C_Mandat était le chemin absolu, coupé à 75 caractères, donc sans « FAYAD.pdf ».
-- Le fichier réel est déjà sur le disque et répond 200 :
--   mandats/Mandat de recherche exclusif - Madame Diana FAYAD.pdf
-- Contrôle avant : une seule ligne, C_RefContact 62788,
-- C_LibelleClient = Madame Diana FAYAD, longueur 75.
-- Retour arrière : 20261126-diana-fayad-mandat-rollback.sql
-- La session SQL du dépôt ne peut pas créer de table.
-- La copie d'une ligne est faite par l'administrateur MariaDB du serveur,
-- puis l'UPDATE ci-dessous.

UPDATE contacts_recherche
   SET C_Mandat = 'mandats/Mandat de recherche exclusif - Madame Diana FAYAD.pdf'
 WHERE C_RefContact = 62788
   AND C_LibelleClient = 'Madame Diana FAYAD'
   AND C_Mandat = '/var/www/perle-rare/web/mandats/Mandat de recherche exclusif - Madame Diana';
