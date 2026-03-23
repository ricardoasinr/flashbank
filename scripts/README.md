# Scripts de desarrollo (FlashBank)

| Script | Uso |
|--------|-----|
| `list-sqs-queues.sh` | Listar nombres/URLs de colas en LocalStack |
| `sqs-show-messages.sh` | **Ver mensajes** de una cola o de **todas** (`--all`), solo el payload JSON |
| `sqs-watch-messages.sh` | Bucle: ir mostrando mensajes de una cola (long polling) |

Ejecutar **desde la raíz del repositorio** `flashbank/`:

```bash
chmod +x scripts/*.sh   # solo la primera vez
./scripts/list-sqs-queues.sh
./scripts/sqs-show-messages.sh --all
./scripts/sqs-show-messages.sh nombre-de-cola
./scripts/sqs-watch-messages.sh nombre-de-cola
```

### Ver mensajes en las colas

1. **Listar colas:** `./scripts/list-sqs-queues.sh`
2. **Inspeccionar todas** (una recepción por cola): `./scripts/sqs-show-messages.sh --all`
3. **Una cola concreta** (p. ej. la que crea MassTransit): `./scripts/sqs-show-messages.sh transaction-created-queue`

**Requisito:** `jq` (`brew install jq`). AWS CLI + credenciales `test`/`test` (como LocalStack).

**Nota:** Los consumers de .NET también leen la cola; si ves “sin mensajes”, puede que ya los hayan consumido. Haz el POST **antes** de ejecutar el script, o para pruebas para el servicio consumer un momento.

**Borrar tras leer** (limpieza): `./scripts/sqs-show-messages.sh mi-cola --delete` (cuidado en entornos compartidos).

Variables opcionales: `LOCALSTACK_ENDPOINT`, `SQS_WAIT_SECONDS`, `SQS_VISIBILITY_TIMEOUT`, `SQS_MAX_MESSAGES`.

Flujo HTTP y eventos (curl, workers): [README principal](../README.md). SQS solo con Docker: [docs/testing/sqs-desde-docker.md](../docs/testing/sqs-desde-docker.md).
