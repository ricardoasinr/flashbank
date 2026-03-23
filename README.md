# FlashBank

Solución .NET 8 con varios servicios (transacciones, cuentas, historial) y mensajería **MassTransit** sobre **Amazon SQS/SNS** vía **LocalStack** en desarrollo. El flujo típico: la API de transacciones persiste un movimiento en **Pending**, publica `TransactionCreated`; el worker de cuentas consume, actualiza saldo y publica `TransactionUpdate`; el consumer de transacciones actualiza el estado en PostgreSQL.

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Docker y Docker Compose
- (Opcional, scripts SQS en el host) AWS CLI + [`jq`](https://jqlang.github.io/jq/)

Copia y ajusta variables desde `.env` si el repo lo incluye como plantilla.

## Arranque rápido

1. **Infraestructura** (PostgreSQL transacciones, LocalStack y, según lo que vayas a probar, el resto):

   ```bash
   docker compose up -d postgres-transactions localstack
   ```

   Colas SQS en LocalStack: ver [`init/README.md`](init/README.md) (`init-aws.sh` en el arranque del contenedor).

2. **Connection strings** en `FlashBank.*/appsettings.Development.json` alineados con los puertos del compose (p. ej. Postgres transacciones en el host suele ser `localhost:5433`).

3. **Servicios .NET** que necesites para el flujo completo, por ejemplo:

   - `FlashBank.Transactions` — API HTTP y publicación de eventos.
   - `FlashBank.Accounts.Worker` — consume `TransactionCreated` y publica `TransactionUpdate`.

## Probar el flujo (curl)

`POST` de ejemplo (puerto por defecto del proyecto; confírmalo en `launchSettings` si cambia):

```bash
curl -sS 'http://localhost:5136/transaction' \
  -H 'Content-Type: application/json' \
  -d '{"accountId":"645f0886-982a-4ca4-adba-ced32e8b696b","amount":100,"type":1}'
```

- `type`: `0` = depósito, `1` = retiro (**Withdrawal**).
- Respuesta esperada: **201** con `id` y `status: Pending` si Postgres y LocalStack responden.

### Qué ocurre detrás

1. Validación (`amount > 0`, `accountId` no vacío).
2. Inserción en `transactions` con estado pendiente.
3. Publicación de `TransactionCreated` hacia la topología SQS/SNS.
4. Con el worker de cuentas en marcha: `AccountConsumer` publica `TransactionUpdate` (completado o fallo) y `TransactionConsumer` actualiza el estado en la base.

### Verificar en SQL

```sql
SELECT id, account_id, amount, type, status, created_at
FROM transactions
ORDER BY created_at DESC
LIMIT 5;
```

## MassTransit

El repo usa **MassTransit 8.x** (p. ej. 8.5.5 con `MassTransit.AmazonSQS`). Las versiones **9+** requieren licencia comercial; mantener 8.x es coherente con .NET 8 y desarrollo sin `MT_LICENSE`.

## Si algo falla

| Síntoma | Causa habitual |
|--------|----------------|
| `Connection refused` hacia la API | `FlashBank.Transactions` no está en ejecución o el puerto no coincide. |
| 500 al guardar | Postgres caído o cadena de conexión incorrecta. |
| 500 tras insertar | LocalStack (SQS/SNS) no accesible o endpoint/puerto distinto (`4566`). |
| 400 | `amount <= 0` o `accountId` inválido/vacío. |
| Cola SQS “vacía” al inspeccionar | Un consumer .NET ya leyó el mensaje; repetir justo después del POST o pausar el worker para depurar. |

## Documentación y scripts

| Recurso | Contenido |
|---------|-----------|
| [`scripts/README.md`](scripts/README.md) | Scripts para listar colas y ver mensajes SQS en LocalStack (`jq` + AWS CLI). |
| [`docs/testing/sqs-desde-docker.md`](docs/testing/sqs-desde-docker.md) | Mismas ideas usando `awslocal` dentro del contenedor LocalStack. |
| [`init/README.md`](init/README.md) | SQL seeds, MongoDB y creación de colas al levantar Docker. |
