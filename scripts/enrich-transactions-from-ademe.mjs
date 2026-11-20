#!/usr/bin/env node
/**
 * Enrichissement 2e passe de la table MariaDB `Transactions` à partir de
 * l'open-data ADEME DPE logements existants (`dpe03existant`, depuis juillet 2021).
 *
 * Ne remplace JAMAIS une valeur déjà renseignée (COALESCE) : complète uniquement
 * les colonnes encore NULL (`T_Etage`, `T_NbEtages`, `T_DernierEtage`, `T_Dpe`).
 *
 * Précautions métier :
 *  - Dans ADEME `dpe03existant`, `numero_etage_appartement = 0` est la valeur par défaut
 *    quand l'étage n'est pas saisi : on ne considère `0` (RDC) que s'il est explicitement
 *    mentionné dans `complement_adresse_logement` ("RDC", "rez-de-chaussée", "étage 0"...).
 *  - Pour éviter les confusions d'étage entre appartements superposés de même surface dans
 *    un même immeuble, on exige une fenêtre de date serrée autour de la vente (DPE établi
 *    pour la transaction) et on écarte les ambiguïtés d'étage dans cette fenêtre.
 *
 * Usage :
 *   node scripts/enrich-transactions-from-ademe.mjs --dry-run [--depts=75,92,93,94] [--max-pages=5]
 *   node scripts/enrich-transactions-from-ademe.mjs --apply   [--depts=75,92,93,94]
 */

import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';
import { normalizeHouseNumberKey } from './enrich-transactions-from-property.mjs';
import { normalizeStreet } from './import-dvf-transactions.mjs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const BACKEND_ROOT = path.resolve(__dirname, '..');
const DEFAULT_APPSETTINGS = path.join(BACKEND_ROOT, 'api-perle-rare-decompiled', 'appsettings.Development.json');

const ADEME_BASE_URL = 'https://data.ademe.fr/data-fair/api/v1/datasets/dpe03existant/lines';
const ADEME_SELECT = [
  'code_postal_ban',
  'numero_voie_ban',
  'nom_rue_ban',
  'surface_habitable_logement',
  'numero_etage_appartement',
  'nombre_niveau_immeuble',
  'complement_adresse_logement',
  'etiquette_dpe',
  'date_etablissement_dpe',
].join(',');

const PAGE_SIZE = 10000;
const DAY_MS = 86_400_000;

export function stripAccentsLower(s) {
  return String(s ?? '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase();
}

/**
 * Extrait le numéro d'étage fiable depuis un enregistrement ADEME DPE.
 * Retourne `null` si indéterminé (ne prend PAS `numero_etage_appartement === 0`
 * comme RDC sans mention explicite dans `complement_adresse_logement`).
 */
export function parseAdemeFloor(complementAdresse, numeroEtageAppartement) {
  const txt = stripAccentsLower(complementAdresse).replace(/\s+/g, ' ').trim();

  if (txt) {
    // 1. RDC / rez-de-chaussée / rez-de-jardin / étage 0 explicite
    if (
      /\b(?:r\.?\s*d\.?\s*c\.?|r\.?\s*d\.?\s*j\.?|rez[\s-]*de[\s-]*chaussee|rez[\s-]*de[\s-]*jardin|0\s*(?:er|ere|e|eme|°)?\s*(?:etage|etg)|(?:etage|etg|et\.?)\s*[:n°#\-\s]*0)\b/.test(
        txt
      )
    ) {
      return 0;
    }

    // 2. "4e étage", "4ème étage", "1er etg", "2° etage"
    let m = txt.match(/\b(\d{1,2})\s*(?:er|ere|e|eme|°)\s*(?:etage|etg|et\.?)\b/);
    if (m) {
      const n = parseInt(m[1], 10);
      if (n >= 0 && n <= 45) return n;
    }

    // 3. "étage 4", "etg : 3", "et. 2", "étage n°5"
    m = txt.match(/\b(?:etage|etg|et\.?)\s*(?:n°|n\b|:|#|-|\s)*(\d{1,2})\b/);
    if (m) {
      const n = parseInt(m[1], 10);
      if (n >= 0 && n <= 45) return n;
    }

    // 4. "R+3", "R + 2"
    m = txt.match(/\br\s*\+\s*(\d{1,2})\b/);
    if (m) {
      const n = parseInt(m[1], 10);
      if (n >= 1 && n <= 45) return n;
    }
  }

  // 5. Fallback sur numero_etage_appartement uniquement si >= 1 (0 = valeur par défaut ADEME)
  if (numeroEtageAppartement != null && numeroEtageAppartement !== '') {
    const n = Number(numeroEtageAppartement);
    if (Number.isInteger(n) && n >= 1 && n <= 45) {
      return n;
    }
  }

  return null;
}

/**
 * Parseur CSV RFC 4180 léger (gère guillemets, virgules internes, "" échappés).
 */
export function parseCsvLine(line) {
  const out = [];
  let cur = '';
  let inQuotes = false;
  for (let i = 0; i < line.length; i++) {
    const ch = line[i];
    if (inQuotes) {
      if (ch === '"') {
        if (line[i + 1] === '"') {
          cur += '"';
          i++;
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
    } else if (ch !== '\r') {
      cur += ch;
    }
  }
  out.push(cur);
  return out;
}

export function normalizeDpeLabel(raw) {
  if (!raw) return null;
  const s = String(raw).trim().toUpperCase();
  return /^[A-G]$/.test(s) ? s : null;
}

/**
 * Rapproche une transaction DVF de la liste des DPE ADEME trouvés à la même adresse exacte.
 * Retourne { etage, nbEtages, dernierEtage, dpe, matchedUnit }.
 */
export function matchTransactionToAdeme(tx, ademeRecords, buildingNbEtages = null) {
  const out = {
    etage: null,
    nbEtages: buildingNbEtages,
    dernierEtage: null,
    dpe: null,
    matchedUnit: false,
  };

  if (!ademeRecords || ademeRecords.length === 0) return out;

  const txSurf = tx.surface != null && tx.surface > 0 ? Number(tx.surface) : null;
  if (!txSurf) return out;

  const txDateMs = typeof tx.dateMs === 'number' ? tx.dateMs : Date.parse(tx.dateTransaction);
  if (!Number.isFinite(txDateMs)) return out;

  const candidates = [];
  for (const r of ademeRecords) {
    if (r.surface == null || r.surface <= 0) continue;
    const surfDiff = Math.abs(r.surface - txSurf);
    const surfRel = surfDiff / Math.max(txSurf, 1);
    const maxSurfDiff = Math.max(2.0, txSurf * 0.06);
    if (surfDiff > maxSurfDiff) continue;

    if (!Number.isFinite(r.dateMs)) continue;
    // daysBefore > 0 : DPE établi AVANT la vente (cas normal)
    const daysBefore = (txDateMs - r.dateMs) / DAY_MS;
    if (daysBefore < -180 || daysBefore > 1095) continue;

    let dateScore = 0;
    let inSaleWindow = false;
    if (daysBefore >= 0 && daysBefore <= 270) {
      dateScore = 45;
      inSaleWindow = true;
    } else if (daysBefore > 270 && daysBefore <= 540) {
      dateScore = 30;
      inSaleWindow = true;
    } else if (daysBefore >= -30 && daysBefore < 0) {
      dateScore = 25;
      inSaleWindow = true;
    } else if (daysBefore > 540 && daysBefore <= 1095) {
      dateScore = 12;
    } else {
      // -180..-31 jours après acte : moins fiable (peut être un autre lot superposé)
      dateScore = 5;
    }

    const surfScore = Math.max(0, 45 - surfRel * 650);
    const infoBonus = (r.etage != null ? 5 : 0) + (r.dpe ? 2 : 0);
    const score = surfScore + dateScore + infoBonus;

    candidates.push({ r, score, surfDiff, surfRel, daysBefore, inSaleWindow });
  }

  if (candidates.length === 0) return out;

  candidates.sort((a, b) => b.score - a.score);
  const best = candidates[0];
  if (best.score < 50) return out;

  out.matchedUnit = true;
  out.dpe = best.r.dpe ?? null;

  // Vérification anti-ambiguïté d'étage (ex. 2 appartements superposés de même surface) :
  // On ne retient l'étage que si :
  //   - le meilleur candidat est dans la fenêtre de vente ([-30j, +540j]), OU il est le seul
  //     étage distinct parmi les candidats proches en surface (surfDiff <= 1.5 m²)
  //   - ET il n'y a pas un second candidat d'étage différent avec un score quasi égal (< 8 pts d'écart).
  const withFloor = candidates.filter((c) => c.r.etage != null);
  if (withFloor.length > 0) {
    const topFloor = withFloor[0];
    const conflicting = withFloor.find(
      (c) => c.r.etage !== topFloor.r.etage && topFloor.score - c.score < 8
    );

    const distinctFloors = new Set(withFloor.map((c) => c.r.etage));
    const singleConsensusFloor = distinctFloors.size === 1 && topFloor.surfDiff <= 2.0;

    if (!conflicting && (topFloor.inSaleWindow || singleConsensusFloor)) {
      out.etage = topFloor.r.etage;
    }
  }

  if (best.r.nbEtages != null && out.nbEtages == null) {
    out.nbEtages = best.r.nbEtages;
  }
  if (out.etage != null && out.nbEtages != null && out.nbEtages > 0) {
    out.dernierEtage = out.etage >= out.nbEtages ? 1 : 0;
  }

  return out;
}

function loadMysql2() {
  const req = createRequire(import.meta.url);
  const candidates = [
    path.join(BACKEND_ROOT, 'node_modules', 'mysql2', 'promise.js'),
    path.resolve(BACKEND_ROOT, '..', 'perlerareFrontWeb', 'node_modules', 'mysql2', 'promise.js'),
    path.resolve(BACKEND_ROOT, '..', 'frontend', 'node_modules', 'mysql2', 'promise.js'),
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

function parseConnectionString(cs) {
  const map = {};
  for (const part of String(cs || '').split(';')) {
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

function resolveDbConfig(appsettingsPath) {
  if (process.env.PR_DB_HOST && process.env.PR_DB_USER && process.env.PR_DB_NAME) {
    return {
      host: process.env.PR_DB_HOST,
      port: parseInt(process.env.PR_DB_PORT || '3306', 10),
      database: process.env.PR_DB_NAME,
      user: process.env.PR_DB_USER,
      password: process.env.PR_DB_PASSWORD || '',
    };
  }
  const json = JSON.parse(fs.readFileSync(appsettingsPath, 'utf8'));
  const cs = json?.ConnectionStrings?.PerleRareDB;
  if (!cs) throw new Error(`ConnectionStrings:PerleRareDB introuvable dans ${appsettingsPath}`);
  return parseConnectionString(cs);
}

function parseArgs(argv) {
  const opts = {
    apply: false,
    dryRun: true,
    depts: ['75', '92', '93', '94'],
    appsettings: DEFAULT_APPSETTINGS,
    maxPages: 0,
    minDate: null,
    concurrency: 4,
  };
  for (const a of argv) {
    if (a === '--apply') {
      opts.apply = true;
      opts.dryRun = false;
    } else if (a === '--dry-run') {
      opts.apply = false;
      opts.dryRun = true;
    } else if (a.startsWith('--depts=')) {
      opts.depts = a.slice(8).split(',').map((s) => s.trim()).filter(Boolean);
    } else if (a.startsWith('--appsettings=')) {
      opts.appsettings = a.slice(14);
    } else if (a.startsWith('--max-pages=')) {
      opts.maxPages = parseInt(a.slice(12), 10) || 0;
    } else if (a.startsWith('--min-date=')) {
      opts.minDate = a.slice(11).trim() || null;
    } else if (a.startsWith('--concurrency=')) {
      opts.concurrency = Math.max(1, Math.min(4, parseInt(a.slice(14), 10) || 4));
    }
  }
  return opts;
}

async function fetchTextWithRetry(url, maxRetries = 8) {
  let attempt = 0;
  while (true) {
    try {
      const res = await fetch(url, {
        headers: { 'User-Agent': 'PerleRareCRM-EnrichDPE/1.0' },
      });
      if (res.status === 429 || res.status >= 500) {
        const retryAfter = parseInt(res.headers.get('retry-after') || '', 10);
        const err = new Error(`HTTP ${res.status}`);
        err.retryAfterSec = Number.isFinite(retryAfter) && retryAfter > 0 ? retryAfter : null;
        throw err;
      }
      if (!res.ok) {
        const body = await res.text().catch(() => '');
        throw new Error(`HTTP ${res.status}: ${body.slice(0, 200)}`);
      }
      const link = res.headers.get('link') || '';
      const nextMatch = link.match(/<([^>]+)>;\s*rel="?next"?/i);
      const nextUrl = nextMatch ? nextMatch[1] : null;
      const text = await res.text();
      return { text, nextUrl };
    } catch (err) {
      attempt++;
      if (attempt > maxRetries) throw err;
      const delayMs = err.retryAfterSec
        ? (err.retryAfterSec + 1) * 1000
        : Math.min(60_000, 2000 * Math.pow(2, attempt - 1));
      console.log(`  [ADEME retry ${attempt}/${maxRetries}] ${err.message} — attente ${(delayMs / 1000).toFixed(0)}s`);
      await new Promise((r) => setTimeout(r, delayMs));
    }
  }
}

async function streamAdemeDept(dept, neededAddresses, ademeByAddr, nbEtagesCountsByAddr, maxPages, minDpeDate = null) {
  const dpeDateClause = minDpeDate ? ` AND date_etablissement_dpe:[${minDpeDate} TO *]` : '';
  const qs = encodeURIComponent(`code_departement_ban:${dept} AND type_batiment:appartement${dpeDateClause}`);
  let url = `${ADEME_BASE_URL}?format=csv&size=${PAGE_SIZE}&qs=${qs}&select=${encodeURIComponent(ADEME_SELECT)}`;
  let pages = 0;
  let rowsRead = 0;
  let rowsKept = 0;

  while (url) {
    pages++;
    const { text, nextUrl } = await fetchTextWithRetry(url);
    const lines = text.split('\n');
    if (lines.length <= 1) break;

    const header = parseCsvLine(lines[0]);
    const idx = {};
    for (let i = 0; i < header.length; i++) idx[header[i].trim()] = i;

    const iCp = idx.code_postal_ban;
    const iNum = idx.numero_voie_ban;
    const iRue = idx.nom_rue_ban;
    const iSurf = idx.surface_habitable_logement;
    const iNumEtage = idx.numero_etage_appartement;
    const iNbNiv = idx.nombre_niveau_immeuble;
    const iComp = idx.complement_adresse_logement;
    const iDpe = idx.etiquette_dpe;
    const iDate = idx.date_etablissement_dpe;

    for (let li = 1; li < lines.length; li++) {
      const raw = lines[li];
      if (!raw || raw.length < 5) continue;
      rowsRead++;
      const cols = parseCsvLine(raw);

      const cp = (cols[iCp] || '').trim();
      if (!cp) continue;
      const rueNorm = normalizeStreet(cols[iRue]);
      if (!rueNorm) continue;
      const numKey = normalizeHouseNumberKey(cols[iNum]);
      if (!numKey) continue;

      const addrKey = `${cp}|${rueNorm}|${numKey}`;
      if (!neededAddresses.has(addrKey)) continue;

      const nbNivRaw = cols[iNbNiv] ? parseInt(cols[iNbNiv], 10) : NaN;
      const nbEtages = Number.isInteger(nbNivRaw) && nbNivRaw >= 1 && nbNivRaw <= 50 ? nbNivRaw : null;
      if (nbEtages != null) {
        let cmap = nbEtagesCountsByAddr.get(addrKey);
        if (!cmap) {
          cmap = new Map();
          nbEtagesCountsByAddr.set(addrKey, cmap);
        }
        cmap.set(nbEtages, (cmap.get(nbEtages) || 0) + 1);
      }

      const etage = parseAdemeFloor(cols[iComp], cols[iNumEtage]);
      const dpe = normalizeDpeLabel(cols[iDpe]);
      if (etage == null && dpe == null) continue;

      const surf = cols[iSurf] ? Number(cols[iSurf]) : NaN;
      const dateMs = cols[iDate] ? Date.parse(cols[iDate]) : NaN;
      if (!Number.isFinite(surf) || surf <= 5 || !Number.isFinite(dateMs)) continue;

      let list = ademeByAddr.get(addrKey);
      if (!list) {
        list = [];
        ademeByAddr.set(addrKey, list);
      }
      list.push({
        surface: surf,
        dateMs,
        etage,
        nbEtages,
        dpe,
      });
      rowsKept++;
    }

    if (pages % 20 === 0) {
      console.log(`  [ADEME dept ${dept}] page ${pages} — ${rowsRead} lues, ${rowsKept} utiles`);
    }
    if (maxPages > 0 && pages >= maxPages) break;
    url = nextUrl;
    if (url) await new Promise((r) => setTimeout(r, 150));
  }

  return { dept, pages, rowsRead, rowsKept };
}

function pickMode(countMap) {
  if (!countMap || countMap.size === 0) return null;
  let bestVal = null;
  let bestCnt = 0;
  for (const [val, cnt] of countMap.entries()) {
    if (cnt > bestCnt || (cnt === bestCnt && val > bestVal)) {
      bestVal = val;
      bestCnt = cnt;
    }
  }
  return bestVal;
}

async function flushUpdates(conn, updates) {
  if (updates.length === 0) return;
  const BATCH = 1000;
  for (let i = 0; i < updates.length; i += BATCH) {
    const chunk = updates.slice(i, i + BATCH);
    const ids = chunk.map((u) => u.id);

    const caseExpr = (col, getter) => {
      const parts = [];
      const vals = [];
      for (const u of chunk) {
        const v = getter(u);
        if (v !== null && v !== undefined) {
          parts.push('WHEN ? THEN ?');
          vals.push(u.id, v);
        }
      }
      if (parts.length === 0) return null;
      return { sql: `${col} = COALESCE(${col}, CASE T_Ref ${parts.join(' ')} END)`, vals };
    };

    const specs = [
      caseExpr('T_Etage', (u) => u.etage),
      caseExpr('T_NbEtages', (u) => u.nbEtages),
      caseExpr('T_DernierEtage', (u) => u.dernierEtage),
      caseExpr('T_Dpe', (u) => u.dpe),
    ].filter(Boolean);

    if (specs.length === 0) continue;
    const setSql = specs.map((s) => s.sql).join(', ');
    const params = specs.flatMap((s) => s.vals).concat(ids);
    const inPlaceholders = ids.map(() => '?').join(',');

    await conn.query(
      `UPDATE Transactions SET ${setSql}, T_DateMaj = NOW() WHERE T_Ref IN (${inPlaceholders})`,
      params
    );
  }
}

async function main() {
  const opts = parseArgs(process.argv.slice(2));
  const t0 = Date.now();
  console.log(
    `Enrichissement 2e passe Transactions <- ADEME DPE : depts=[${opts.depts.join(',')}] mode=${
      opts.apply ? 'APPLY' : 'DRY-RUN'
    }`
  );

  const mysql = loadMysql2();
  const dbCfg = resolveDbConfig(opts.appsettings);
  const conn = await mysql.createConnection(dbCfg);

  try {
    // 1. Charger les transactions Appartement qui ont encore T_Etage / T_NbEtages / T_Dpe NULL
    const deptPlaceholders = opts.depts.map(() => '?').join(',');
    const dateFilterSql = opts.minDate ? ' AND T_DateTransaction >= ?' : '';
    const queryParams = opts.minDate ? [...opts.depts, opts.minDate] : [...opts.depts];
    const [txRows] = await conn.query(
      `SELECT T_Ref, T_CodeDepartement, T_CP, T_AdresseRueNorm, T_AdresseNum, T_AdresseSuffixe, T_DateTransaction,
              T_SurfaceCarrez, T_SurfaceBatie,
              T_Etage, T_NbEtages, T_DernierEtage, T_Dpe
       FROM Transactions
       WHERE T_CodeDepartement IN (${deptPlaceholders})
         AND T_TypeBien = 'Appartement'
         AND T_CP IS NOT NULL AND T_AdresseRueNorm IS NOT NULL AND T_AdresseNum IS NOT NULL
         AND (T_Etage IS NULL OR T_NbEtages IS NULL OR T_Dpe IS NULL)${dateFilterSql}`,
      queryParams
    );

    const neededAddresses = new Set();
    const txs = [];
    for (const r of txRows) {
      const rawNum = r.T_AdresseSuffixe ? `${r.T_AdresseNum} ${r.T_AdresseSuffixe}` : r.T_AdresseNum;
      const numKey = normalizeHouseNumberKey(rawNum);
      if (!numKey) continue;
      const addrKey = `${r.T_CP}|${r.T_AdresseRueNorm}|${numKey}`;
      neededAddresses.add(addrKey);
      const surf = r.T_SurfaceCarrez != null ? Number(r.T_SurfaceCarrez) : (r.T_SurfaceBatie != null ? Number(r.T_SurfaceBatie) : null);
      const dateMs = r.T_DateTransaction instanceof Date ? r.T_DateTransaction.getTime() : Date.parse(r.T_DateTransaction);
      txs.push({
        id: r.T_Ref,
        dept: r.T_CodeDepartement,
        addrKey,
        surface: surf,
        dateMs,
        curEtage: r.T_Etage,
        curNbEtages: r.T_NbEtages,
        curDernierEtage: r.T_DernierEtage,
        curDpe: r.T_Dpe,
      });
    }

    console.log(
      `  Transactions cibles (Appartement avec au moins 1 champ NULL) : ${txs.length} sur ${neededAddresses.size} adresses (${(
        (Date.now() - t0) / 1000
      ).toFixed(1)}s)`
    );

    // 2. Traiter département par département (évite les 429 ADEME et applique au fil de l'eau)
    const stats = {
      scanned: txs.length,
      toUpdate: 0,
      newEtage: 0,
      newNbEtages: 0,
      newDernierEtage: 0,
      newDpe: 0,
      byDept: {},
    };

    for (const dept of opts.depts) {
      const deptTxs = txs.filter((t) => t.dept === dept);
      if (deptTxs.length === 0) continue;
      const deptAddresses = new Set(deptTxs.map((t) => t.addrKey));
      const ademeByAddr = new Map();
      const nbEtagesCountsByAddr = new Map();

      const minDpeDate = opts.minDate
        ? new Date(Date.parse(opts.minDate) - 730 * DAY_MS).toISOString().slice(0, 10)
        : null;

      const dr = await streamAdemeDept(
        dept,
        deptAddresses,
        ademeByAddr,
        nbEtagesCountsByAddr,
        opts.maxPages,
        minDpeDate
      );
      console.log(
        `  ADEME dept ${dr.dept} terminé : ${dr.pages} pages, ${dr.rowsRead} lignes lues, ${dr.rowsKept} retenues`
      );

      const updates = [];
      stats.byDept[dept] = { updated: 0, etage: 0, nbEtages: 0, dpe: 0 };

      for (const tx of deptTxs) {
        const ademeRecords = ademeByAddr.get(tx.addrKey) || null;
        const buildingNbEtages = pickMode(nbEtagesCountsByAddr.get(tx.addrKey));
        if (!ademeRecords && buildingNbEtages == null) continue;

        const m = matchTransactionToAdeme(tx, ademeRecords, buildingNbEtages);

        const u = {
          id: tx.id,
          etage: tx.curEtage == null ? m.etage : null,
          nbEtages: tx.curNbEtages == null ? m.nbEtages : null,
          dernierEtage: tx.curDernierEtage == null ? m.dernierEtage : null,
          dpe: tx.curDpe == null ? m.dpe : null,
        };

        if (u.etage != null && tx.curDernierEtage == null && u.dernierEtage == null) {
          const effNbEtages = tx.curNbEtages ?? u.nbEtages;
          if (effNbEtages != null && effNbEtages > 0) {
            u.dernierEtage = u.etage >= effNbEtages ? 1 : 0;
          }
        }

        if (u.etage == null && u.nbEtages == null && u.dernierEtage == null && u.dpe == null) {
          continue;
        }

        updates.push(u);
        stats.toUpdate++;
        if (u.etage != null) stats.newEtage++;
        if (u.nbEtages != null) stats.newNbEtages++;
        if (u.dernierEtage != null) stats.newDernierEtage++;
        if (u.dpe != null) stats.newDpe++;

        stats.byDept[dept].updated++;
        if (u.etage != null) stats.byDept[dept].etage++;
        if (u.nbEtages != null) stats.byDept[dept].nbEtages++;
        if (u.dpe != null) stats.byDept[dept].dpe++;
      }

      if (opts.apply && updates.length > 0) {
        console.log(`  [dept ${dept}] Application de ${updates.length} mises à jour en base...`);
        await flushUpdates(conn, updates);
      }
    }

    console.log(`\nTerminé en ${((Date.now() - t0) / 1000).toFixed(1)}s :`);
    console.log(JSON.stringify(stats, null, 2));
  } finally {
    await conn.end();
  }
}

if (process.argv[1] && path.resolve(process.argv[1]) === __filename) {
  main().catch((err) => {
    console.error('ERREUR enrich-transactions-from-ademe:', err);
    process.exit(1);
  });
}
