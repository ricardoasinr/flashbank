#!/usr/bin/env bash
# Lista colas SQS en LocalStack (puerto 4566 por defecto).
# Requiere: AWS CLI instalado (no hace falta awslocal si usas --endpoint-url).

set -euo pipefail

ENDPOINT="${LOCALSTACK_ENDPOINT:-http://localhost:4566}"
REGION="${AWS_DEFAULT_REGION:-us-east-1}"

export AWS_ACCESS_KEY_ID="${AWS_ACCESS_KEY_ID:-test}"
export AWS_SECRET_ACCESS_KEY="${AWS_SECRET_ACCESS_KEY:-test}"

aws sqs list-queues \
  --endpoint-url "$ENDPOINT" \
  --region "$REGION" \
  --output table
