#!/usr/bin/env bash
# =============================================================================
# FlashBank - LocalStack Initialization Script
# Evento: ready.d (se ejecuta una vez que LocalStack está completamente listo)
# Crea las colas SQS necesarias para el patrón CQRS/Event-Driven.
# =============================================================================

set -euo pipefail

REGION="${AWS_DEFAULT_REGION:-us-east-1}"
ENDPOINT="http://localhost:4566"

echo "[FlashBank] Iniciando configuración de recursos AWS en LocalStack..."
echo "[FlashBank] Región: ${REGION}"

# -----------------------------------------------------------------------------
# Helper: crear cola SQS si no existe
# -----------------------------------------------------------------------------
create_queue() {
  local queue_name="$1"
  echo "[FlashBank] Creando cola SQS: ${queue_name}"
  awslocal sqs create-queue \
    --queue-name "${queue_name}" \
    --region "${REGION}" \
    --attributes '{
      "MessageRetentionPeriod": "86400",
      "VisibilityTimeout": "30",
      "ReceiveMessageWaitTimeSeconds": "20"
    }' \
    --output json
  echo "[FlashBank] Cola '${queue_name}' creada exitosamente."
}

# -----------------------------------------------------------------------------
# Colas SQS
# transaction-created-queue : publicada por FlashBank.Transactions al crear una Tx
# transaction-update-queue   : TransactionUpdate → TransactionConsumer (MassTransit SetEntityName)
# transaction-processed-queue: publicada por FlashBank.Accounts.Worker al procesar
# -----------------------------------------------------------------------------
create_queue "transaction-created-queue"
create_queue "transaction-update-queue"
create_queue "transaction-processed-queue"

# -----------------------------------------------------------------------------
# Topic SNS + suscripción: mismos nombres que MassTransit (SetEntityName en Program.cs).
# Sin cola real, SNS puede quedar suscrito a una cola inexistente y los mensajes se pierden.
# -----------------------------------------------------------------------------
wire_sns_topic_to_queue() {
  local queue_name="$1"
  local topic_name="$2"

  local queue_url
  queue_url="$(awslocal sqs get-queue-url --queue-name "${queue_name}" --region "${REGION}" --query QueueUrl --output text)"
  local queue_arn
  queue_arn="$(awslocal sqs get-queue-attributes --queue-url "${queue_url}" --region "${REGION}" \
    --attribute-names QueueArn --query 'Attributes.QueueArn' --output text)"
  local topic_arn
  topic_arn="$(awslocal sns create-topic --name "${topic_name}" --region "${REGION}" --query TopicArn --output text)"

  echo "[FlashBank] Topic SNS '${topic_name}' → cola '${queue_name}' (ARN cola: ${queue_arn})"

  local attrs_json
  attrs_json="$(python3 -c "
import json, sys
queue_arn, topic_arn = sys.argv[1], sys.argv[2]
policy = {
    'Version': '2012-10-17',
    'Statement': [{
        'Effect': 'Allow',
        'Principal': {'Service': 'sns.amazonaws.com'},
        'Action': 'sqs:SendMessage',
        'Resource': queue_arn,
        'Condition': {'ArnEquals': {'aws:SourceArn': topic_arn}},
    }],
}
print(json.dumps({'Policy': json.dumps(policy)}))
" "${queue_arn}" "${topic_arn}")"

  awslocal sqs set-queue-attributes \
    --queue-url "${queue_url}" \
    --region "${REGION}" \
    --attributes "${attrs_json}"

  local already
  already="$(awslocal sns list-subscriptions-by-topic --topic-arn "${topic_arn}" --region "${REGION}" --output json \
    | python3 -c "import json,sys; d=json.load(sys.stdin); subs=d.get('Subscriptions')or[]; print('1' if any(s.get('Endpoint')=='${queue_arn}' for s in subs) else '0')" 2>/dev/null || echo 0)"

  if [[ "${already}" == "1" ]]; then
    echo "[FlashBank] Suscripción SNS→SQS ya existía; se omite create-subscription."
  else
    awslocal sns subscribe \
      --topic-arn "${topic_arn}" \
      --protocol sqs \
      --notification-endpoint "${queue_arn}" \
      --region "${REGION}" \
      --output json
    echo "[FlashBank] Suscripción SNS→SQS creada."
  fi
}

wire_sns_topic_to_queue "transaction-created-queue" "transaction-created"
wire_sns_topic_to_queue "transaction-update-queue" "transaction-update"

# -----------------------------------------------------------------------------
# Verificación final
# -----------------------------------------------------------------------------
echo ""
echo "[FlashBank] Colas SQS disponibles:"
awslocal sqs list-queues --region "${REGION}" --output table

echo ""
echo "[FlashBank] Topics SNS:"
awslocal sns list-topics --region "${REGION}" --output table

echo ""
echo "[FlashBank] Configuración de LocalStack completada."
