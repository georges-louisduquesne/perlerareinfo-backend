import test from 'node:test';
import assert from 'node:assert/strict';
import {
  extractFirstPhotoUrl,
  inferBuildingInfo,
  matchTransactionToProperty,
  normalizeHouseNumberKey,
} from './enrich-transactions-from-property.mjs';

test('normalizeHouseNumberKey harmonise bis/ter/espaces', () => {
  assert.equal(normalizeHouseNumberKey('12 bis'), '12B');
  assert.equal(normalizeHouseNumberKey('12bis'), '12B');
  assert.equal(normalizeHouseNumberKey('12 B'), '12B');
  assert.equal(normalizeHouseNumberKey('45'), '45');
});

test('extractFirstPhotoUrl extrait la première URL JSON valide', () => {
  assert.equal(
    extractFirstPhotoUrl('["https://pix.yanport.com/a.jpg","https://pix.yanport.com/b.jpg"]'),
    'https://pix.yanport.com/a.jpg',
  );
  assert.equal(extractFirstPhotoUrl(''), null);
});

test('inferBuildingInfo déduit ascenseur=1 dès qu’une annonce Appartement de l’immeuble a P_Asc=1', () => {
  const info = inferBuildingInfo([
    { typeNorm: 'Appartement', asc: 0, nbEtages: 6 },
    { typeNorm: 'Appartement', asc: 1, nbEtages: 6 },
    { typeNorm: 'Appartement', asc: 0, nbEtages: 7 },
  ]);
  assert.deepEqual(info, { ascenseur: 1, nbEtages: 6 });

  const noAsc = inferBuildingInfo([
    { typeNorm: 'Appartement', asc: 0, nbEtages: 4 },
    { typeNorm: 'Appartement', asc: 0, nbEtages: 4 },
  ]);
  assert.deepEqual(noAsc, { ascenseur: 0, nbEtages: 4 });
});

test('matchTransactionToProperty rapproche un lot par surface/prix/date et complète l’étage depuis un doublon', () => {
  const tx = {
    dateTransaction: '2025-10-06',
    typeBien: 'Appartement',
    nbPieces: 2,
    prix: 450000,
    surfaceCarrez: 34.92,
    surfaceBatie: 38,
  };
  const props = [
    {
      propertyId: 'prop-exact-no-floor',
      typeNorm: 'Appartement',
      nbPieces: 2,
      nbChambres: 1,
      etage: null,
      nbEtages: 9,
      dernierEtage: 0,
      asc: 1,
      surface: 34.92,
      prix: 490000,
      dateDebutMs: Date.parse('2025-02-14'),
      dateFinMs: Date.parse('2025-07-30'),
      dpe: 'E',
      photoUrl: 'https://pix.yanport.com/p1.jpg',
    },
    {
      propertyId: 'prop-dup-with-floor',
      typeNorm: 'Appartement',
      nbPieces: 2,
      nbChambres: 1,
      etage: 3,
      nbEtages: 9,
      dernierEtage: 0,
      asc: 1,
      surface: 35.0,
      prix: 495000,
      dateDebutMs: Date.parse('2025-02-20'),
      dateFinMs: Date.parse('2025-07-25'),
      dpe: 'E',
      photoUrl: null,
    },
  ];
  const res = matchTransactionToProperty(tx, props);
  assert.equal(res.matchedUnit, true);
  assert.equal(res.etage, 3);
  assert.equal(res.nbEtages, 9);
  assert.equal(res.nbChambres, 1);
  assert.equal(res.ascenseur, 1);
  assert.equal(res.dpe, 'E');
  assert.equal(res.photoUrl, 'https://pix.yanport.com/p1.jpg');
});

test('matchTransactionToProperty écarte les faux positifs de prix mais conserve l’ascenseur immeuble', () => {
  const tx = {
    dateTransaction: '2025-09-29',
    typeBien: 'Appartement',
    nbPieces: 3,
    prix: 889000,
    surfaceBatie: 99,
  };
  const props = [
    {
      propertyId: 'too-expensive',
      typeNorm: 'Appartement',
      nbPieces: 4,
      nbChambres: 2,
      etage: 2,
      nbEtages: 10,
      asc: 1,
      surface: 101,
      prix: 1650000, // ratio 1.85 > 1.40 -> rejeté au niveau lot
      dateDebutMs: Date.parse('2025-02-01'),
      dateFinMs: Date.parse('2025-07-01'),
    },
  ];
  const res = matchTransactionToProperty(tx, props);
  assert.equal(res.matchedUnit, false);
  assert.equal(res.propertyId, null);
  assert.equal(res.etage, null);
  assert.equal(res.ascenseur, 1);
  assert.equal(res.nbEtages, 10);
});

test('matchTransactionToProperty accepte un écart de pièces DVF/annonce quand la surface est quasi identique', () => {
  const tx = {
    dateTransaction: '2025-11-30',
    typeBien: 'Appartement',
    nbPieces: 11,
    prix: 8200000,
    surfaceCarrez: 404.69,
  };
  const props = [
    {
      propertyId: 'foch-26',
      typeNorm: 'Appartement',
      nbPieces: 7,
      nbChambres: 5,
      etage: 3,
      nbEtages: 6,
      asc: 1,
      surface: 404,
      prix: 10400000,
      dateDebutMs: Date.parse('2025-06-10'),
      dateFinMs: Date.parse('2025-06-19'),
      dpe: 'D',
    },
  ];
  const res = matchTransactionToProperty(tx, props);
  assert.equal(res.matchedUnit, true);
  assert.equal(res.etage, 3);
  assert.equal(res.nbChambres, 5);
  assert.equal(res.ascenseur, 1);
});
