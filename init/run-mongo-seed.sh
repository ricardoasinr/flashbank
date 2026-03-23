#!/usr/bin/env bash
# Ejecuta init-mongo-seed.js contra el contenedor mongodb (útil tras cambios o BD ya inicializada).
# Uso: desde la raíz del repo: ./init/run-mongo-seed.sh
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
if [[ ! -f .env ]]; then
  echo "No se encontró .env en $ROOT" >&2
  exit 1
fi
# Carga solo variables MONGO_* (evita side effects del .env completo)
while IFS= read -r line || [[ -n "$line" ]]; do
  [[ "$line" =~ ^[[:space:]]*# ]] && continue
  [[ "$line" =~ ^[[:space:]]*$ ]] && continue
  [[ "$line" =~ ^MONGO_INITDB_ROOT_ ]] && export "$line"
done < .env

docker compose exec -T mongodb mongosh \
  -u "${MONGO_INITDB_ROOT_USERNAME}" \
  -p "${MONGO_INITDB_ROOT_PASSWORD}" \
  --authenticationDatabase admin \
  < "$ROOT/init/init-mongo-seed.js"

echo "Listo."
