-- Retour arrière : le mandat de Madame Diana FAYAD reprend le chemin coupé
-- lu avant l'écriture du 26 novembre 2026.
-- C_RefContact 62788, C_LibelleClient Madame Diana FAYAD,
-- C_Mandat = /var/www/perle-rare/web/mandats/Mandat de recherche exclusif - Madame Diana
-- Copie d'avant écriture, si elle a pu être créée :
-- contacts_recherche_bak_20261126_diana_mandat

UPDATE contacts_recherche
   SET C_Mandat = '/var/www/perle-rare/web/mandats/Mandat de recherche exclusif - Madame Diana'
 WHERE C_RefContact = 62788
   AND C_LibelleClient = 'Madame Diana FAYAD'
   AND C_Mandat = 'mandats/Mandat de recherche exclusif - Madame Diana FAYAD.pdf';
