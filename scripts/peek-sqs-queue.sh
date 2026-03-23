#!/usr/bin/env bash
# Lee mensajes de una cola (nombre corto). Uso: ./scripts/peek-sqs-queue.sh <nombre-cola>
set -euo pipefail
QUEUE_NAME="${1:?Nombre de cola requerido}"
ENDPOINT="${LOCALSTACK_ENDPOINT:-http://localhost:4566}"
REGION="${AWS_DEFAULT_REGION:-us-east-1}"
export AWS_ACCESS_KEY_ID="${AWS_ACCESS_KEY_ID:-test}"
export AWS_SECRET_ACCESS_KEY="${AWS_SECRET_ACCESS_KEY:-test}"
QUEUE_URL="$(aws sqs get-queue-url --queue-name "$QUEUE_NAME" --endpoint-url "$ENDPOINT" --region "$REGION" --query QueueUrl --output text)"
echo "QueueUrl: $QUEUE_URL"
echo ""
aws sqs receive-message \
  --queue-url "$QUEUE_URL" \
  --endpoint-url "$ENDPOINT" \
  --region "$REGION" \
  --max-number-of-messages 5 \
  --visibility-timeout 5 \
  --wait-time-seconds 2 \
  --attribute-names All \
  --message-attribute-names All \
  --output json
