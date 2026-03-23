-- =============================================================================
-- FlashBank - PostgreSQL flashbank_accounts (postgres-accounts :5434)
-- Solo tabla accounts (bounded context Cuentas).
-- =============================================================================

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TABLE IF NOT EXISTS accounts (
    id          UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(255)    NOT NULL,
    balance     DECIMAL(18, 2)  NOT NULL DEFAULT 0.00,
    created_at  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_balance_non_negative CHECK (balance >= 0)
);

CREATE INDEX IF NOT EXISTS idx_accounts_name ON accounts (name);

INSERT INTO accounts (name, balance) VALUES
  ('Cuenta Corriente Demo',  15000.50),
  ('Cuenta Ahorro Demo',     8250.00),
  ('Empresa FlashBank S.A.', 100000.00);
