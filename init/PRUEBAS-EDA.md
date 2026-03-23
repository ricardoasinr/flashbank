# Pruebas del flujo EDA (Postman + LocalStack + PostgreSQL)

## Prerrequisitos

1. **Docker Compose** levantado (Postgres transacciones, LocalStack):
   ```bash
   docker compose up -d postgres-transactions localstack
   ```
2. **PostgreSQL transacciones** accesible en `localhost:5433` (connection string en `FlashBank.Transactions/appsettings.Development.json`).
3. **LocalStack** en `http://localhost:4566`.
4. **AWS CLI** (opcional pero recomendado) para listar/peek colas: variables `AWS_ACCESS_KEY_ID=test`, `AWS_SECRET_ACCESS_KEY=test`.

---

## 1) POST desde Postman (primer ciclo)

### Endpoint

| Modo   | URL |
|--------|-----|
| HTTP   | `POST http://localhost:5136/transaction` |
| HTTPS  | `POST https://localhost:7058/transaction` |

También válido: `POST .../transactions` (mismo comportamiento).

> El puerto **5136** sale de `FlashBank.Transactions/Properties/launchSettings.json` (perfil `http`).

### Headers

- `Content-Type: application/json`

### Body (JSON)

Los enums van como **número** (serialización por defecto de System.Text.Json):

| `type` | Valor |
|--------|--------|
| Deposit    | `0` |
| Withdrawal | `1` |

Ejemplo:

```json
{
  "accountId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "amount": 100,
  "type": 1
}
```

### Respuesta esperada

- **201 Created** con `id`, `status: "Pending"`, etc.
- En consola del servicio **Transactions** deberías ver logs de creación y publicación del evento.

### “Transacción enviada”

En el diagrama, eso es la respuesta **201** al cliente: la transacción ya está en BD como `Pending` y el evento `TransactionCreated` se ha publicado. El mensaje en SQS lo procesan **Accounts.Worker** e **History** en paralelo (si están corriendo).

---

## 2) Ver mensajes en la cola (visualmente)

LocalStack **community** no trae una UI tipo “explorador de colas”. Opciones:

### A) Terminal — listar colas

```bash
./init/list-sqs-queues.sh
```

### B) Terminal — leer mensajes de una cola por nombre

```bash
./init/peek-sqs-queue.sh <nombre-de-cola>
```

**Importante:** MassTransit crea colas automáticamente con **nombres distintos** a los del script `init-aws.sh` (`transaction-created-queue`, `transaction-processed-queue`), salvo que en el futuro configures endpoints explícitos. Por eso:

1. Arranca **Transactions** (y si quieres el flujo completo, **Accounts.Worker** e **History**).
2. Ejecuta `./init/list-sqs-queues.sh` y busca colas relacionadas con `transaction`, `created`, `consumer`, etc.

Si solo existe `transaction-created-queue` y está vacía pero ves tráfico en otra cola, el bus está usando esa otra cola (topología MassTransit + SNS/SQS).

---

## 3) Simular el “segundo SQS” — `TransactionUpdate`

No uses un JSON a mano en SQS: el envelope de MassTransit es fácil de equivocar. Usa la herramienta que publica igual que el código:

### Desde la raíz del repo

```bash
chmod +x init/send-transaction-update.sh
./init/send-transaction-update.sh <TransactionId> <AccountId> Completed
```

Ejemplo con los GUID que te devolvió el POST:

```bash
./init/send-transaction-update.sh \
  "3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  "a1b2c3d4-e5f6-7890-abcd-ef1234567890" \
  Completed
```

Para forzar fallo:

```bash
./init/send-transaction-update.sh "<TxId>" "<AccountId>" Failed "Saldo insuficiente"
```

### Requisito

Debe estar corriendo **solo** el publicador no necesita el consumer; pero para **ver el estado actualizado en PostgreSQL**, el servicio **FlashBank.Transactions** debe estar en marcha con **TransactionConsumer** activo.

---

## 4) Comprobar que el estado cambió en PostgreSQL

Con `psql` o DataGrip contra `localhost:5433`, base `flashbank_transactions`:

```sql
SELECT id, account_id, amount, type, status, created_at
FROM transactions
ORDER BY created_at DESC
LIMIT 5;
```

Tras el `TransactionUpdate` con `Completed`, el `status` de esa fila debe pasar de `Pending` a `Completed` (o `Failed`).

---

## Resumen del flujo

```text
POST /transaction  →  INSERT Pending  →  Publish TransactionCreated  →  cola(s) SQS
                                                                         ↓
SimulateTransactionUpdate  →  Publish TransactionUpdate  →  cola consumer Transactions
                                                                         ↓
                                                    TransactionConsumer  →  UPDATE status
```
