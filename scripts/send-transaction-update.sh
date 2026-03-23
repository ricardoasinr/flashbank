#!/usr/bin/env bash
# Publica TransactionUpdate vía MassTransit (mismo formato que AccountConsumer).
# Ejecutar desde la raíz del repo: ./scripts/send-transaction-update.sh <TxId> <AccountId> Completed
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"
exec dotnet run --project tools/FlashBank.SimulateTransactionUpdate -- "$@"
