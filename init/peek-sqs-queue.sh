#!/usr/bin/env bash
# Lee hasta 5 mensajes de una cola (sin esperar mucho). Útil para ver el cuerpo en consola.
# Nota: en SQS un receive-message sigue siendo un "receive"; con visibility-timeout bajo
#       el mensaje vuelve a estar disponible rápido para el consumer real.
#
# Uso:
#   ./init/peek-sqs-queue.sh transaction-created-queue
#   ./init/peek-sqs-queue.sh nombre-exacto-de-cola-masstransit
#
# Para ver el nombre real que creó MassTransit tras arrancar los servicios:
#   ./init/list-sqs-queues.sh

set -euo pipefail

QUEUE_NAME="${1:?Nombre de cola requerido}"

ENDPOINT="${LOCALSTACK_ENDPOINT:-http://localhost:4566}"
REGION="${AWS_DEFAULT_REGION:-us-east-1}"

export AWS_ACCESS_KEY_ID="${AWS_ACCESS_KEY_ID:-test}"
export AWS_SECRET_ACCESS_KEY="${AWS_SECRET_ACCESS_KEY:-test}"

QUEUE_URL="$(aws sqs get-queue-url \
  --queue-name "$QUEUE_NAME" \
  --endpoint-url "$ENDPOINT" \
  --region "$REGION" \
  --query QueueUrl \
  --output text)"

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
