#!/usr/bin/env bash
# Alias de sqs-show-messages.sh (compatibilidad).
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec "$SCRIPT_DIR/sqs-show-messages.sh" "$@"
