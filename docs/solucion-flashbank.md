# FlashBank — Decisiones técnicas y arquitectura de solución

## Diagrama de arquitectura

![Diagrama de arquitectura FlashBank](../diagrama.jpeg)

---

## Contexto del problema

FlashBank es un neobanco que migra desde un monolito hacia microservicios para escalar ante el crecimiento de usuarios. El dolor más concreto reportado es el endpoint `GET /accounts/{id}/history`, cuya consulta directa sobre la tabla `transactions` (con cientos de millones de filas) genera picos de CPU del 95% en la base de datos relacional. La prueba técnica exige resolver tres problemas interrelacionados.

---

## 1. Arquitectura de microservicios — desacoplamiento entre servicios

### El problema del acoplamiento

En un monolito o en microservicios mal diseñados, el servicio de **Transacciones** llamaría directamente (HTTP síncrono) al servicio de **Cuentas** para actualizar el saldo. Esto crea acoplamiento temporal: si Cuentas está caído o lento, Transacciones también falla. Los servicios evolucionan juntos y no pueden desplegarse de forma independiente.

### La solución implementada: mensajería asíncrona vía SQS/SNS con MassTransit

Transacciones y Cuentas no se conocen entre sí. Se comunican **exclusivamente a través de eventos**, siguiendo el patrón **Event-Driven Architecture**:

```
Cliente
  │
  ▼
POST /transaction                     ← FlashBank.Transactions (API)
  │  Persiste Tx (Pending) en PostgreSQL
  │  Publica TransactionCreated → SNS topic "transaction-created"
  │
  ▼
SQS "transaction-created-queue"
  │
  ▼
AccountConsumer                       ← FlashBank.Accounts.Worker
  │  Actualiza saldo en PostgreSQL
  │  Publica TransactionUpdate (Completed | Failed) → SNS topic "transaction-update"
  │
  ▼
SQS "transaction-update-queue"
  │
  ▼
TransactionConsumer                   ← FlashBank.Transactions
  │  Actualiza estado de Tx en PostgreSQL
  └─ Escribe documento en MongoDB     ← Modelo de lectura (historial)
```

MassTransit 8.x abstrae el transporte (Amazon SQS + SNS) y define los nombres de entidad explícitamente (`SetEntityName`), lo que alinea publishers y consumers sin que ningún servicio conozca la dirección física del otro. En desarrollo, **LocalStack** emula toda la infraestructura AWS en el puerto `4566`; el script `init/init-aws.sh` crea colas, topics SNS y suscripciones al arrancar el contenedor.

### Resultado

- **Transacciones** y **Cuentas** despliegan, escalan y fallan de forma completamente independiente.
- Si el worker de Cuentas está caído, el mensaje queda retenido en la cola SQS hasta que vuelva. La API de Transacciones sigue respondiendo.
- Agregar nuevos consumidores (por ejemplo, auditoría, notificaciones push) solo requiere suscribirse al topic SNS sin tocar código existente.

---

## 2. Optimización de lectura — el modelo CQRS con MongoDB

### El problema

El endpoint `GET /accounts/{id}/history` ejecuta un `SELECT` sobre `transactions` cada vez que un usuario abre la app. Con cientos de millones de registros, esa tabla relacional no está optimizada para lecturas de historial paginado por cuenta; cada consulta golpea índices grandes y compite con las escrituras del flujo transaccional.

### La solución implementada: read model en MongoDB (CQRS)

Se aplica el patrón **CQRS (Command Query Responsibility Segregation)**: el lado de escritura sigue siendo PostgreSQL (source of truth transaccional), pero se mantiene un **modelo de lectura desnormalizado** en MongoDB diseñado específicamente para la consulta de historial.

Cada vez que una transacción alcanza un estado final (`Completed` o `Failed`), `TransactionService.UpdateStatusAsync` escribe un documento en la colección `transaction-history` de MongoDB:

```csharp
// FlashBank.Transactions/Services/TransactionService.cs
var historyDoc = new TransactionHistoryDocument
{
    TransactionId = transaction.Id,
    AccountId     = transaction.AccountId,
    Amount        = transaction.Amount,
    Type          = transaction.Type.ToString(),
    Status        = transaction.Status.ToString(),
    OccurredAt    = DateTime.UtcNow
};
await _history.InsertAsync(historyDoc, ct);
```

El documento `TransactionHistoryDocument` está aplanado: sin joins, sin relaciones. Una consulta de historial para una cuenta es un simple `find({ accountId: "..." })` sobre una colección optimizada para lectura, con índice sobre `accountId`.

### Por qué MongoDB y no PostgreSQL para lectura

| | PostgreSQL (write side) | MongoDB (read side) |
|---|---|---|
| Modelo | Relacional normalizado | Documentos desnormalizados |
| Acceso | Escritura transaccional | Lectura por `accountId` |
| Escala | Vertical + replicación | Escalabilidad horizontal nativa |
| Carga en pico | Alta (miles de escrituras/seg) | Aislada (sin competencia con writes) |

Al separar las bases de datos por responsabilidad, el pico de CPU en la base relacional desaparece para las consultas de historial: esas lecturas **nunca más tocan PostgreSQL**.

### Flujo de escritura al historial

El diseño es deliberado: el historial se escribe **solo con estado definitivo** para evitar registrar movimientos intermedios (`Pending`). `FlashBank.History` existe como servicio independiente con su propio consumer de `TransactionCreated`, pero actualmente solo registra un log; la escritura efectiva ocurre en `TransactionService` porque en ese punto ya existe el estado final y los datos completos de la transacción.

---

## 3. Consistencia de datos en entornos distribuidos

### El problema

En un sistema distribuido, una transacción puede persistirse en PostgreSQL pero fallar antes de que el saldo de la cuenta se actualice (o viceversa). Sin un mecanismo de compensación, los datos quedarían inconsistentes: la transacción marcada como `Completed` pero el saldo sin modificar, o el saldo debitado sin registro de transacción.

### La solución implementada: compensación por eventos con garantía de entrega

La consistencia se garantiza mediante una **saga de compensación** implícita en el flujo de eventos, aprovechando las garantías **at-least-once** de SQS:

**Paso 1 — Escritura local primero.** El servicio de Transacciones persiste la fila con estado `Pending` en su propia base PostgreSQL *antes* de publicar el evento. Si la publicación falla, la transacción queda en `Pending` indefinidamente (detectable y recuperable con un job de reconciliación).

**Paso 2 — Procesamiento atómico en Cuentas.** `AccountConsumer` ejecuta `UpdateBalanceAsync` dentro de la unidad de trabajo de EF Core. Si el `SaveChangesAsync` falla (por ejemplo, fondos insuficientes o cuenta inexistente), se lanza una excepción y **no se confirma el mensaje** a SQS; MassTransit lo deja disponible para reintento.

**Paso 3 — Compensación explícita en el `catch`.** Si `UpdateBalanceAsync` lanza cualquier excepción (incluyendo fondos insuficientes), `AccountConsumer` captura el error y publica `TransactionUpdate` con estado `Failed`:

```csharp
// FlashBank.Accounts.Worker/Consumers/AccountConsumer.cs
catch (Exception ex)
{
    var update = new TransactionUpdate(
        msg.TransactionId,
        msg.AccountId,
        TransactionStatus.Failed,
        ex.Message);

    await context.Publish(update);
}
```

Este evento de compensación viaja por la misma topología SNS/SQS y llega a `TransactionConsumer`, que actualiza el estado en PostgreSQL a `Failed`. El saldo **nunca se toca** si hay error; la base de cuentas permanece coherente.

**Paso 4 — Idempotencia.** Si el broker reintentara un mensaje ya procesado, `UpdateBalanceAsync` aplicaría el movimiento dos veces. Para producción, la extensión natural es agregar una columna `ProcessedTransactionIds` o una tabla de idempotencia en la base de cuentas, que evite procesar el mismo `TransactionId` más de una vez.

### Resumen del contrato de consistencia

| Escenario | Estado en PostgreSQL Tx | Estado en PostgreSQL Accounts |
|---|---|---|
| Flujo exitoso | `Completed` | Saldo actualizado |
| Cuenta no encontrada | `Failed` | Sin cambio |
| Fondos insuficientes | `Failed` | Sin cambio |
| Worker caído (SQS retiene msg) | `Pending` | Sin cambio (reintento posterior) |
| Error transitorio en Accounts | `Pending` → `Completed` tras reintento | Actualizado en reintento |

El saldo de la cuenta **nunca queda afectado** en un estado inconsistente respecto al estado de la transacción.

---

## Componentes del repositorio

| Proyecto | Rol |
|---------|-----|
| `FlashBank.Transactions` | API REST (`POST /transaction`), publicación de `TransactionCreated`, consumo de `TransactionUpdate`, escritura al historial en MongoDB. |
| `FlashBank.Accounts.Worker` | Consume `TransactionCreated`, actualiza saldo, publica `TransactionUpdate` (Completed o Failed). |
| `FlashBank.Accounts` | API de gestión de cuentas (creación, consulta de saldo). |
| `FlashBank.History` | Worker suscrito a `TransactionCreated` para extensión futura (actualmente en modo pasivo/log). |
| `FlashBank.Shared` | Contratos de eventos (`TransactionCreated`, `TransactionUpdate`) y enums compartidos. |

## Infraestructura (Docker Compose)

| Servicio | Puerto host | Propósito |
|---------|------------|-----------|
| `postgres-transactions` | 5433 | Write side de transacciones |
| `postgres-accounts` | 5434 | Write side de cuentas |
| `mongodb` | 27018 | Read model / historial |
| `localstack` | 4566 | Emulación de SQS + SNS en desarrollo |

---

## Referencias

- Arranque del entorno: [`README.md`](../README.md)
- Inicialización SQL, Mongo y colas AWS: [`init/`](../init/)
- Inspección de colas SQS: [`scripts/README.md`](../scripts/README.md) y [`docs/testing/sqs-desde-docker.md`](testing/sqs-desde-docker.md)
