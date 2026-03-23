-- =============================================================================
-- FlashBank - PostgreSQL flashbank_transactions (postgres-transactions :5433)
-- Solo tabla transactions (bounded context Transacciones).
-- =============================================================================

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TABLE IF NOT EXISTS transactions (
    id          UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id  UUID            NOT NULL,
    amount      DECIMAL(18, 2)  NOT NULL,
    type        VARCHAR(50)     NOT NULL,
    status      VARCHAR(20)     NOT NULL DEFAULT 'Pending',
    created_at  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_transaction_amount_positive CHECK (amount > 0),
    CONSTRAINT chk_transaction_type   CHECK (type   IN ('Deposit', 'Withdrawal')),
    CONSTRAINT chk_transaction_status CHECK (status IN ('Pending', 'Completed', 'Failed'))
);

CREATE INDEX IF NOT EXISTS idx_transactions_account_id ON transactions (account_id);
CREATE INDEX IF NOT EXISTS idx_transactions_status     ON transactions (status);
CREATE INDEX IF NOT EXISTS idx_transactions_created_at ON transactions (created_at DESC);

DO $seed$
DECLARE
  acc1 uuid;
  acc2 uuid;
  acc3 uuid;
BEGIN
  acc1 := gen_random_uuid();
  acc2 := gen_random_uuid();
  acc3 := gen_random_uuid();
  INSERT INTO transactions (account_id, amount, type, status, created_at) VALUES
    (acc1, 2500.00,  'Deposit',    'Completed', NOW() - INTERVAL '5 days'),
    (acc1, 150.75,   'Withdrawal', 'Completed', NOW() - INTERVAL '4 days'),
    (acc2, 5000.00,  'Deposit',    'Completed', NOW() - INTERVAL '3 days'),
    (acc2, 120.00,   'Withdrawal', 'Pending',   NOW() - INTERVAL '2 days'),
    (acc3, 10000.00, 'Deposit',    'Completed', NOW() - INTERVAL '1 day'),
    (acc3, 999.99,   'Withdrawal', 'Failed',    NOW() - INTERVAL '6 hours');
END
$seed$;
