# Ver mensajes SQS con Docker (LocalStack)

Las colas viven en **LocalStack**. Para inspeccionarlas puedes usar **`awslocal`** dentro del contenedor o el script en el host **`scripts/sqs-show-messages.sh`** (recomendado: formatea el JSON y desenve SNS → MassTransit).

## Desde tu máquina (recomendado)

Con LocalStack en `http://localhost:4566`:

```bash
export LOCALSTACK_ENDPOINT=http://localhost:4566
export AWS_ACCESS_KEY_ID=test AWS_SECRET_ACCESS_KEY=test AWS_DEFAULT_REGION=us-east-1
./scripts/sqs-show-messages.sh transaction-created-queue
./scripts/sqs-show-messages.sh transaction-processed-queue
./scripts/sqs-show-messages.sh --all
```

Opciones útiles: `--delete` (borra tras leer), variables `SQS_WAIT_SECONDS`, `SQS_VISIBILITY_TIMEOUT`, `SQS_MAX_MESSAGES`. Detalle en `scripts/README.md`.

---

## Comandos manuales dentro de LocalStack (`awslocal`)

### Listar colas

```bash
docker compose exec localstack awslocal sqs list-queues --region us-east-1
```

### `receive-message` (JSON crudo de AWS)

```bash
docker compose exec localstack sh -c '
  Q=$(awslocal sqs get-queue-url --queue-name transaction-created-queue --region us-east-1 --query QueueUrl --output text)
  awslocal sqs receive-message --region us-east-1 --queue-url "$Q" \
    --max-number-of-messages 10 --wait-time-seconds 5 --attribute-names All
'
```

Sustituye el nombre de cola si MassTransit creó otras (copia el nombre de `list-queues`).

---

## Notas

- **`receive-message`** oculta el mensaje un tiempo (visibility timeout); si no haces `delete-message`, vuelve a la cola.
- Si la cola sale vacía, otro servicio puede haber consumido el mensaje; ejecuta el flujo (p. ej. POST) y vuelve a intentar, o para temporalmente los workers.
