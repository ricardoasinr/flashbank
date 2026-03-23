#!/usr/bin/env bash
# Muestra mensajes de una o todas las colas SQS en LocalStack, con JSON legible.
#
# Uso:
#   ./scripts/sqs-show-messages.sh <nombre-cola>
#   ./scripts/sqs-show-messages.sh --all
#
# Requisitos: AWS CLI. Opcional: jq (recomendado para formatear el Body).
#
# Nota: hace receive-message (los mensajes quedan invisible ~10s y luego vuelven
#       a la cola si no se borran). No borra mensajes salvo que uses --delete.
#
# Requiere: jq (brew install jq / apt install jq)
set -euo pipefail

if ! command -v jq &>/dev/null; then
  echo "Instala jq para usar este script: brew install jq   (o apt install jq)" >&2
  exit 1
fi

ENDPOINT="${LOCALSTACK_ENDPOINT:-http://localhost:4566}"
REGION="${AWS_DEFAULT_REGION:-us-east-1}"
VISIBILITY="${SQS_PEEK_VISIBILITY:-10}"
WAIT="${SQS_WAIT_SECONDS:-5}"
MAX="${SQS_MAX_MESSAGES:-10}"
DELETE_AFTER=0

export AWS_ACCESS_KEY_ID="${AWS_ACCESS_KEY_ID:-test}"
export AWS_SECRET_ACCESS_KEY="${AWS_SECRET_ACCESS_KEY:-test}"

pretty() { jq .; }

# Desenve SNS: { "Message": "{ \"message\": ... MassTransit ... }" }
unwrap_body() {
  local raw="$1"
  if ! echo "$raw" | jq -e . &>/dev/null; then
    printf '%s\n' "$raw"
    return
  fi
  echo "$raw" | pretty
  local inner
  inner=$(echo "$raw" | jq -r '.Message // empty')
  if [[ -n "$inner" ]] && [[ "$inner" != "null" ]]; then
    echo ""
    echo "  ↳ .Message (envoltorio SNS, a veces usado por MassTransit):"
    if echo "$inner" | jq -e . &>/dev/null; then
      echo "$inner" | pretty
    else
      printf '%s\n' "$inner"
    fi
  fi
}

print_messages_for_queue_url() {
  local queue_url="$1"
  local label="$2"

  echo ""
  echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
  echo " Cola: $label"
  echo " URL:  $queue_url"
  echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

  local result
  result=$(aws sqs receive-message \
    --queue-url "$queue_url" \
    --endpoint-url "$ENDPOINT" \
    --region "$REGION" \
    --max-number-of-messages "$MAX" \
    --visibility-timeout "$VISIBILITY" \
    --wait-time-seconds "$WAIT" \
    --attribute-names All \
    --message-attribute-names All \
    --output json 2>/dev/null) || result='{"Messages":null}'

  local count
  count=$(echo "$result" | jq '.Messages | length // 0' 2>/dev/null || echo 0)

  if [[ "$count" -eq 0 ]]; then
    echo "(sin mensajes en esta recepción; la cola puede estar vacía o los mensajes están en vuelo / visibility timeout)"
    return
  fi

  echo "$result" | jq -c '.Messages[]' | while IFS= read -r msg; do
    local mid receipt body
    mid=$(echo "$msg" | jq -r '.MessageId // "?"')
    receipt=$(echo "$msg" | jq -r '.ReceiptHandle // ""')
    body=$(echo "$msg" | jq -r '.Body // ""')
    echo ""
    echo "── MessageId: $mid ──"
    unwrap_body "$body"
    if [[ "$DELETE_AFTER" -eq 1 ]] && [[ -n "$receipt" ]]; then
      aws sqs delete-message \
        --queue-url "$queue_url" \
        --receipt-handle "$receipt" \
        --endpoint-url "$ENDPOINT" \
        --region "$REGION" >/dev/null
      echo "(mensaje eliminado de la cola)"
    fi
  done
  echo ""
}

usage() {
  echo "Uso: $0 <nombre-cola> | $0 --all [--delete]" >&2
  echo "  --all      inspeccionar todas las colas de LocalStack" >&2
  echo "  --delete   tras mostrar, borrar cada mensaje (limpieza; ¡ojo en dev!)" >&2
  exit 1
}

ARGS=()
for a in "$@"; do
  case "$a" in
    --delete) DELETE_AFTER=1 ;;
    -h|--help) usage ;;
    *) ARGS+=("$a") ;;
  esac
done
set -- "${ARGS[@]}"

[[ $# -lt 1 ]] && usage

if [[ "${1:-}" == "--all" ]]; then
  echo "LocalStack: $ENDPOINT | región: $REGION"
  urls_json=$(aws sqs list-queues --endpoint-url "$ENDPOINT" --region "$REGION" --output json 2>/dev/null || echo '{"QueueUrls":[]}')
  any=0
  while IFS= read -r queue_url; do
    [[ -z "$queue_url" ]] && continue
    any=1
    label="${queue_url##*/}"
    print_messages_for_queue_url "$queue_url" "$label"
  done < <(echo "$urls_json" | jq -r '.QueueUrls[]? // empty')

  if [[ "$any" -eq 0 ]]; then
    echo "No hay colas (¿LocalStack arriba? ¿SQS habilitado?)."
  fi
  exit 0
fi

QUEUE_NAME="$1"
QUEUE_URL="$(aws sqs get-queue-url \
  --queue-name "$QUEUE_NAME" \
  --endpoint-url "$ENDPOINT" \
  --region "$REGION" \
  --query QueueUrl \
  --output text)"

print_messages_for_queue_url "$QUEUE_URL" "$QUEUE_NAME"
