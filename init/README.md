# `init/` — Arranque de infraestructura (Docker / bases de datos)

Aquí van **solo** los artefactos que usa el entorno al levantar contenedores o poblar datos iniciales:

| Archivo | Propósito |
|---------|-----------|
| `init-transactions.sql` | Esquema + seed PostgreSQL (servicio `postgres-transactions`) |
| `init-accounts.sql` | Esquema + seed PostgreSQL (`postgres-accounts`) |
| `init-mongo-seed.js` | Seed MongoDB |
| `run-mongo-seed.sh` | Ejecutar seed manualmente |
| `init-aws.sh` | Crea colas SQS en LocalStack (hook `ready.d`) |

**Probar el flujo** (curl, workers, scripts SQS): [`README.md`](../README.md) y [`scripts/README.md`](../scripts/README.md).
