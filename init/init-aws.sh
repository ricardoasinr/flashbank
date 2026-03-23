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
# transaction-processed-queue: publicada por FlashBank.Accounts.Worker al procesar
# -----------------------------------------------------------------------------
create_queue "transaction-created-queue"
create_queue "transaction-processed-queue"

# -----------------------------------------------------------------------------
# Verificación final
# -----------------------------------------------------------------------------
echo ""
echo "[FlashBank] Colas SQS disponibles:"
awslocal sqs list-queues --region "${REGION}" --output table

echo ""
echo "[FlashBank] Configuración de LocalStack completada."
