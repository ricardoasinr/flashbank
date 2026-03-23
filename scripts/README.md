# Scripts de desarrollo (FlashBank)

| Script | Uso |
|--------|-----|
| `list-sqs-queues.sh` | Ver colas en LocalStack (`http://localhost:4566`) |
| `peek-sqs-queue.sh` | Inspeccionar mensajes de una cola por nombre |
| `send-transaction-update.sh` | Simular `TransactionUpdate` (MassTransit → segundo paso EDA) |

Ejecutar **desde la raíz del repositorio** `flashbank/`:

```bash
chmod +x scripts/*.sh   # solo la primera vez
./scripts/list-sqs-queues.sh
./scripts/peek-sqs-queue.sh nombre-de-cola
./scripts/send-transaction-update.sh "<TransactionId>" "<AccountId>" Completed
```

La guía paso a paso está en [docs/testing/pruebas-eda.md](../docs/testing/pruebas-eda.md).
