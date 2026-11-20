import test from 'node:test';
import assert from 'node:assert/strict';
import {
  aggregateMutationRows,
  buildBatchInsertSql,
  formatHouseNumber,
  mapDgfipTxtRowToGeoRow,
  normalizeStreet,
  parseCsvLine,
  selectDvfResourcesForYears,
} from './import-dvf-transactions.mjs';

test('normalizeStreet harmonise les abréviations DVF et accents BAN', () => {
  assert.equal(normalizeStreet('BD HAUSSMANN'), 'boulevard haussmann');
  assert.equal(normalizeStreet('Boulevard Haussmann'), 'boulevard haussmann');
  assert.equal(normalizeStreet('RUE ST HONORE'), 'rue saint honore');
  assert.equal(normalizeStreet('Rue Saint-Honoré'), 'rue saint honore');
  assert.equal(normalizeStreet('AV VICTOR HUGO'), 'avenue victor hugo');
  assert.equal(normalizeStreet('ALL DES ACACIAS'), 'allee des acacias');
});

test('formatHouseNumber normalise numéro + suffixe', () => {
  assert.deepEqual(formatHouseNumber('12', 'B'), { num: '12B', suffix: 'B' });
  assert.deepEqual(formatHouseNumber('12.0', 'bis'), { num: '12B', suffix: 'B' });
  assert.deepEqual(formatHouseNumber('45', ''), { num: '45', suffix: null });
});

test('parseCsvLine gère les guillemets et virgules internes', () => {
  assert.deepEqual(parseCsvLine('a,"b,c","d""e",f'), ['a', 'b,c', 'd"e', 'f']);
});

test('aggregateMutationRows regroupe appartement + dépendances sans doubler le prix et extrait Carrez', () => {
  const rows = [
    {
      id_mutation: '2025-123',
      numero_disposition: '000001',
      date_mutation: '2025-03-14',
      nature_mutation: 'Vente',
      valeur_fonciere: '650000',
      adresse_numero: '12',
      adresse_suffixe: 'B',
      adresse_nom_voie: 'RUE DE RIVOLI',
      adresse_code_voie: '8274',
      code_postal: '75004',
      code_commune: '75104',
      nom_commune: 'Paris 4e Arrondissement',
      code_departement: '75',
      id_parcelle: '75104000AB0012',
      type_local: 'Appartement',
      surface_reelle_bati: '62',
      nombre_pieces_principales: '3',
      nombre_lots: '2',
      lot1_numero: '14',
      lot1_surface_carrez: '60.50',
      latitude: '48.8551',
      longitude: '2.3572',
    },
    {
      id_mutation: '2025-123',
      numero_disposition: '000001',
      date_mutation: '2025-03-14',
      nature_mutation: 'Vente',
      valeur_fonciere: '650000',
      adresse_numero: '12',
      adresse_suffixe: 'B',
      adresse_nom_voie: 'RUE DE RIVOLI',
      code_postal: '75004',
      type_local: 'Dépendance',
      nombre_lots: '2',
      lot2_numero: '42',
    },
  ];
  const res = aggregateMutationRows(rows);
  assert.equal(res.status, 'ok');
  assert.equal(res.tx.prix, 650000);
  assert.equal(res.tx.adresseNum, '12B');
  assert.equal(res.tx.adresseRueNorm, 'rue de rivoli');
  assert.equal(res.tx.nbDependances, 1);
  assert.equal(res.tx.surfaceBatie, 62);
  assert.equal(res.tx.surfaceCarrez, 60.5);
  assert.equal(res.tx.prixM2, 10743.8);
  assert.equal(res.tx.nbPieces, 3);
  assert.equal(res.tx.fingerprint.length, 64);

  const sql = buildBatchInsertSql([res.tx], '2026-11-19 18:00:00');
  assert.match(sql, /INSERT INTO Transactions/);
  assert.match(sql, /ON DUPLICATE KEY UPDATE/);
  assert.match(sql, /T_SurfaceNormee/);
});

test('aggregateMutationRows exclut VEFA et ventes en bloc > 2 logements', () => {
  const vefa = aggregateMutationRows([
    {
      date_mutation: '2025-01-10',
      nature_mutation: "Vente en l'état futur d'achèvement",
      valeur_fonciere: '400000',
      code_postal: '75015',
      adresse_nom_voie: 'RUE LECOURBE',
      type_local: 'Appartement',
      surface_reelle_bati: '40',
    },
  ]);
  assert.equal(vefa.status, 'skip');
  assert.equal(vefa.reason, 'nature');

  const blockRow = {
    date_mutation: '2025-01-10',
    nature_mutation: 'Vente',
    valeur_fonciere: '3000000',
    code_postal: '75015',
    adresse_numero: '10',
    adresse_nom_voie: 'RUE LECOURBE',
    type_local: 'Appartement',
    surface_reelle_bati: '50',
  };
  const block = aggregateMutationRows([blockRow, blockRow, blockRow]);
  assert.equal(block.status, 'skip');
  assert.equal(block.reason, 'multi_unit_block');
});

test('selectDvfResourcesForYears extrait full-2026-s1 et filtre les années demandées', () => {
  const resources = [
    {
      title: 'DVF 2025',
      url: 'https://static.data.gouv.fr/resources/demandes-de-valeurs-foncieres-geolocalisees/20261020-114721/full-2025.csv.gz',
      last_modified: '2026-10-20T11:47:28Z',
    },
    {
      title: 'DVF 2026 (1er semestre)',
      url: 'https://static.data.gouv.fr/resources/demandes-de-valeurs-foncieres-geolocalisees/20261020-114825/full-2026-s1.csv.gz',
      last_modified: '2026-10-20T11:48:28Z',
    },
    {
      title: 'DVF fichier unique',
      url: 'https://static.data.gouv.fr/resources/demandes-de-valeurs-foncieres-geolocalisees/20261021-085741/dvf.csv.gz',
      last_modified: '2026-10-21T08:58:04Z',
    },
  ];
  const all = selectDvfResourcesForYears(resources);
  assert.deepEqual(all.map((r) => r.year), [2025, 2026]);
  const only2026 = selectDvfResourcesForYears(resources, [2026]);
  assert.equal(only2026.length, 1);
  assert.equal(only2026[0].year, 2026);
  assert.match(only2026[0].url, /full-2026-s1\.csv\.gz$/);

  const hist = selectDvfResourcesForYears([], [2014, 2020]);
  assert.deepEqual(hist.map((r) => r.year), [2014, 2020]);
  assert.equal(hist[0].format, 'dgfip-txt');
  assert.equal(hist[1].format, 'dgfip-txt-zip');
});

test('mapDgfipTxtRowToGeoRow convertit le format brut DGFiP pipe/virgule vers le format geo-dvf', () => {
  const header = [
    'No disposition', 'Date mutation', 'Nature mutation', 'Valeur fonciere',
    'No voie', 'B/T/Q', 'Type de voie', 'Code voie', 'Voie', 'Code postal',
    'Commune', 'Code departement', 'Code commune', 'Prefixe de section',
    'Section', 'No plan', '1er lot', 'Surface Carrez du 1er lot',
    'Nombre de lots', 'Type local', 'Surface reelle bati', 'Nombre pieces principales',
  ];
  const idx = Object.fromEntries(header.map((h, i) => [h, i]));
  const row = mapDgfipTxtRowToGeoRow([
    '000001', '14/03/2019', 'Vente', '650000,00',
    '12', 'B', 'RUE', '8274', 'DE RIVOLI', '75004',
    'PARIS 04', '75', '104', '', 'AB', '12', '14', '60,50',
    '2', 'Appartement', '62', '3',
  ], idx);
  assert.equal(row.date_mutation, '2019-03-14');
  assert.equal(row.valeur_fonciere, '650000.00');
  assert.equal(row.adresse_nom_voie, 'RUE DE RIVOLI');
  assert.equal(row.lot1_surface_carrez, '60.50');
  assert.equal(row.code_commune, '75104');
  assert.equal(row.id_parcelle, '75104000AB0012');

  const agg = aggregateMutationRows([row]);
  assert.equal(agg.status, 'ok');
  assert.equal(agg.tx.prix, 650000);
  assert.equal(agg.tx.surfaceCarrez, 60.5);
  assert.equal(agg.tx.adresseRueNorm, 'rue de rivoli');
});
