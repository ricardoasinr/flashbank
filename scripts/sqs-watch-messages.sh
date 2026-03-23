#!/usr/bin/env bash
# Muestra mensajes de una cola en bucle (long polling vía sqs-show-messages). Ctrl+C para salir.
#
# Uso: ./scripts/sqs-watch-messages.sh <nombre-cola>
set -euo pipefail

QUEUE_NAME="${1:?Uso: $0 <nombre-cola>}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

export SQS_WAIT_SECONDS="${SQS_WAIT_SECONDS:-20}"
export SQS_MAX_MESSAGES="${SQS_MAX_MESSAGES:-10}"

echo "Observando cola: $QUEUE_NAME (SQS_WAIT_SECONDS=$SQS_WAIT_SECONDS) — Ctrl+C para salir"
echo ""

while true; do
  "$SCRIPT_DIR/sqs-show-messages.sh" "$QUEUE_NAME" || true
  sleep 1
done
