#!/bin/bash
set -e

# Local PostgreSQL for the demo. Storage is ephemeral on the free tier;
# the API reseeds demo data on every cold start.
PGBIN=$(ls -d /usr/lib/postgresql/*/bin | head -1)
export PATH="$PGBIN:$PATH"

if [ ! -s /data/pg/PG_VERSION ]; then
    initdb -D /data/pg --auth=trust --username=appuser >/dev/null
fi

pg_ctl -D /data/pg -o "-c listen_addresses=localhost" -w start
createdb -h localhost inventorydb 2>/dev/null || true

exec dotnet InventoryAPI.Api.dll
