-- Soft-hash passwords: CP_MotDePasse must hold PBKDF2 strings (~90 chars).
-- Safe: VARCHAR widen is non-destructive; existing plaintext (≤15) still fits.
-- Applied on MariaDB `perle-rareinfo` (prod DB) 2026-09-14 : varchar(15) → varchar(255).
-- Soft-hash writes still need a writable API user (demo = cursor_client SELECT-only).

ALTER TABLE conseillers_personnels
  MODIFY COLUMN CP_MotDePasse VARCHAR(255) NULL;
