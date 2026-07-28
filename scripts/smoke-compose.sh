#!/usr/bin/env bash
set -euo pipefail

repo_root=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
cd "$repo_root"

for command in docker curl python3; do
  if ! command -v "$command" >/dev/null 2>&1; then
    echo "$command is required" >&2
    exit 2
  fi
done

created_env=0
env_file=${STOCKVERITY_ENV_FILE:-}
backup_file=
restore_env_file=
oversized_file=
if [[ -z "$env_file" ]]; then
  env_file=$(mktemp)
  created_env=1
  cat >"$env_file" <<ENV
POSTGRES_DB=stockverity_smoke
POSTGRES_USER=stockverity
POSTGRES_PASSWORD=smoke-database-password-$(date +%s)
JWT_SECRET=smoke-only-signing-key-$(printf 'x%.0s' {1..64})
POSTGRES_PORT=${POSTGRES_PORT:-55433}
API_PORT=${API_PORT:-55000}
UI_PORT=${UI_PORT:-53000}
APPLY_MIGRATIONS=true
DEMO_DATA_ENABLED=true
OPENAPI_ENABLED=true
ENV
fi

api_port=$(awk -F= '$1=="API_PORT" {print $2}' "$env_file" | tail -1)
ui_port=$(awk -F= '$1=="UI_PORT" {print $2}' "$env_file" | tail -1)
api_port=${api_port:-5000}
ui_port=${ui_port:-3000}
compose=(docker compose --env-file "$env_file")

cleanup() {
  status=$?
  if [[ $status -ne 0 ]]; then
    "${compose[@]}" ps || true
    "${compose[@]}" logs --no-color --tail=250 || true
  fi
  "${compose[@]}" down -v --remove-orphans >/dev/null 2>&1 || true
  if [[ $created_env -eq 1 ]]; then
    rm -f "$env_file"
  fi
  if [[ -n "$backup_file" ]]; then
    rm -f "$backup_file"
  fi
  if [[ -n "$restore_env_file" ]]; then
    rm -f "$restore_env_file"
  fi
  if [[ -n "$oversized_file" ]]; then
    rm -f "$oversized_file"
  fi
  exit "$status"
}
trap cleanup EXIT INT TERM

"${compose[@]}" config >/dev/null
"${compose[@]}" up -d --build

for attempt in {1..90}; do
  if curl --fail --silent --show-error "http://127.0.0.1:${api_port}/health/ready" >/dev/null; then
    break
  fi
  if [[ $attempt -eq 90 ]]; then
    echo "API readiness check timed out" >&2
    exit 1
  fi
  sleep 2
done

curl --fail --silent --show-error "http://127.0.0.1:${ui_port}/" >/dev/null

oversized_file=$(mktemp)
python3 - "$oversized_file" <<'PY'
import json
import sys

with open(sys.argv[1], "w", encoding="utf-8") as output:
    json.dump({"email": "admin@stockverity.local", "password": "x" * 1_050_000}, output)
PY
oversized_status=$(curl --silent --output /dev/null --write-out '%{http_code}' \
  -H 'Content-Type: application/json' \
  --data-binary "@$oversized_file" \
  "http://127.0.0.1:${api_port}/api/v1/auth/login")
[[ "$oversized_status" == "413" ]]
rm -f "$oversized_file"
oversized_file=

login_json=$(curl --fail --silent --show-error \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@stockverity.local","password":"Admin123!"}' \
  "http://127.0.0.1:${api_port}/api/v1/auth/login")
token=$(python3 -c 'import json,sys; print(json.load(sys.stdin)["accessToken"])' <<<"$login_json")

sku="SMOKE-$(date +%s)-$RANDOM"
product_json=$(curl --fail --silent --show-error \
  -H "Authorization: Bearer $token" \
  -H 'Content-Type: application/json' \
  -d "{\"sku\":\"$sku\",\"name\":\"Smoke test part\",\"description\":\"Created by the Compose smoke test\",\"category\":\"Test\",\"openingStock\":10,\"reorderPoint\":2,\"reorderQuantity\":5,\"unitOfMeasure\":\"EA\",\"unitCost\":1.25,\"location\":\"SMOKE-A\"}" \
  "http://127.0.0.1:${api_port}/api/v1/products")
product_id=$(python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])' <<<"$product_json")
operation_id=$(python3 -c 'import uuid; print(uuid.uuid4())')
movement_payload="{\"operationId\":\"$operation_id\",\"productId\":\"$product_id\",\"type\":\"Receipt\",\"quantity\":5,\"reason\":\"Compose idempotency smoke\"}"

first=$(curl --fail --silent --show-error \
  -H "Authorization: Bearer $token" \
  -H 'Content-Type: application/json' \
  -d "$movement_payload" \
  "http://127.0.0.1:${api_port}/api/v1/stockmovements")
retry=$(curl --fail --silent --show-error \
  -H "Authorization: Bearer $token" \
  -H 'Content-Type: application/json' \
  -d "$movement_payload" \
  "http://127.0.0.1:${api_port}/api/v1/stockmovements")

FIRST="$first" RETRY="$retry" python3 - <<'PY'
import json, os
first = json.loads(os.environ["FIRST"])
retry = json.loads(os.environ["RETRY"])
assert first["id"] == retry["id"], (first, retry)
assert first["operationId"] == retry["operationId"]
assert first["balanceBefore"] == 10
assert first["balanceAfter"] == 15
assert retry["balanceAfter"] == 15
PY

conflict_status=$(curl --silent --output /tmp/stockverity-conflict.json --write-out '%{http_code}' \
  -H "Authorization: Bearer $token" \
  -H 'Content-Type: application/json' \
  -d "{\"operationId\":\"$operation_id\",\"productId\":\"$product_id\",\"type\":\"Receipt\",\"quantity\":6,\"reason\":\"Different operation\"}" \
  "http://127.0.0.1:${api_port}/api/v1/stockmovements")
[[ "$conflict_status" == "409" ]]

product_after=$(curl --fail --silent --show-error \
  -H "Authorization: Bearer $token" \
  "http://127.0.0.1:${api_port}/api/v1/products/${product_id}")
PRODUCT_AFTER="$product_after" python3 - <<'PY'
import json, os
product = json.loads(os.environ["PRODUCT_AFTER"])
assert product["currentStock"] == 15, product
PY

database_name=$(awk -F= '$1=="POSTGRES_DB" {print $2}' "$env_file" | tail -1)
database_user=$(awk -F= '$1=="POSTGRES_USER" {print $2}' "$env_file" | tail -1)
restore_database="stockverity_restore_${RANDOM}_$(date +%s)"
backup_file=$(mktemp)
restore_env_file=$(mktemp)

"${compose[@]}" exec -T postgres \
  pg_dump --username "$database_user" --dbname "$database_name" --format=custom \
  >"$backup_file"
"${compose[@]}" exec -T postgres \
  createdb --username "$database_user" "$restore_database"
"${compose[@]}" exec -T postgres \
  pg_restore --username "$database_user" --dbname "$restore_database" \
  --exit-on-error --no-owner --no-privileges <"$backup_file"

fingerprint_sql=$(cat <<'SQL'
SELECT
  (SELECT COUNT(*) FROM "Users") || ':' ||
  (SELECT COUNT(*) FROM "Products") || ':' ||
  (SELECT COUNT(*) FROM "StockMovements") || ':' ||
  (SELECT COUNT(*) FROM "WorkOrders") || ':' ||
  (SELECT COUNT(*) FROM "WorkOrderItems") || ':' ||
  (SELECT string_agg("MigrationId", ',' ORDER BY "MigrationId") FROM "__EFMigrationsHistory");
SQL
)
source_fingerprint=$("${compose[@]}" exec -T postgres \
  psql --username "$database_user" --dbname "$database_name" \
  --tuples-only --no-align --command "$fingerprint_sql")
restore_fingerprint=$("${compose[@]}" exec -T postgres \
  psql --username "$database_user" --dbname "$restore_database" \
  --tuples-only --no-align --command "$fingerprint_sql")
[[ "$source_fingerprint" == "$restore_fingerprint" ]]

awk -F= -v restored_database="$restore_database" '
  $1=="POSTGRES_DB" { print "POSTGRES_DB=" restored_database; next }
  $1=="APPLY_MIGRATIONS" { print "APPLY_MIGRATIONS=false"; next }
  $1=="DEMO_DATA_ENABLED" { print "DEMO_DATA_ENABLED=false"; next }
  $1=="OPENAPI_ENABLED" { print "OPENAPI_ENABLED=false"; next }
  { print }
' "$env_file" >"$restore_env_file"
restore_compose=(docker compose --env-file "$restore_env_file")
"${restore_compose[@]}" up -d --no-deps --force-recreate api

for attempt in {1..60}; do
  if curl --fail --silent --show-error "http://127.0.0.1:${api_port}/health/ready" >/dev/null; then
    break
  fi
  if [[ $attempt -eq 60 ]]; then
    echo "Restored API readiness check timed out" >&2
    exit 1
  fi
  sleep 2
done

restore_login=$(curl --fail --silent --show-error \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@stockverity.local","password":"Admin123!"}' \
  "http://127.0.0.1:${api_port}/api/v1/auth/login")
restore_token=$(python3 -c 'import json,sys; print(json.load(sys.stdin)["accessToken"])' <<<"$restore_login")
restored_product=$(curl --fail --silent --show-error \
  -H "Authorization: Bearer $restore_token" \
  "http://127.0.0.1:${api_port}/api/v1/products/${product_id}")
RESTORED_PRODUCT="$restored_product" python3 - <<'PY'
import json, os
product = json.loads(os.environ["RESTORED_PRODUCT"])
assert product["currentStock"] == 15, product
PY

"${compose[@]}" stop postgres >/dev/null
readiness_failed=0
for attempt in {1..30}; do
  readiness_status=$(curl --silent --output /dev/null --write-out '%{http_code}' \
    "http://127.0.0.1:${api_port}/health/ready" || true)
  if [[ "$readiness_status" != "200" ]]; then
    readiness_failed=1
    break
  fi
  sleep 1
done
[[ $readiness_failed -eq 1 ]]

"${compose[@]}" start postgres >/dev/null
for attempt in {1..60}; do
  if curl --fail --silent --show-error "http://127.0.0.1:${api_port}/health/ready" >/dev/null; then
    break
  fi
  if [[ $attempt -eq 60 ]]; then
    echo "API readiness did not recover after PostgreSQL restart" >&2
    exit 1
  fi
  sleep 2
done

echo "Compose smoke passed: request-size rejection, readiness failure/recovery, UI, authentication, opening balance, idempotent posting, conflict rejection, cached balance, and backup/restore API recovery (${source_fingerprint})."
