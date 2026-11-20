#!/usr/bin/env node
/**
 * Enrichit la table MariaDB `Transactions` à partir de la table `property` (annonces historiques Yanport) :
 * - Rapprochement lot-à-lot (même adresse exacte CP + voie normalisée + n°, type compatible,
 *   surface proche, prix cohérent, fenêtre temporelle avant acte) -> T_PropertyId, T_Etage,
 *   T_NbEtages, T_DernierEtage, T_NbChambres, T_Ascenseur, T_Dpe, T_PhotoUrl.
 * - Inférence au niveau immeuble (même adresse exacte pour Appartement) -> T_Ascenseur
 *   (Oui si >= 1 annonce Appartement de l'immeuble a P_Asc = 1 ; Non si >= 2 annonces toutes à 0)
 *   et T_NbEtages (mode immeuble).
 * - Ne remplace jamais une valeur déjà renseignée dans Transactions (COALESCE côté SQL).
 *
 * Exemples :
 *   node scripts/enrich-transactions-from-property.mjs --dry-run
 *   node scripts/enrich-transactions-from-property.mjs --apply
 */

import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';
import { normalizeStreet, normalizeSuffix } from './import-dvf-transactions.mjs';

const require = createRequire(import.meta.url);
export const DEFAULT_DEPTS = ['75', '92', '93', '94'];

const HOUSE_NUM_RE = /^(\d+)\s*([a-zA-Z]+)?$/;

/**
 * Normalise un numéro de rue (`12 bis`, `12bis`, `12 B` -> `12B`, `45` -> `45`).
 */
export function normalizeHouseNumberKey(raw) {
  const trimmed = String(raw || '').trim();
  if (!trimmed) {
    return '';
  }
  const m = trimmed.match(HOUSE_NUM_RE);
  if (!m) {
    return trimmed.replace(/\s+/g, '').toUpperCase();
  }
  const digits = m[1];
  const suffix = normalizeSuffix(m[2] || '');
  return suffix ? `${digits}${suffix}` : digits;
}

export function buildAddressKey(cp, streetNorm, numKey) {
  if (!cp || !streetNorm || !numKey) {
    return '';
  }
  return `${cp}|${streetNorm}|${numKey}`;
}

export function extractFirstPhotoUrl(rawImages) {
  if (!rawImages) {
    return null;
  }
  const text = String(rawImages).trim();
  if (!text) {
    return null;
  }
  try {
    const parsed = JSON.parse(text);
    if (Array.isArray(parsed)) {
      for (const item of parsed) {
        if (typeof item === 'string' && /^https?:\/\//i.test(item.trim())) {
          return item.trim().slice(0, 500);
        }
      }
    }
  } catch {
    const match = text.match(/https?:\/\/[^\s"',\]]+/i);
    if (match) {
      return match[0].slice(0, 500);
    }
  }
  return null;
}

function normalizePropertyType(raw) {
  const s = String(raw || '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase();
  if (s.includes('appart') || s.includes('studio') || s.includes('duplex') || s.includes('loft') || s.includes('triplex')) {
    return 'Appartement';
  }
  if (s.includes('maison') || s.includes('villa') || s.includes('pavillon') || s.includes('hotel particulier')) {
    return 'Maison';
  }
  return '';
}

function parseDateMs(raw) {
  if (!raw) {
    return null;
  }
  if (raw instanceof Date) {
    const ms = raw.getTime();
    return Number.isFinite(ms) && raw.getUTCFullYear() >= 1990 ? ms : null;
  }
  const text = String(raw).trim();
  if (!text || /^0000-|^0001-/.test(text)) {
    return null;
  }
  const ms = Date.parse(text);
  if (!Number.isFinite(ms)) {
    return null;
  }
  return new Date(ms).getUTCFullYear() >= 1990 ? ms : null;
}

const DAY_MS = 86400000;

/**
 * Infère la présence d'ascenseur et le nombre d'étages d'un immeuble à partir des annonces Appartement.
 */
export function inferBuildingInfo(buildingProps) {
  const apts = (buildingProps || []).filter((p) => p.typeNorm === 'Appartement');
  if (apts.length === 0) {
    return { ascenseur: null, nbEtages: null };
  }
  let ascYes = 0;
  let ascNo = 0;
  const floorsCount = new Map();
  for (const p of apts) {
    if (p.asc === 1 || p.asc === true) {
      ascYes += 1;
    } else if (p.asc === 0 || p.asc === false) {
      ascNo += 1;
    }
    if (typeof p.nbEtages === 'number' && p.nbEtages > 0 && p.nbEtages <= 60) {
      floorsCount.set(p.nbEtages, (floorsCount.get(p.nbEtages) || 0) + 1);
    }
  }

  let ascenseur = null;
  if (ascYes >= 1) {
    ascenseur = 1;
  } else if (ascNo >= 2) {
    ascenseur = 0;
  }

  let nbEtages = null;
  let bestCount = 0;
  for (const [val, count] of floorsCount.entries()) {
    if (count > bestCount || (count === bestCount && (nbEtages == null || val > nbEtages))) {
      bestCount = count;
      nbEtages = val;
    }
  }
  return { ascenseur, nbEtages };
}

/**
 * Rapproche une transaction DVF des annonces `property` de la même adresse exacte.
 * Retourne un objet d'enrichissement (ou null si rien d'exploitable).
 */
export function matchTransactionToProperty(tx, buildingProps) {
  const building = tx.typeBien === 'Appartement' ? inferBuildingInfo(buildingProps) : { ascenseur: null, nbEtages: null };
  const txSurface = tx.surfaceNormee ?? tx.surfaceCarrez ?? tx.surfaceBatie;
  const txDateMs = parseDateMs(tx.dateTransaction);
  const txPrix = Number(tx.prix) || 0;

  const scored = [];
  if (buildingProps && buildingProps.length > 0 && txSurface > 0 && txDateMs != null) {
    const maxSurfDiff = Math.max(2.5, txSurface * 0.08);
    for (const p of buildingProps) {
      if (p.typeNorm && tx.typeBien && p.typeNorm !== tx.typeBien) {
        continue;
      }
      if (!(p.surface > 0)) {
        continue;
      }
      const surfDiff = Math.abs(p.surface - txSurface);
      if (surfDiff > maxSurfDiff) {
        continue;
      }
      const surfRel = surfDiff / txSurface;

      if (txPrix > 0 && p.prix > 0) {
        const priceRatio = p.prix / txPrix;
        if (priceRatio < 0.80 || priceRatio > 1.40) {
          continue;
        }
      }

      const debutMs = p.dateDebutMs;
      const finMs = p.dateFinMs;
      if (debutMs == null) {
        continue;
      }
      const debutDaysBefore = (txDateMs - debutMs) / DAY_MS;
      if (debutDaysBefore < -30 || debutDaysBefore > 730) {
        continue;
      }
      let dateScore = 0;
      if (finMs != null) {
        const finDaysBefore = (txDateMs - finMs) / DAY_MS;
        if (finDaysBefore < -60 || finDaysBefore > 540) {
          continue;
        }
        if (finDaysBefore >= 15 && finDaysBefore <= 210) {
          dateScore = 30;
        } else if (finDaysBefore >= -15 && finDaysBefore <= 365) {
          dateScore = 18;
        } else {
          dateScore = 6;
        }
      } else if (debutDaysBefore >= 30 && debutDaysBefore <= 365) {
        dateScore = 10;
      } else {
        continue;
      }

      let surfScore = 0;
      if (surfDiff <= 1.0) {
        surfScore = 40;
      } else if (surfRel <= 0.03) {
        surfScore = 30;
      } else if (surfRel <= 0.05) {
        surfScore = 20;
      } else {
        surfScore = 10;
      }

      let priceScore = 0;
      if (txPrix > 0 && p.prix > 0) {
        const r = p.prix / txPrix;
        if (r >= 0.98 && r <= 1.18) {
          priceScore = 25;
        } else if (r >= 0.90 && r <= 1.28) {
          priceScore = 14;
        } else {
          priceScore = 5;
        }
      }

      let piecesScore = 0;
      if (tx.nbPieces != null && p.nbPieces != null) {
        const dp = Math.abs(tx.nbPieces - p.nbPieces);
        if (dp === 0) piecesScore = 10;
        else if (dp === 1) piecesScore = 4;
        else if (dp >= 3 && surfRel > 0.02) continue;
      }

      let richnessScore = 0;
      if (p.etage != null) richnessScore += 12;
      if (p.nbChambres != null) richnessScore += 4;
      if (p.nbEtages != null) richnessScore += 4;
      if (p.photoUrl) richnessScore += 2;

      const totalScore = surfScore + dateScore + priceScore + piecesScore + richnessScore;
      if (totalScore >= 55) {
        scored.push({ p, totalScore, surfDiff });
      }
    }
  }

  scored.sort((a, b) => b.totalScore - a.totalScore || a.surfDiff - b.surfDiff);
  const best = scored[0]?.p || null;

  // Si plusieurs annonces quasi identiques (doublons d'agences sur le même bien), compléter un champ manquant (ex. étage).
  let etage = best?.etage ?? null;
  let nbEtages = best?.nbEtages ?? null;
  let dernierEtage = best?.dernierEtage ?? null;
  let nbChambres = best?.nbChambres ?? null;
  let dpe = best?.dpe ?? null;
  let photoUrl = best?.photoUrl ?? null;
  let ascenseur = best ? (best.asc === 1 ? 1 : (building.ascenseur ?? 0)) : building.ascenseur;

  if (best && scored.length > 1) {
    for (let i = 1; i < Math.min(scored.length, 5); i += 1) {
      const alt = scored[i];
      if (alt.totalScore < scored[0].totalScore - 20) {
        break;
      }
      if (etage == null && alt.p.etage != null) etage = alt.p.etage;
      if (nbEtages == null && alt.p.nbEtages != null) nbEtages = alt.p.nbEtages;
      if (dernierEtage == null && alt.p.dernierEtage != null) dernierEtage = alt.p.dernierEtage;
      if (nbChambres == null && alt.p.nbChambres != null) nbChambres = alt.p.nbChambres;
      if (dpe == null && alt.p.dpe != null) dpe = alt.p.dpe;
      if (photoUrl == null && alt.p.photoUrl != null) photoUrl = alt.p.photoUrl;
      if (ascenseur !== 1 && alt.p.asc === 1) ascenseur = 1;
    }
  }

  if (nbEtages == null && building.nbEtages != null) {
    nbEtages = building.nbEtages;
  }
  if (etage != null && nbEtages != null && dernierEtage == null && etage > 0) {
    dernierEtage = etage === nbEtages ? 1 : 0;
  }

  const hasAny =
    best != null
    || etage != null
    || nbEtages != null
    || ascenseur != null
    || nbChambres != null
    || dpe != null
    || photoUrl != null;

  if (!hasAny) {
    return null;
  }

  return {
    propertyId: best?.propertyId ?? null,
    etage,
    nbEtages,
    dernierEtage,
    nbChambres,
    ascenseur,
    dpe,
    photoUrl,
    matchedUnit: best != null,
  };
}

function loadMysql2() {
  const cache = path.join(os.homedir(), 'AppData', 'Local', 'npm-cache', '_npx');
  if (fs.existsSync(cache)) {
    const dir = fs.readdirSync(cache).find((d) => fs.existsSync(path.join(cache, d, 'node_modules', 'mysql2')));
    if (dir) {
      return require(path.join(cache, dir, 'node_modules', 'mysql2', 'promise'));
    }
  }
  return require('mysql2/promise');
}

function readDbConfig() {
  const cfgPath = path.resolve('api-perle-rare-decompiled/appsettings.Development.json');
  const cfg = JSON.parse(fs.readFileSync(cfgPath, 'utf8'));
  const cs = cfg.ConnectionStrings?.PerleRareDB;
  if (!cs) {
    throw new Error('ConnectionStrings:PerleRareDB introuvable dans appsettings.Development.json');
  }
  const parts = {};
  for (const seg of cs.split(';')) {
    const idx = seg.indexOf('=');
    if (idx > 0) {
      parts[seg.slice(0, idx).trim().toLowerCase()] = seg.slice(idx + 1).trim();
    }
  }
  return {
    host: parts.server || parts.host || '127.0.0.1',
    port: Number(parts.port || 3306),
    user: parts.uid || parts.user || parts['user id'],
    password: parts.pwd || parts.password,
    database: parts.database,
    charset: 'utf8mb4',
  };
}

function parseCliArgs(argv) {
  const opts = {
    depts: DEFAULT_DEPTS,
    apply: false,
    batchSize: 400,
  };
  for (const arg of argv) {
    if (arg === '--apply') opts.apply = true;
    else if (arg === '--dry-run') opts.apply = false;
    else if (arg.startsWith('--depts=')) {
      opts.depts = arg.slice('--depts='.length).split(',').map((d) => d.trim()).filter(Boolean);
    }
  }
  return opts;
}

function sqlVal(v) {
  if (v === null || v === undefined) return 'NULL';
  if (typeof v === 'number') return Number.isFinite(v) ? String(v) : 'NULL';
  return `'${String(v).replace(/\\/g, '\\\\').replace(/'/g, "''")}'`;
}

async function applyUpdates(conn, updates, batchSize) {
  for (let i = 0; i < updates.length; i += batchSize) {
    const chunk = updates.slice(i, i + batchSize);
    const ids = chunk.map((u) => u.id);
    const colCases = (field, colName, numeric = false) => {
      const whens = chunk
        .filter((u) => u[field] !== null && u[field] !== undefined)
        .map((u) => `WHEN ${u.id} THEN ${numeric ? u[field] : sqlVal(u[field])}`);
      if (whens.length === 0) {
        return null;
      }
      return `${colName} = COALESCE(${colName}, CASE T_Ref ${whens.join(' ')} END)`;
    };
    const sets = [
      colCases('propertyId', 'T_PropertyId', false),
      colCases('etage', 'T_Etage', true),
      colCases('nbEtages', 'T_NbEtages', true),
      colCases('dernierEtage', 'T_DernierEtage', true),
      colCases('nbChambres', 'T_NbChambres', true),
      colCases('ascenseur', 'T_Ascenseur', true),
      colCases('dpe', 'T_Dpe', false),
      colCases('photoUrl', 'T_PhotoUrl', false),
    ].filter(Boolean);
    if (sets.length === 0) {
      continue;
    }
    sets.push('T_DateMaj = NOW()');
    const sql = `UPDATE Transactions SET ${sets.join(', ')} WHERE T_Ref IN (${ids.join(',')})`;
    await conn.query(sql);
  }
}

async function main() {
  const opts = parseCliArgs(process.argv.slice(2));
  const mysql = loadMysql2();
  const conn = await mysql.createConnection(readDbConfig());
  const t0 = Date.now();
  const deptListSql = opts.depts.map((d) => `'${d.replace(/'/g, "''")}'`).join(',');

  console.log(`Enrichissement Transactions <- property : depts=[${opts.depts.join(',')}] mode=${opts.apply ? 'APPLY' : 'dry-run'}`);

  const [propRows] = await conn.query(
    `SELECT P_PropertyId, P_CP, P_AdresseNum, P_AdresseRue, P_Type,
            P_NbPieces, P_NbChambres, P_Etage, P_NbEtages, P_EstDernierEtage,
            P_Asc, P_Surface, P_Prix, P_DateDebut, P_DateFin,
            P_ConsumptionLetter, P_Images
     FROM property
     WHERE P_TypeTransaction = 'A'
       AND LEFT(P_CP, 2) IN (${deptListSql})
       AND P_AdresseNum IS NOT NULL AND P_AdresseNum <> ''
       AND P_AdresseRue IS NOT NULL AND P_AdresseRue <> ''`,
  );

  const byAddress = new Map();
  for (const r of propRows) {
    const cp = String(r.P_CP || '').replace(/\D/g, '').padStart(5, '0');
    const streetNorm = normalizeStreet(r.P_AdresseRue);
    const numKey = normalizeHouseNumberKey(r.P_AdresseNum);
    const key = buildAddressKey(cp, streetNorm, numKey);
    if (!key) continue;
    const item = {
      propertyId: String(r.P_PropertyId || '').trim() || null,
      typeNorm: normalizePropertyType(r.P_Type),
      nbPieces: r.P_NbPieces != null ? Number(r.P_NbPieces) : null,
      nbChambres: r.P_NbChambres != null ? Number(r.P_NbChambres) : null,
      etage: r.P_Etage != null ? Number(r.P_Etage) : null,
      nbEtages: r.P_NbEtages != null && Number(r.P_NbEtages) > 0 ? Number(r.P_NbEtages) : null,
      dernierEtage: r.P_EstDernierEtage != null ? (r.P_EstDernierEtage ? 1 : 0) : null,
      asc: r.P_Asc != null ? (r.P_Asc ? 1 : 0) : null,
      surface: r.P_Surface != null ? Number(r.P_Surface) : null,
      prix: r.P_Prix != null ? Number(r.P_Prix) : null,
      dateDebutMs: parseDateMs(r.P_DateDebut),
      dateFinMs: parseDateMs(r.P_DateFin),
      dpe: r.P_ConsumptionLetter && /^[A-G]$/i.test(String(r.P_ConsumptionLetter).trim())
        ? String(r.P_ConsumptionLetter).trim().toUpperCase()
        : null,
      photoUrl: extractFirstPhotoUrl(r.P_Images),
    };
    let list = byAddress.get(key);
    if (!list) {
      list = [];
      byAddress.set(key, list);
    }
    list.push(item);
  }

  console.log(`  property indexées : ${propRows.length} lignes sur ${byAddress.size} adresses (${((Date.now() - t0) / 1000).toFixed(1)}s)`);

  const stats = {
    scanned: 0,
    toUpdate: 0,
    matchedUnit: 0,
    withEtage: 0,
    withAscenseur: 0,
    withAscenseurYes: 0,
    withNbEtages: 0,
    withChambres: 0,
    withDpe: 0,
    withPhoto: 0,
    byDept: {},
  };

  for (const dept of opts.depts) {
    const [txRows] = await conn.query(
      `SELECT T_Ref, T_DateTransaction, T_CP, T_AdresseRueNorm, T_AdresseNum,
              T_TypeBien, T_NbPieces, T_Prix, T_SurfaceNormee, T_SurfaceCarrez, T_SurfaceBatie,
              T_PropertyId, T_Etage, T_NbEtages, T_DernierEtage, T_NbChambres, T_Ascenseur, T_Dpe, T_PhotoUrl
       FROM Transactions
       WHERE T_CodeDepartement = ?`,
      [dept],
    );

    const updates = [];
    const deptStats = { total: txRows.length, updated: 0, unit: 0, etage: 0, asc: 0, chambres: 0 };

    for (const r of txRows) {
      stats.scanned += 1;
      const key = buildAddressKey(r.T_CP, r.T_AdresseRueNorm, normalizeHouseNumberKey(r.T_AdresseNum));
      const buildingProps = key ? byAddress.get(key) : null;
      if (!buildingProps || buildingProps.length === 0) {
        continue;
      }

      const match = matchTransactionToProperty({
        dateTransaction: r.T_DateTransaction,
        typeBien: r.T_TypeBien,
        nbPieces: r.T_NbPieces != null ? Number(r.T_NbPieces) : null,
        prix: Number(r.T_Prix),
        surfaceNormee: r.T_SurfaceNormee != null ? Number(r.T_SurfaceNormee) : null,
        surfaceCarrez: r.T_SurfaceCarrez != null ? Number(r.T_SurfaceCarrez) : null,
        surfaceBatie: r.T_SurfaceBatie != null ? Number(r.T_SurfaceBatie) : null,
      }, buildingProps);

      if (!match) {
        continue;
      }

      const delta = {
        id: r.T_Ref,
        propertyId: r.T_PropertyId == null ? match.propertyId : null,
        etage: r.T_Etage == null ? match.etage : null,
        nbEtages: r.T_NbEtages == null ? match.nbEtages : null,
        dernierEtage: r.T_DernierEtage == null ? match.dernierEtage : null,
        nbChambres: r.T_NbChambres == null ? match.nbChambres : null,
        ascenseur: r.T_Ascenseur == null ? match.ascenseur : null,
        dpe: r.T_Dpe == null ? match.dpe : null,
        photoUrl: r.T_PhotoUrl == null ? match.photoUrl : null,
      };

      const hasDelta = Object.entries(delta).some(([k, v]) => k !== 'id' && v !== null && v !== undefined);
      if (!hasDelta) {
        continue;
      }

      updates.push(delta);
      stats.toUpdate += 1;
      deptStats.updated += 1;
      if (match.matchedUnit) {
        stats.matchedUnit += 1;
        deptStats.unit += 1;
      }
      if (delta.etage != null) {
        stats.withEtage += 1;
        deptStats.etage += 1;
      }
      if (delta.ascenseur != null) {
        stats.withAscenseur += 1;
        deptStats.asc += 1;
        if (delta.ascenseur === 1) stats.withAscenseurYes += 1;
      }
      if (delta.nbEtages != null) stats.withNbEtages += 1;
      if (delta.nbChambres != null) {
        stats.withChambres += 1;
        deptStats.chambres += 1;
      }
      if (delta.dpe != null) stats.withDpe += 1;
      if (delta.photoUrl != null) stats.withPhoto += 1;
    }

    if (opts.apply && updates.length > 0) {
      await applyUpdates(conn, updates, opts.batchSize);
    }
    stats.byDept[dept] = deptStats;
    console.log(`  dept ${dept} : ${deptStats.updated}/${deptStats.total} enrichies (lot=${deptStats.unit}, etage=${deptStats.etage}, asc=${deptStats.asc}, ch=${deptStats.chambres})`);
  }

  console.log(`\nTerminé en ${((Date.now() - t0) / 1000).toFixed(1)}s :`);
  console.log(JSON.stringify(stats, null, 2));
  await conn.end();
}

const currentFile = fileURLToPath(import.meta.url);
if (process.argv[1] && path.resolve(process.argv[1]) === path.resolve(currentFile)) {
  main().catch((err) => {
    console.error('Erreur enrichissement :', err.message);
    process.exit(1);
  });
}
