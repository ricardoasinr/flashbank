#!/usr/bin/env bash
# =============================================================================
# Publica un TransactionUpdate con MassTransit (mismo formato que AccountConsumer).
# Requiere: LocalStack en marcha, mismas credenciales que appsettings.
#
# Uso (desde la raíz del repo flashbank/):
#   ./init/send-transaction-update.sh <TransactionId-GUID> <AccountId-GUID> Completed
#   ./init/send-transaction-update.sh <TransactionId-GUID> <AccountId-GUID> Failed "mensaje de error"
# =============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$REPO_ROOT"

exec dotnet run --project tools/FlashBank.SimulateTransactionUpdate -- "$@"
