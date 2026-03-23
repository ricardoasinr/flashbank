# Pruebas del flujo EDA (curl, LocalStack, PostgreSQL)

Los scripts viven en [`scripts/`](../../scripts/README.md). La carpeta [`init/`](../../init/) es **solo** para arranque de infraestructura (SQL, seeds, hook de LocalStack en Docker).

---

## Qué hace tu `curl` a `POST /transaction`

Con el cuerpo:

```json
{ "accountId": "645f0886-982a-4ca4-adba-ced32e8b696b", "amount": 100, "type": 1 }
```

(`type: 1` = **Withdrawal**)

**Si todo está bien** (API en marcha, Postgres en `5433`, LocalStack en `4566`):

1. La API valida `amount > 0` y `accountId` no vacío.
2. **INSERT** en `transactions` con `status = Pending`, `amount = 100`, `type = Withdrawal`.
3. **`IBus.Publish(TransactionCreated)`** hacia la topología SQS/SNS de MassTransit.
4. Respuesta **201 Created** con el `id` de la transacción y `status: Pending`.

Eso es el “primer ciclo”: persistencia local + evento publicado.

**Si algo falla, suele ser:**

| Síntoma | Causa típica |
|---------|----------------|
| `Connection refused` | No está corriendo `FlashBank.Transactions` o el puerto no es `5136`. |
| `500` al guardar | Postgres no levantado o connection string incorrecta (`localhost:5433`). |
| `500` después del INSERT | Fallo al publicar en LocalStack (SQS/SNS): LocalStack caído o puerto `4566` distinto. |
| `400` | `amount <= 0` o `accountId` vacío (`00000000-...`). |

Comando equivalente al que probaste:

```bash
curl --location 'http://localhost:5136/transaction' \
  --header 'Content-Type: application/json' \
  --data '{
    "accountId": "645f0886-982a-4ca4-adba-ced32e8b696b",
    "amount": 100,
    "type": 1
  }'
```

---

## Prerrequisitos

1. **Docker Compose**: `postgres-transactions` y `localstack` (y opcionalmente el resto).
   ```bash
   docker compose up -d postgres-transactions localstack
   ```
2. **Connection string** en `FlashBank.Transactions/appsettings.Development.json`.
3. **AWS CLI** (opcional) para scripts de colas: `AWS_ACCESS_KEY_ID=test`, `AWS_SECRET_ACCESS_KEY=test`.

---

## 1) POST (Postman o curl)

| URL HTTP | `POST http://localhost:5136/transaction` |
|----------|-------------------------------------------|
| Alternativa | `POST http://localhost:5136/transactions` |

Enums en JSON como **número**: `0` = Deposit, `1` = Withdrawal.

---

## 2) Ver colas y mensajes (terminal)

```bash
./scripts/list-sqs-queues.sh
./scripts/peek-sqs-queue.sh <nombre-de-cola>
```

MassTransit puede crear **nombres distintos** a `transaction-created-queue` del `init-aws.sh`. Lista las colas **después** de arrancar los servicios .NET.

---

## 3) Simular `TransactionUpdate` (segundo paso)

No envíes JSON crudo a SQS; usa MassTransit:

```bash
./scripts/send-transaction-update.sh "<TransactionId-del-201>" "645f0886-982a-4ca4-adba-ced32e8b696b" Completed
```

Para fallo:

```bash
./scripts/send-transaction-update.sh "<TxId>" "<AccountId>" Failed "mensaje"
```

Con **FlashBank.Transactions** en marcha, `TransactionConsumer` actualizará el `status` en PostgreSQL.

---

## 4) Verificar en SQL

```sql
SELECT id, account_id, amount, type, status, created_at
FROM transactions
ORDER BY created_at DESC
LIMIT 5;
```

---

## Resumen

```text
POST /transaction → INSERT Pending → Publish TransactionCreated → SQS/SNS
./scripts/send-transaction-update.sh → Publish TransactionUpdate → Consumer → UPDATE status
```
