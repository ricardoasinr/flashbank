# Ver mensajes SQS solo con Docker (sin apps ni scripts en el Mac)

Usa el contenedor **LocalStack** que ya tienes: dentro viene **`awslocal`**. Todo con `docker compose` desde la carpeta del repo.

## 1. Listar colas

```bash
docker compose exec localstack awslocal sqs list-queues --region us-east-1
```

Ahí ves los nombres exactos (las colas del `init-aws.sh` suelen ser `transaction-created-queue` y `transaction-processed-queue`; MassTransit puede crear **otras** adicionales).

## 2. Ver mensajes en la cola “transaction-created” (primera)

Un solo `exec` (resuelve la URL y hace `receive-message`):

```bash
docker compose exec localstack sh -c '
  Q=$(awslocal sqs get-queue-url --queue-name transaction-created-queue --region us-east-1 --query QueueUrl --output text)
  awslocal sqs receive-message --region us-east-1 --queue-url "$Q" \
    --max-number-of-messages 10 --wait-time-seconds 5 --attribute-names All
'
```

## 3. Ver mensajes en la cola “transaction-processed” (segunda)

```bash
docker compose exec localstack sh -c '
  Q=$(awslocal sqs get-queue-url --queue-name transaction-processed-queue --region us-east-1 --query QueueUrl --output text)
  awslocal sqs receive-message --region us-east-1 --queue-url "$Q" \
    --max-number-of-messages 10 --wait-time-seconds 5 --attribute-names All
'
```

El JSON que imprime AWS CLI es el mensaje tal cual está en SQS (a veces con envoltorio SNS si MassTransit lo usa).

## Notas

- **`receive-message`** hace que el mensaje sea invisible unos segundos (visibility timeout por defecto); si no lo borras, vuelve a la cola. No estás “perdiendo” el mensaje para siempre salvo que un consumer lo elimine.
- Si la cola sale **vacía**, puede que un servicio .NET ya haya consumido el mensaje. Haz el `POST` y enseguida vuelve a ejecutar el comando, o para temporalmente **Accounts.Worker** / **History** / **Transactions**.
- Si el nombre de la cola es otro (MassTransit), copia el nombre de `list-queues` y sustituye `transaction-created-queue` o `transaction-processed-queue` en los comandos.

## LocalStack “partido” en dos colas

No hace falta otro contenedor: **dos colas dentro del mismo LocalStack**. Los dos bloques de arriba son “una cola” y “la otra”, solo cambia el `--queue-name`.
