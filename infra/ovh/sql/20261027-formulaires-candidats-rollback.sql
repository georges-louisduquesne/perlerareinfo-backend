-- Retire les 2 mails rattrapés tant qu’ils n’ont pas été attribués ni rejetés.
-- Ne supprime pas la table.

DELETE FROM formulaires_candidats
WHERE FC_Statut = 1
  AND FC_MessageId IN (
    '<MI3P195MB3300626E0BBA26B2A3E69FDCCC662@MI3P195MB3300.EURP195.PROD.OUTLOOK.COM>',
    '<MI3P195MB33004AD396790BCDCA5647BACC602@MI3P195MB3300.EURP195.PROD.OUTLOOK.COM>'
  );
