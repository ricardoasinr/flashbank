#!/usr/bin/env bash
# Lee SQS en LocalStack y escribe solo el payload del mensaje (stdout).
# Si el Body es notificación SNS, muestra el JSON interno (MassTransit).
#
# Uso:
#   ./scripts/sqs-show-messages.sh <nombre-cola>
#   ./scripts/sqs-show-messages.sh --all
#
# Opciones: --delete  borra cada mensaje tras leerlo.
#
# Requiere: jq, AWS CLI.
# receive-message deja el mensaje invisible unos segundos si no usas --delete.
set -euo pipefail

if ! command -v jq &>/dev/null; then
  echo "Instala jq: brew install jq" >&2
  exit 1
fi

ENDPOINT="${LOCALSTACK_ENDPOINT:-http://localhost:4566}"
REGION="${AWS_DEFAULT_REGION:-us-east-1}"
VISIBILITY="${SQS_VISIBILITY_TIMEOUT:-10}"
WAIT="${SQS_WAIT_SECONDS:-5}"
MAX="${SQS_MAX_MESSAGES:-10}"
DELETE_AFTER=0

export AWS_ACCESS_KEY_ID="${AWS_ACCESS_KEY_ID:-test}"
export AWS_SECRET_ACCESS_KEY="${AWS_SECRET_ACCESS_KEY:-test}"

emit_body_payload() {
  local raw="$1"
  if ! echo "$raw" | jq -e . &>/dev/null; then
    printf '%s\n' "$raw"
    return
  fi
  local inner
  inner=$(echo "$raw" | jq -r '.Message // empty')
  if [[ -n "$inner" ]] && [[ "$inner" != "null" ]]; then
    if echo "$inner" | jq -e . &>/dev/null; then
      echo "$inner" | jq .
    else
      printf '%s\n' "$inner"
    fi
  else
    echo "$raw" | jq .
  fi
}

print_messages_for_queue_url() {
  local queue_url="$1"

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
  count=$(echo "$result" | jq '(.Messages // []) | length' 2>/dev/null || echo 0)

  if [[ "$count" -eq 0 ]]; then
    return
  fi

  local sep=0
  while IFS= read -r msg; do
    [[ -z "$msg" ]] && continue
    local receipt body
    receipt=$(echo "$msg" | jq -r '.ReceiptHandle // ""')
    body=$(echo "$msg" | jq -r '.Body // ""')
    if [[ "$sep" -eq 1 ]]; then
      echo ""
    fi
    sep=1
    emit_body_payload "$body"
    if [[ "$DELETE_AFTER" -eq 1 ]] && [[ -n "$receipt" ]]; then
      aws sqs delete-message \
        --queue-url "$queue_url" \
        --receipt-handle "$receipt" \
        --endpoint-url "$ENDPOINT" \
        --region "$REGION" >/dev/null
    fi
  done < <(echo "$result" | jq -c '.Messages[]? // empty')
}

usage() {
  echo "Uso: $0 <nombre-cola> | $0 --all [--delete]" >&2
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
  urls_json=$(aws sqs list-queues --endpoint-url "$ENDPOINT" --region "$REGION" --output json 2>/dev/null || echo '{"QueueUrls":[]}')
  first_queue=1
  while IFS= read -r queue_url; do
    [[ -z "$queue_url" ]] && continue
    if [[ "$first_queue" -eq 0 ]]; then
      echo ""
    fi
    first_queue=0
    print_messages_for_queue_url "$queue_url"
  done < <(echo "$urls_json" | jq -r '.QueueUrls[]? // empty')
  exit 0
fi

QUEUE_NAME="$1"
QUEUE_URL="$(aws sqs get-queue-url \
  --queue-name "$QUEUE_NAME" \
  --endpoint-url "$ENDPOINT" \
  --region "$REGION" \
  --query QueueUrl \
  --output text)"

print_messages_for_queue_url "$QUEUE_URL"
