import test from 'node:test';
import assert from 'node:assert/strict';
import {
  parseAdemeFloor,
  parseCsvLine,
  matchTransactionToAdeme,
} from './enrich-transactions-from-ademe.mjs';

test('parseAdemeFloor extrait les étages français courants depuis complement_adresse_logement', () => {
  assert.equal(parseAdemeFloor('5e ETAGE, porte n° 12', 0), 5);
  assert.equal(parseAdemeFloor('4ème étage à gauche', 0), 4);
  assert.equal(parseAdemeFloor('Etage 4; Porte Gauche', 0), 4);
  assert.equal(parseAdemeFloor('1er etg droite', 0), 1);
  assert.equal(parseAdemeFloor('Bât A - R+3 - Apt 14', 0), 3);
  assert.equal(parseAdemeFloor('Rez-de-chaussée gauche', 0), 0);
  assert.equal(parseAdemeFloor('RDC cour', 0), 0);
  assert.equal(parseAdemeFloor('Rez de jardin', 0), 0);
});

test('parseAdemeFloor ne prend PAS numero_etage_appartement=0 comme RDC sans mention explicite', () => {
  assert.equal(parseAdemeFloor('Batiment 01053001010112', 0), null);
  assert.equal(parseAdemeFloor('Porte 4, Escalier B', 0), null);
  assert.equal(parseAdemeFloor('', 0), null);
  // En revanche, un numero_etage_appartement >= 1 est retenu si complement vide
  assert.equal(parseAdemeFloor('', 3), 3);
  assert.equal(parseAdemeFloor('Porte droite', 6), 6);
});

test('parseCsvLine gère les guillemets, virgules internes et guillemets échappés', () => {
  assert.deepEqual(
    parseCsvLine('"75016","22","Avenue Foch",36.8,0,,"5e ETAGE, porte ""12""","F","2021-10-10"'),
    ['75016', '22', 'Avenue Foch', '36.8', '0', '', '5e ETAGE, porte "12"', 'F', '2021-10-10']
  );
});

test('matchTransactionToAdeme rapproche un lot par surface et fenêtre DPE avant vente', () => {
  const tx = {
    surface: 37.0,
    dateMs: Date.parse('2022-01-15'),
  };
  const records = [
    { surface: 36.8, dateMs: Date.parse('2021-10-10'), etage: 5, nbEtages: 6, dpe: 'F' },
    { surface: 85.0, dateMs: Date.parse('2021-11-01'), etage: 2, nbEtages: 6, dpe: 'D' },
  ];
  const res = matchTransactionToAdeme(tx, records, 6);
  assert.equal(res.matchedUnit, true);
  assert.equal(res.etage, 5);
  assert.equal(res.nbEtages, 6);
  assert.equal(res.dernierEtage, 0);
  assert.equal(res.dpe, 'F');
});

test('matchTransactionToAdeme refuse l étage en cas d ambiguïté entre 2 lots superposés de même surface', () => {
  const tx = {
    surface: 60.0,
    dateMs: Date.parse('2024-06-15'),
  };
  const records = [
    { surface: 60.0, dateMs: Date.parse('2024-04-01'), etage: 2, nbEtages: 6, dpe: 'D' },
    { surface: 60.0, dateMs: Date.parse('2024-04-05'), etage: 4, nbEtages: 6, dpe: 'D' },
  ];
  const res = matchTransactionToAdeme(tx, records, 6);
  assert.equal(res.matchedUnit, true);
  assert.equal(res.etage, null); // ambiguïté 2e vs 4e étage → on ne devine pas
  assert.equal(res.dpe, 'D');
  assert.equal(res.nbEtages, 6);
});
