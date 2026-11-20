#!/usr/bin/env node
/**
 * Import DVF géolocalisé (data.gouv.fr) vers la table MariaDB `Transactions`.
 *
 * - Mode --dry-run par défaut (aucune écriture tant que --out-sql ou --apply n'est pas fourni).
 * - Résout automatiquement les URLs officielles de la livraison en cours via l'API data.gouv.fr :
 *   https://www.data.gouv.fr/api/1/datasets/demandes-de-valeurs-foncieres-geolocalisees/
 *   (fichiers annuels/semestriels `full-YYYY*.csv.gz`, ex. `full-2026-s1.csv.gz`),
 *   puis filtre les départements demandés en streaming.
 * - Agrège les lignes par (id_mutation, numero_disposition) pour éviter le double
 *   comptage de valeur_fonciere, compter les dépendances et extraire les surfaces Carrez.
 * - Génère / exécute des requêtes INSERT ... ON DUPLICATE KEY UPDATE basées sur T_Fingerprint (SHA-256)
 *   qui mettent à jour uniquement les colonnes DVF sans jamais écraser les colonnes enrichies
 *   (T_NbChambres, T_Etage, T_Ascenseur, T_Dpe, T_SurfaceNormee, T_PropertyId, T_PhotoUrl...).
 *
 * Exemples :
 *   node scripts/import-dvf-transactions.mjs --dry-run --years=2026
 *   node scripts/import-dvf-transactions.mjs --depts=75,92,93,94 --years=2025,2026 --apply
 *   node scripts/import-dvf-transactions.mjs --depts=75,92,93,94 --out-sql=artifacts/dvf-75-92-93-94.sql
 */

import { execFileSync } from 'node:child_process';
import crypto from 'node:crypto';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import readline from 'node:readline';
import { createRequire } from 'node:module';
import { Readable } from 'node:stream';
import zlib from 'node:zlib';
import { fileURLToPath } from 'node:url';

export const DEFAULT_DEPTS = ['75', '92', '93', '94'];
export const DEFAULT_NATURES = new Set(['Vente']);
export const RESIDENTIAL_TYPES = new Set(['Appartement', 'Maison']);
const DVF_DATASET_API_URL = 'https://www.data.gouv.fr/api/1/datasets/demandes-de-valeurs-foncieres-geolocalisees/';

// Archives publiques des dernières livraisons officielles DGFiP par millésime (2014-2020)
// avant leur sortie de la fenêtre glissante de 5 ans sur data.gouv.fr (miroir OSM France).
export const HISTORICAL_DGFIP_RESOURCES = new Map([
  [2014, { year: 2014, title: 'DVF 2014 (archive DGFiP 2019-04)', format: 'dgfip-txt', url: 'https://data.cquest.org/dgfip_dvf/201904/valeursfoncieres-2014.txt' }],
  [2015, { year: 2015, title: 'DVF 2015 (archive DGFiP 2020-04)', format: 'dgfip-txt', url: 'https://data.cquest.org/dgfip_dvf/202004/valeursfoncieres-2015.txt' }],
  [2016, { year: 2016, title: 'DVF 2016 (archive DGFiP 2021-04)', format: 'dgfip-txt', url: 'https://data.cquest.org/dgfip_dvf/202104/valeursfoncieres-2016.txt' }],
  [2017, { year: 2017, title: 'DVF 2017 (archive DGFiP 2022-04)', format: 'dgfip-txt', url: 'https://data.cquest.org/dgfip_dvf/202204/valeursfoncieres-2017.txt' }],
  [2018, { year: 2018, title: 'DVF 2018 (archive DGFiP 2023-04)', format: 'dgfip-txt', url: 'https://data.cquest.org/dgfip_dvf/202304/valeursfoncieres-2018.txt' }],
  [2019, { year: 2019, title: 'DVF 2019 (archive DGFiP 2024-04)', format: 'dgfip-txt', url: 'https://data.cquest.org/dgfip_dvf/202404/valeursfoncieres-2019.txt' }],
  [2020, { year: 2020, title: 'DVF 2020 (archive DGFiP 2025-04)', format: 'dgfip-txt-zip', url: 'https://data.cquest.org/dgfip_dvf/202504/valeursfoncieres-2020.txt.zip' }],
]);

const LEADING_STREET_ABBREVS = new Map([
  ['r', 'rue'],
  ['av', 'avenue'],
  ['ave', 'avenue'],
  ['bd', 'boulevard'],
  ['bld', 'boulevard'],
  ['boul', 'boulevard'],
  ['pl', 'place'],
  ['imp', 'impasse'],
  ['all', 'allee'],
  ['sq', 'square'],
  ['fg', 'faubourg'],
  ['fbg', 'faubourg'],
  ['pass', 'passage'],
  ['pas', 'passage'],
  ['crs', 'cours'],
  ['che', 'chemin'],
  ['ch', 'chemin'],
  ['rte', 'route'],
  ['qu', 'quai'],
  ['cite', 'cite'],
  ['villa', 'villa'],
  ['sent', 'sentier'],
  ['res', 'residence'],
]);

/**
 * Normalisation canonique d'un nom de voie (identique à TransactionValuationLogic.NormalizeStreet en C#).
 */
export function normalizeStreet(raw) {
  const folded = String(raw || '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, ' ')
    .trim();
  if (!folded) {
    return '';
  }
  const tokens = folded.split(' ').filter(Boolean);
  if (tokens.length > 1 && LEADING_STREET_ABBREVS.has(tokens[0])) {
    tokens[0] = LEADING_STREET_ABBREVS.get(tokens[0]);
  }
  for (let i = 0; i < tokens.length; i += 1) {
    if (tokens[i] === 'st') {
      tokens[i] = 'saint';
    } else if (tokens[i] === 'ste') {
      tokens[i] = 'sainte';
    }
  }
  return tokens.join(' ');
}

/**
 * Normalise le suffixe de numéro DVF (`B` / `BIS` -> `B`, `T` / `TER` -> `T`, `Q` / `QUATER` -> `Q`).
 */
export function normalizeSuffix(raw) {
  const cleaned = String(raw || '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toUpperCase()
    .replace(/[^A-Z0-9]/g, '');
  if (!cleaned) {
    return '';
  }
  if (cleaned === 'BIS') return 'B';
  if (cleaned === 'TER') return 'T';
  if (cleaned === 'QUATER') return 'Q';
  return cleaned.slice(0, 10);
}

/**
 * Formate T_AdresseNum (ex. "12" ou "12B") à partir des colonnes DVF adresse_numero + adresse_suffixe.
 */
export function formatHouseNumber(rawNum, rawSuffix) {
  const num = String(rawNum || '').trim().replace(/\.0+$/, '');
  const suffix = normalizeSuffix(rawSuffix);
  if (!num) {
    return { num: null, suffix: suffix || null };
  }
  return {
    num: (suffix ? `${num}${suffix}` : num).slice(0, 16),
    suffix: suffix || null,
  };
}

/**
 * Parse une ligne CSV RFC 4180 (virgules, guillemets doublés).
 */
export function parseCsvLine(line) {
  const out = [];
  let cur = '';
  let inQuotes = false;
  for (let i = 0; i < line.length; i += 1) {
    const ch = line[i];
    if (inQuotes) {
      if (ch === '"') {
        if (line[i + 1] === '"') {
          cur += '"';
          i += 1;
        } else {
          inQuotes = false;
        }
      } else {
        cur += ch;
      }
    } else if (ch === '"') {
      inQuotes = true;
    } else if (ch === ',') {
      out.push(cur);
      cur = '';
    } else {
      cur += ch;
    }
  }
  out.push(cur);
  return out;
}

function parsePositiveNumber(raw) {
  if (raw === undefined || raw === null || raw === '') {
    return null;
  }
  const n = Number(String(raw).replace(',', '.'));
  return Number.isFinite(n) && n > 0 ? n : null;
}

function parseIntOrNull(raw) {
  if (raw === undefined || raw === null || raw === '') {
    return null;
  }
  const n = Number.parseInt(String(raw), 10);
  return Number.isFinite(n) ? n : null;
}

function parseCoord(raw) {
  if (raw === undefined || raw === null || raw === '') {
    return null;
  }
  const n = Number(String(raw).replace(',', '.'));
  return Number.isFinite(n) ? n : null;
}

function round2(value) {
  return Math.round(value * 100) / 100;
}

/**
 * Empreinte déterministe SHA-256 (stable entre livraisons semestrielles DVF où id_mutation peut changer).
 */
export function computeFingerprint(tx) {
  const parts = [
    tx.dateTransaction,
    tx.natureMutation,
    tx.prix.toFixed(2),
    tx.idParcelle || '',
    tx.cp,
    tx.adresseNum || '',
    tx.adresseRueNorm,
    tx.typeBien,
    tx.surfaceBatie != null ? tx.surfaceBatie.toFixed(2) : '',
    tx.nbPieces != null ? String(tx.nbPieces) : '',
  ];
  return crypto.createHash('sha256').update(parts.join('|')).digest('hex');
}

/**
 * Agrège les lignes brutes d'une même (id_mutation, numero_disposition) en 0 ou 1 transaction résidentielle ancienne.
 * Retourne `{ status: 'ok', tx }` ou `{ status: 'skip', reason }`.
 */
export function aggregateMutationRows(rows, allowedNatures = DEFAULT_NATURES) {
  if (!rows || rows.length === 0) {
    return { status: 'skip', reason: 'empty' };
  }
  const first = rows[0];
  const nature = String(first.nature_mutation || '').trim();
  if (!allowedNatures.has(nature)) {
    return { status: 'skip', reason: 'nature' };
  }
  const dateTransaction = String(first.date_mutation || '').trim();
  if (!/^\d{4}-\d{2}-\d{2}$/.test(dateTransaction)) {
    return { status: 'skip', reason: 'date' };
  }
  const prix = parsePositiveNumber(first.valeur_fonciere);
  if (prix == null || prix < 1000) {
    return { status: 'skip', reason: 'price' };
  }

  const residential = [];
  let nbDependances = 0;
  let surfaceTerrain = 0;
  let natureCulture = null;
  const lotMap = new Map();
  let maxNbLots = null;

  for (const row of rows) {
    const typeLocal = String(row.type_local || '').trim();
    if (RESIDENTIAL_TYPES.has(typeLocal)) {
      residential.push(row);
    } else if (typeLocal === 'Dépendance' || typeLocal === 'Dependance') {
      nbDependances += 1;
    } else if (typeLocal) {
      // Local industriel/commercial dans le même acte : prix mixte non comparable au m² résidentiel.
      return { status: 'skip', reason: 'mixed_commercial' };
    }

    const terrain = parsePositiveNumber(row.surface_terrain);
    if (terrain != null) {
      surfaceTerrain += terrain;
    }
    if (!natureCulture && row.nature_culture) {
      natureCulture = String(row.nature_culture).trim().slice(0, 80);
    }

    const nl = parseIntOrNull(row.nombre_lots);
    if (nl != null && nl > 0 && (maxNbLots == null || nl > maxNbLots)) {
      maxNbLots = nl;
    }

    for (let k = 1; k <= 5; k += 1) {
      const lotNum = String(row[`lot${k}_numero`] ?? row[`lot_${k}_numero`] ?? '').trim();
      const lotCarrez = parsePositiveNumber(row[`lot${k}_surface_carrez`] ?? row[`lot_${k}_surface_carrez`]);
      if (lotNum || lotCarrez != null) {
        const key = lotNum || `lot_${k}_${lotCarrez}`;
        const prev = lotMap.get(key);
        if (!prev || (lotCarrez != null && (prev.carrez == null || lotCarrez > prev.carrez))) {
          lotMap.set(key, { num: lotNum || null, carrez: lotCarrez });
        }
      }
    }
  }

  if (residential.length === 0) {
    return { status: 'skip', reason: 'non_residential' };
  }

  // Au-delà de 2 locaux résidentiels dans un même acte, il s'agit en général d'une vente en bloc / immeuble.
  if (residential.length > 2) {
    return { status: 'skip', reason: 'multi_unit_block' };
  }

  const primary = residential[0];
  if (residential.length === 2) {
    const second = residential[1];
    const sameType = String(primary.type_local || '').trim() === String(second.type_local || '').trim();
    const sameRue = normalizeStreet(primary.adresse_nom_voie) === normalizeStreet(second.adresse_nom_voie);
    const sameNum = String(primary.adresse_numero || '').trim() === String(second.adresse_numero || '').trim();
    if (!sameType || !sameRue || !sameNum) {
      return { status: 'skip', reason: 'multi_unit_distinct' };
    }
  }

  const cp = String(primary.code_postal || '').replace(/\D/g, '').padStart(5, '0');
  const adresseRue = String(primary.adresse_nom_voie || '').trim().replace(/\s+/g, ' ').slice(0, 160);
  const adresseRueNorm = normalizeStreet(adresseRue).slice(0, 160);
  if (cp.length !== 5 || cp === '00000' || !adresseRueNorm) {
    return { status: 'skip', reason: 'incomplete_address' };
  }

  const { num: adresseNum, suffix: adresseSuffixe } = formatHouseNumber(primary.adresse_numero, primary.adresse_suffixe);
  const typeBien = String(primary.type_local || '').trim().slice(0, 50);

  let surfaceBatieSum = 0;
  let hasSurfaceBatie = false;
  let nbPiecesSum = 0;
  let hasNbPieces = false;
  for (const r of residential) {
    const sb = parsePositiveNumber(r.surface_reelle_bati);
    if (sb != null) {
      surfaceBatieSum += sb;
      hasSurfaceBatie = true;
    }
    const np = parseIntOrNull(r.nombre_pieces_principales);
    if (np != null && np > 0) {
      nbPiecesSum += np;
      hasNbPieces = true;
    }
  }
  const surfaceBatie = hasSurfaceBatie ? round2(surfaceBatieSum) : null;
  const nbPieces = hasNbPieces ? Math.min(255, nbPiecesSum) : null;

  const lotsArr = [...lotMap.values()];
  let carrezSum = 0;
  let hasCarrez = false;
  for (const l of lotsArr) {
    if (l.carrez != null) {
      carrezSum += l.carrez;
      hasCarrez = true;
    }
  }
  const surfaceCarrez = hasCarrez ? round2(carrezSum) : null;
  const lotsJson = lotsArr.length > 0 ? JSON.stringify(lotsArr).slice(0, 500) : null;

  const surfaceForPxm = surfaceCarrez ?? surfaceBatie;
  const prixM2 = surfaceForPxm != null && surfaceForPxm > 0 ? round2(prix / surfaceForPxm) : null;

  const tx = {
    dateTransaction,
    natureMutation: nature.slice(0, 60),
    prix: round2(prix),
    adresseNum,
    adresseSuffixe,
    adresseRue,
    adresseRueNorm,
    cp,
    commune: String(primary.nom_commune || '').trim().slice(0, 100) || null,
    codeDepartement: String(primary.code_departement || cp.slice(0, 2)).trim().slice(0, 3),
    typeBien,
    nbPieces,
    surfaceBatie,
    prixM2,
    surfaceCarrez,
    nbLots: maxNbLots,
    lotsJson,
    nbDependances: Math.min(255, nbDependances),
    surfaceTerrain: surfaceTerrain > 0 ? round2(surfaceTerrain) : null,
    natureCulture,
    idMutation: String(primary.id_mutation || '').trim().slice(0, 40) || null,
    idParcelle: String(primary.id_parcelle || '').trim().toUpperCase().slice(0, 14) || null,
    codeCommuneInsee: String(primary.code_commune || '').trim().slice(0, 5) || null,
    codeVoieFantoir: String(primary.adresse_code_voie || '').trim().slice(0, 6) || null,
    lat: parseCoord(primary.latitude),
    lon: parseCoord(primary.longitude),
  };
  tx.fingerprint = computeFingerprint(tx);
  return { status: 'ok', tx };
}

function sqlVal(value) {
  if (value === null || value === undefined) {
    return 'NULL';
  }
  if (typeof value === 'number') {
    return Number.isFinite(value) ? String(value) : 'NULL';
  }
  return `'${String(value).replace(/\\/g, '\\\\').replace(/'/g, "''")}'`;
}

const INSERT_COLS = [
  'T_DateTransaction',
  'T_NatureMutation',
  'T_Prix',
  'T_AdresseNum',
  'T_AdresseSuffixe',
  'T_AdresseRue',
  'T_AdresseRueNorm',
  'T_CP',
  'T_Commune',
  'T_CodeDepartement',
  'T_TypeBien',
  'T_NbPieces',
  'T_SurfaceBatie',
  'T_PrixM2',
  'T_SurfaceCarrez',
  'T_NbLots',
  'T_LotsJson',
  'T_NbDependances',
  'T_SurfaceTerrain',
  'T_NatureCulture',
  'T_IdMutation',
  'T_Fingerprint',
  'T_IdParcelle',
  'T_CodeCommuneInsee',
  'T_CodeVoieFantoir',
  'T_Lat',
  'T_Lon',
  'T_Source',
  'T_DateImport',
];

const ON_DUPLICATE_CLAUSE = [
  'T_IdMutation = VALUES(T_IdMutation)',
  'T_SurfaceCarrez = COALESCE(VALUES(T_SurfaceCarrez), T_SurfaceCarrez)',
  'T_NbLots = COALESCE(VALUES(T_NbLots), T_NbLots)',
  'T_LotsJson = COALESCE(VALUES(T_LotsJson), T_LotsJson)',
  'T_NbDependances = VALUES(T_NbDependances)',
  'T_SurfaceTerrain = COALESCE(VALUES(T_SurfaceTerrain), T_SurfaceTerrain)',
  'T_NatureCulture = COALESCE(VALUES(T_NatureCulture), T_NatureCulture)',
  'T_Lat = COALESCE(VALUES(T_Lat), T_Lat)',
  'T_Lon = COALESCE(VALUES(T_Lon), T_Lon)',
  'T_PrixM2 = CASE WHEN T_SurfaceNormee IS NOT NULL AND T_SurfaceNormee > 0 THEN ROUND(VALUES(T_Prix) / T_SurfaceNormee, 2) ELSE VALUES(T_PrixM2) END',
  'T_DateMaj = NOW()',
].join(', ');

export function buildBatchInsertSql(batch, importStamp) {
  const tuples = batch.map((tx) => '(' + [
    sqlVal(tx.dateTransaction),
    sqlVal(tx.natureMutation),
    sqlVal(tx.prix),
    sqlVal(tx.adresseNum),
    sqlVal(tx.adresseSuffixe),
    sqlVal(tx.adresseRue),
    sqlVal(tx.adresseRueNorm),
    sqlVal(tx.cp),
    sqlVal(tx.commune),
    sqlVal(tx.codeDepartement),
    sqlVal(tx.typeBien),
    sqlVal(tx.nbPieces),
    sqlVal(tx.surfaceBatie),
    sqlVal(tx.prixM2),
    sqlVal(tx.surfaceCarrez),
    sqlVal(tx.nbLots),
    sqlVal(tx.lotsJson),
    sqlVal(tx.nbDependances),
    sqlVal(tx.surfaceTerrain),
    sqlVal(tx.natureCulture),
    sqlVal(tx.idMutation),
    sqlVal(tx.fingerprint),
    sqlVal(tx.idParcelle),
    sqlVal(tx.codeCommuneInsee),
    sqlVal(tx.codeVoieFantoir),
    sqlVal(tx.lat),
    sqlVal(tx.lon),
    sqlVal('DVF'),
    sqlVal(importStamp),
  ].join(', ') + ')');
  return `INSERT INTO Transactions (${INSERT_COLS.join(', ')}) VALUES\n${tuples.join(',\n')}\nON DUPLICATE KEY UPDATE ${ON_DUPLICATE_CLAUSE};\n`;
}

/**
 * Convertit une ligne DGFiP brute (séparateur `|`, dates JJ/MM/AAAA, décimales avec `,`)
 * vers le format d'objet normalisé attendu par `aggregateMutationRows`.
 */
export function mapDgfipTxtRowToGeoRow(values, idx) {
  const get = (name) => {
    const i = idx[name];
    return i !== undefined ? String(values[i] ?? '').trim() : '';
  };
  const dec = (s) => (s ? s.replace(',', '.') : '');

  const rawDate = get('Date mutation');
  let isoDate = '';
  const dm = rawDate.match(/^(\d{2})\/(\d{2})\/(\d{4})$/);
  if (dm) {
    isoDate = `${dm[3]}-${dm[2]}-${dm[1]}`;
  } else if (/^\d{4}-\d{2}-\d{2}$/.test(rawDate)) {
    isoDate = rawDate;
  }

  const dept = get('Code departement');
  const rawComm = get('Code commune');
  const codeCommune = dept && rawComm
    ? (dept.length === 3 ? `${dept}${rawComm.padStart(2, '0')}` : `${dept.padStart(2, '0')}${rawComm.padStart(3, '0')}`)
    : '';

  const rawCp = get('Code postal');
  const cp = /^\d{4,5}$/.test(rawCp) ? rawCp.padStart(5, '0') : rawCp;

  const typeVoie = get('Type de voie');
  const voie = get('Voie');
  const nomVoie = [typeVoie, voie].filter(Boolean).join(' ');

  const pref = (get('Prefixe de section') || '000').padStart(3, '0');
  const sec = get('Section').padStart(2, '0');
  const plan = get('No plan').padStart(4, '0');
  const idParcelle = codeCommune && sec.trim() && plan.trim() && get('Section') && get('No plan')
    ? `${codeCommune}${pref}${sec}${plan}`
    : '';

  return {
    id_mutation: '',
    numero_disposition: get('No disposition'),
    date_mutation: isoDate,
    nature_mutation: get('Nature mutation'),
    valeur_fonciere: dec(get('Valeur fonciere')),
    adresse_numero: get('No voie'),
    adresse_suffixe: get('B/T/Q'),
    adresse_nom_voie: nomVoie,
    adresse_code_voie: get('Code voie'),
    code_postal: cp,
    code_commune: codeCommune,
    nom_commune: get('Commune'),
    code_departement: dept,
    id_parcelle: idParcelle,
    lot1_numero: get('1er lot'),
    lot1_surface_carrez: dec(get('Surface Carrez du 1er lot')),
    lot2_numero: get('2eme lot'),
    lot2_surface_carrez: dec(get('Surface Carrez du 2eme lot')),
    lot3_numero: get('3eme lot'),
    lot3_surface_carrez: dec(get('Surface Carrez du 3eme lot')),
    lot4_numero: get('4eme lot'),
    lot4_surface_carrez: dec(get('Surface Carrez du 4eme lot')),
    lot5_numero: get('5eme lot'),
    lot5_surface_carrez: dec(get('Surface Carrez du 5eme lot')),
    nombre_lots: get('Nombre de lots'),
    type_local: get('Type local'),
    surface_reelle_bati: dec(get('Surface reelle bati')),
    nombre_pieces_principales: get('Nombre pieces principales'),
    nature_culture: get('Nature culture'),
    surface_terrain: dec(get('Surface terrain')),
    latitude: '',
    longitude: '',
  };
}

/**
 * Sélectionne les ressources officielles `full-YYYY*.csv.gz` depuis la réponse API data.gouv.fr,
 * et complète si besoin avec les archives DGFiP 2014-2020 pour les années explicitement demandées.
 * Retourne un tableau trié par année croissante : [{ year, title, format, url, lastModified }].
 */
export function selectDvfResourcesForYears(resources, requestedYears = null) {
  const byYear = new Map();
  for (const r of resources || []) {
    const url = String(r?.url || '');
    const m = url.match(/\/full-(\d{4})(?:-s[12])?\.csv\.gz$/i);
    if (!m) continue;
    const year = Number.parseInt(m[1], 10);
    if (!Number.isFinite(year)) continue;
    if (requestedYears && requestedYears.length > 0 && !requestedYears.includes(year)) {
      continue;
    }
    const existing = byYear.get(year);
    const mod = String(r.last_modified || '');
    if (!existing || mod > existing.lastModified) {
      byYear.set(year, {
        year,
        title: String(r.title || `DVF ${year}`),
        format: 'geo-csv-gz',
        url,
        lastModified: mod,
      });
    }
  }
  if (requestedYears && requestedYears.length > 0) {
    for (const y of requestedYears) {
      if (!byYear.has(y) && HISTORICAL_DGFIP_RESOURCES.has(y)) {
        const hist = HISTORICAL_DGFIP_RESOURCES.get(y);
        byYear.set(y, { ...hist, lastModified: '' });
      }
    }
  }
  return Array.from(byYear.values()).sort((a, b) => a.year - b.year);
}

async function discoverDvfResources(requestedYears = null) {
  const onlyHistorical = requestedYears && requestedYears.length > 0
    && requestedYears.every((y) => HISTORICAL_DGFIP_RESOURCES.has(y));
  if (onlyHistorical) {
    return selectDvfResourcesForYears([], requestedYears);
  }
  const res = await fetch(DVF_DATASET_API_URL, {
    headers: { 'User-Agent': 'PerleRareCRM-ImportDVF/2.0' },
  });
  if (!res.ok) {
    throw new Error(`HTTP ${res.status} lors de la résolution du jeu DVF sur data.gouv.fr`);
  }
  const dataset = await res.json();
  return selectDvfResourcesForYears(dataset.resources || [], requestedYears);
}

async function downloadToTempFile(url, ext = '.dat', maxRetries = 3) {
  const tmpPath = path.join(os.tmpdir(), `dvf-${Date.now()}-${Math.random().toString(36).slice(2)}${ext}`);
  let attempt = 0;
  while (true) {
    try {
      const res = await fetch(url, {
        headers: { 'User-Agent': 'PerleRareCRM-ImportDVF/2.0' },
      });
      if (!res.ok) {
        throw new Error(`HTTP ${res.status} pour ${url}`);
      }
      const ws = fs.createWriteStream(tmpPath);
      await new Promise((resolve, reject) => {
        const rs = Readable.fromWeb(res.body);
        rs.on('error', reject);
        ws.on('error', reject);
        ws.on('finish', resolve);
        rs.pipe(ws);
      });
      return tmpPath;
    } catch (err) {
      fs.rmSync(tmpPath, { force: true });
      attempt += 1;
      if (attempt >= maxRetries) throw err;
      await new Promise((r) => setTimeout(r, 1500 * attempt));
    }
  }
}

async function processDgfipTxtResource(resource, deptSet, onTransaction, stats) {
  const isZip = resource.format === 'dgfip-txt-zip';
  const tmpPath = await downloadToTempFile(resource.url, isZip ? '.zip' : '.txt');
  let extractDir = null;
  let txtPath = tmpPath;

  try {
    if (isZip) {
      extractDir = fs.mkdtempSync(path.join(os.tmpdir(), 'dvf-unzip-'));
      execFileSync('tar', ['-xf', tmpPath, '-C', extractDir], { stdio: 'ignore' });
      const files = fs.readdirSync(extractDir).filter((f) => f.toLowerCase().endsWith('.txt'));
      if (files.length === 0) {
        throw new Error(`Aucun fichier .txt trouvé dans ${resource.url}`);
      }
      txtPath = path.join(extractDir, files[0]);
    }

    const rl = readline.createInterface({
      input: fs.createReadStream(txtPath, { encoding: 'utf8' }),
      crlfDelay: Infinity,
    });

    let idx = null;
    let deptIdx = -1;
    let currentKey = null;
    let currentRows = [];
    let mutSeq = 0;

    const flush = () => {
      if (currentRows.length === 0) return;
      mutSeq += 1;
      const synthId = `${resource.year}-H${mutSeq}`;
      for (const r of currentRows) r.id_mutation = synthId;
      stats.mutations += 1;
      const agg = aggregateMutationRows(currentRows);
      if (agg.status === 'ok') {
        stats.kept += 1;
        onTransaction(agg.tx);
      } else {
        stats.skipped[agg.reason] = (stats.skipped[agg.reason] || 0) + 1;
      }
      currentRows = [];
    };

    for await (const line of rl) {
      if (!line) continue;
      if (!idx) {
        const header = line.replace(/^\uFEFF/, '').split('|');
        idx = {};
        for (let i = 0; i < header.length; i += 1) {
          idx[header[i].trim()] = i;
        }
        deptIdx = idx['Code departement'] ?? -1;
        continue;
      }
      stats.rawRows += 1;
      const values = line.split('|');
      if (deptIdx >= 0) {
        const d = String(values[deptIdx] ?? '').trim();
        if (!deptSet.has(d)) {
          if (currentRows.length > 0) {
            flush();
            currentKey = null;
          }
          continue;
        }
      }
      const row = mapDgfipTxtRowToGeoRow(values, idx);
      const key = `${row.date_mutation}|${row.nature_mutation}|${row.valeur_fonciere}|${row.numero_disposition}|${row.code_departement}|${row.code_commune}`;
      if (currentKey !== null && key !== currentKey) {
        flush();
      }
      currentKey = key;
      currentRows.push(row);
    }
    flush();
  } finally {
    fs.rmSync(tmpPath, { force: true });
    if (extractDir) fs.rmSync(extractDir, { recursive: true, force: true });
  }
}

async function processNationalYearResource(resource, deptSet, onTransaction, stats) {
  if (resource.format === 'dgfip-txt' || resource.format === 'dgfip-txt-zip') {
    return processDgfipTxtResource(resource, deptSet, onTransaction, stats);
  }
  const tmpPath = await downloadToTempFile(resource.url, '.csv.gz');
  try {
    const gunzip = zlib.createGunzip();
    const nodeStream = fs.createReadStream(tmpPath).pipe(gunzip);
    const rl = readline.createInterface({ input: nodeStream, crlfDelay: Infinity });

    let headers = null;
    let deptIdx = -1;
    let currentKey = null;
    let currentRows = [];

    const flush = () => {
      if (currentRows.length === 0) {
        return;
      }
      stats.mutations += 1;
      const agg = aggregateMutationRows(currentRows);
      if (agg.status === 'ok') {
        stats.kept += 1;
        onTransaction(agg.tx);
      } else {
        stats.skipped[agg.reason] = (stats.skipped[agg.reason] || 0) + 1;
      }
      currentRows = [];
    };

    for await (const line of rl) {
      if (!line) {
        continue;
      }
      if (!headers) {
        headers = parseCsvLine(line);
        deptIdx = headers.indexOf('code_departement');
        continue;
      }
      stats.rawRows += 1;
      const values = parseCsvLine(line);
      if (deptIdx >= 0) {
        const d = String(values[deptIdx] ?? '').trim();
        if (!deptSet.has(d)) {
          if (currentRows.length > 0) {
            flush();
            currentKey = null;
          }
          continue;
        }
      }
      const row = {};
      for (let i = 0; i < headers.length; i += 1) {
        row[headers[i]] = values[i] ?? '';
      }
      const key = `${row.id_mutation || ''}|${row.numero_disposition || ''}`;
      if (currentKey !== null && key !== currentKey) {
        flush();
      }
      currentKey = key;
      currentRows.push(row);
    }
    flush();
  } finally {
    fs.rmSync(tmpPath, { force: true });
  }
}

async function backfillMissingCoords(conn) {
  const [res] = await conn.query(
    `UPDATE Transactions t
     INNER JOIN (
       SELECT T_CP, T_AdresseRueNorm, T_AdresseNum,
              ROUND(AVG(T_Lat), 7) AS lat, ROUND(AVG(T_Lon), 7) AS lon
       FROM Transactions
       WHERE T_Lat IS NOT NULL AND T_Lon IS NOT NULL AND T_AdresseNum IS NOT NULL
       GROUP BY T_CP, T_AdresseRueNorm, T_AdresseNum
     ) g
       ON g.T_CP = t.T_CP
      AND g.T_AdresseRueNorm = t.T_AdresseRueNorm
      AND g.T_AdresseNum = t.T_AdresseNum
     SET t.T_Lat = g.lat, t.T_Lon = g.lon
     WHERE t.T_Lat IS NULL AND t.T_AdresseNum IS NOT NULL`
  );
  return Number(res?.affectedRows || 0);
}

function loadMysql2() {
  const req = createRequire(import.meta.url);
  const backendRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
  const candidates = [
    path.join(backendRoot, 'node_modules', 'mysql2', 'promise.js'),
    path.resolve(backendRoot, '..', 'perlerareFrontWeb', 'node_modules', 'mysql2', 'promise.js'),
  ];
  for (const p of candidates) {
    if (fs.existsSync(p)) return req(p);
  }
  const cache = path.join(os.homedir(), 'AppData', 'Local', 'npm-cache', '_npx');
  if (fs.existsSync(cache)) {
    const dir = fs.readdirSync(cache).find((d) => fs.existsSync(path.join(cache, d, 'node_modules', 'mysql2')));
    if (dir) {
      return req(path.join(cache, dir, 'node_modules', 'mysql2', 'promise'));
    }
  }
  return req('mysql2/promise');
}

function resolveDbConfig(appsettingsPath) {
  const json = JSON.parse(fs.readFileSync(appsettingsPath, 'utf8'));
  const cs = json?.ConnectionStrings?.PerleRareDB;
  if (!cs) throw new Error(`ConnectionStrings:PerleRareDB introuvable dans ${appsettingsPath}`);
  const map = {};
  for (const part of String(cs).split(';')) {
    const idx = part.indexOf('=');
    if (idx <= 0) continue;
    map[part.slice(0, idx).trim().toLowerCase()] = part.slice(idx + 1).trim();
  }
  return {
    host: map.server || map.host || '127.0.0.1',
    port: parseInt(map.port || '3306', 10),
    database: map.database,
    user: map.user || map.uid || map['user id'],
    password: map.password || map.pwd,
  };
}

function parseCliArgs(argv) {
  const backendRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
  const opts = {
    depts: DEFAULT_DEPTS,
    years: null,
    outSql: null,
    apply: false,
    backfillGeoOnly: false,
    appsettings: path.join(backendRoot, 'api-perle-rare-decompiled', 'appsettings.Development.json'),
    batchSize: 500,
    dryRun: true,
  };
  for (const arg of argv) {
    if (arg === '--dry-run') {
      opts.dryRun = true;
      opts.apply = false;
    } else if (arg === '--apply') {
      opts.apply = true;
      opts.dryRun = false;
    } else if (arg === '--backfill-geo-only') {
      opts.backfillGeoOnly = true;
      opts.dryRun = false;
    } else if (arg.startsWith('--depts=')) {
      opts.depts = arg.slice('--depts='.length).split(',').map((d) => d.trim()).filter(Boolean);
    } else if (arg.startsWith('--years=')) {
      opts.years = arg.slice('--years='.length).split(',').map((y) => Number.parseInt(y.trim(), 10)).filter(Number.isFinite);
    } else if (arg.startsWith('--out-sql=')) {
      opts.outSql = arg.slice('--out-sql='.length).trim();
      opts.dryRun = false;
    } else if (arg.startsWith('--appsettings=')) {
      opts.appsettings = arg.slice('--appsettings='.length).trim();
    } else if (arg.startsWith('--batch-size=')) {
      const n = Number.parseInt(arg.slice('--batch-size='.length), 10);
      if (Number.isFinite(n) && n > 0 && n <= 5000) {
        opts.batchSize = n;
      }
    }
  }
  return opts;
}

async function main() {
  const opts = parseCliArgs(process.argv.slice(2));
  if (opts.backfillGeoOnly) {
    const mysql = loadMysql2();
    const conn = await mysql.createConnection(resolveDbConfig(opts.appsettings));
    try {
      const geoFilled = await backfillMissingCoords(conn);
      console.log(`Géocodage recopié depuis adresses connues : +${geoFilled} lignes`);
    } finally {
      await conn.end();
    }
    return;
  }
  const resources = await discoverDvfResources(opts.years);

  if (resources.length === 0) {
    throw new Error('Aucun millésime DVF géolocalisé trouvé sur data.gouv.fr pour les années demandées.');
  }

  const modeLabel = opts.apply ? 'apply-db' : (opts.outSql ? `sql:${opts.outSql}` : 'dry-run');
  console.log(
    `DVF géolocalisé : départements=[${opts.depts.join(',')}] millésimes=[${resources.map((r) => r.year).join(',')}] mode=${modeLabel}`
  );

  let sqlStream = null;
  if (!opts.dryRun && opts.outSql) {
    fs.mkdirSync(path.dirname(path.resolve(opts.outSql)), { recursive: true });
    sqlStream = fs.createWriteStream(opts.outSql, { encoding: 'utf8' });
    sqlStream.write('-- Import DVF géolocalisé (data.gouv.fr) vers Transactions\n');
    sqlStream.write('SET NAMES utf8mb4;\n');
    sqlStream.write('SET autocommit = 0;\n\n');
  }

  let dbConn = null;
  if (opts.apply) {
    const mysql = loadMysql2();
    dbConn = await mysql.createConnection(resolveDbConfig(opts.appsettings));
  }

  const importStamp = new Date().toISOString().slice(0, 19).replace('T', ' ');
  const deptSet = new Set(opts.depts);
  const seenFingerprints = new Set();
  let batch = [];
  let writtenBatches = 0;
  let dbAffectedRows = 0;

  const stats = {
    rawRows: 0,
    mutations: 0,
    kept: 0,
    duplicatesInRun: 0,
    skipped: {},
    byDept: {},
    byYear: {},
  };

  const flushBatch = async () => {
    if (batch.length === 0) return;
    const sql = buildBatchInsertSql(batch, importStamp);
    if (sqlStream) {
      sqlStream.write(sql);
      writtenBatches += 1;
      if (writtenBatches % 20 === 0) {
        sqlStream.write('COMMIT;\n');
      }
    }
    if (dbConn) {
      const [res] = await dbConn.query(sql);
      dbAffectedRows += Number(res?.affectedRows || 0);
      writtenBatches += 1;
    }
    batch = [];
  };

  try {
    for (const resource of resources) {
      const before = stats.kept;
      const pendingFlush = [];
      await processNationalYearResource(resource, deptSet, (tx) => {
        if (seenFingerprints.has(tx.fingerprint)) {
          stats.duplicatesInRun += 1;
          return;
        }
        seenFingerprints.add(tx.fingerprint);
        stats.byDept[tx.codeDepartement] = (stats.byDept[tx.codeDepartement] || 0) + 1;
        stats.byYear[resource.year] = (stats.byYear[resource.year] || 0) + 1;
        if (sqlStream || dbConn) {
          batch.push(tx);
          if (batch.length >= opts.batchSize) {
            pendingFlush.push(batch);
            batch = [];
          }
        }
      }, stats);

      for (const b of pendingFlush) {
        const sql = buildBatchInsertSql(b, importStamp);
        if (sqlStream) {
          sqlStream.write(sql);
          writtenBatches += 1;
          if (writtenBatches % 20 === 0) sqlStream.write('COMMIT;\n');
        }
        if (dbConn) {
          const [res] = await dbConn.query(sql);
          dbAffectedRows += Number(res?.affectedRows || 0);
          writtenBatches += 1;
        }
      }
      await flushBatch();
      const delta = stats.kept - before;
      console.log(`  ${resource.title} (${resource.year}) : +${delta} transactions retenues`);
    }

    if (dbConn && resources.some((r) => r.format && r.format.startsWith('dgfip-txt'))) {
      const geoFilled = await backfillMissingCoords(dbConn);
      console.log(`  Géocodage recopié depuis adresses connues : +${geoFilled} lignes`);
    }

    if (sqlStream) {
      sqlStream.write('\nCOMMIT;\nSET autocommit = 1;\n');
      await new Promise((resolve, reject) => {
        sqlStream.end((err) => (err ? reject(err) : resolve()));
      });
    }
  } finally {
    if (dbConn) await dbConn.end();
  }

  console.log('\nRésumé :');
  console.log(JSON.stringify({
    rawRows: stats.rawRows,
    mutations: stats.mutations,
    uniqueTransactions: seenFingerprints.size,
    duplicatesInRun: stats.duplicatesInRun,
    byYear: stats.byYear,
    byDept: stats.byDept,
    skipped: stats.skipped,
    dbAffectedRows: opts.apply ? dbAffectedRows : null,
    outSql: opts.outSql || null,
  }, null, 2));
}

const currentFile = fileURLToPath(import.meta.url);
if (process.argv[1] && path.resolve(process.argv[1]) === path.resolve(currentFile)) {
  main().catch((err) => {
    console.error('Erreur import DVF :', err);
    process.exit(1);
  });
}
