# Operations runbook

## Configuration

Required in every runtime:

| Setting | Requirement |
|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| `JwtSettings:SecretKey` | Random secret, at least 32 characters; 64+ recommended |
| `JwtSettings:Issuer` | Must match issued-token validation, default `StockVerity` |
| `JwtSettings:Audience` | Must match issued-token validation, default `StockVerityUsers` |
| `Cors:AllowedOrigins` | Exact browser origins |

Explicit startup switches:

| Setting | Default | Meaning |
|---|---:|---|
| `Database:ApplyMigrations` | `false` | Apply pending EF migrations during startup |
| `DemoData:Enabled` | `false` | Create public demo identities/data |
| `OpenApi:Enabled` | `false` | Serve Swagger/OpenAPI |
| `HttpsRedirection:Enabled` | production-dependent | Redirect direct HTTP requests |

A real deployment should normally apply migrations in a controlled release
step and keep all three feature switches false.

## Startup behavior

Startup is fail-closed:

1. connect to PostgreSQL with bounded retries;
2. inspect pending migrations;
3. apply them only when explicitly enabled;
4. seed only when explicitly enabled;
5. abort startup on connection, migration, or seed failure.

A listening process therefore indicates that the initialization contract
completed; `/health/ready` additionally checks database readiness.

## Migration procedure

1. Back up the database and record the application commit/image digest.
2. Review generated SQL:

   ```bash
   dotnet ef migrations script \
     --idempotent \
     --project src/InventoryAPI.Infrastructure \
     --startup-project src/InventoryAPI.Api
   ```

3. Apply in staging against a production-like copy.
4. Run the PostgreSQL integration suite and smoke scenarios.
5. Apply during a controlled release window:

   ```bash
   dotnet ef database update \
     --project src/InventoryAPI.Infrastructure \
     --startup-project src/InventoryAPI.Api
   ```

6. Start the API with `Database:ApplyMigrations=false` and verify readiness.

Never edit an already released migration to make a new environment pass. Add a
new corrective migration.

## Backup and restore

Minimum backup evidence before claiming operational readiness:

```bash
pg_dump --format=custom --file=stockverity.dump "$DATABASE_URL"
createdb stockverity_restore_test
pg_restore --clean --if-exists --dbname=stockverity_restore_test stockverity.dump
```

Then point a non-production API instance at the restored database, verify
migration state, login, product count, movement count, and work-order detail.
Record backup timestamp, PostgreSQL version, restore duration, and checks.

`./scripts/smoke-compose.sh` automates a local acceptance drill: it creates a
custom-format dump, restores it into a new database, compares core-table and
migration fingerprints, starts the API against the restore with migrations,
demo data, and OpenAPI disabled, then verifies login and stock state. It also
stops PostgreSQL to prove readiness fails and restarts it to prove recovery.
This is evidence for the demo topology, not a substitute for encrypted,
retained, timed backups in a real deployment.

## Health and observation

- `/health/live`: process-level liveness; no dependency probe.
- `/health/ready`: PostgreSQL-backed readiness.
- `/api/v1/health`: compatibility alias for readiness.

Alert on sustained readiness failure, restart loops, authentication failure
spikes, 409 concurrency/idempotency spikes, 429 rate-limit spikes, and any 500.
Do not log bearer tokens, passwords, refresh tokens, or connection strings.

## Incident playbooks

### Database unavailable

1. Stop automated restarts if they amplify load.
2. Confirm DNS/network/credentials and PostgreSQL health.
3. Do not switch to an in-memory fallback.
4. Restore service, verify pending migrations, then readiness.
5. Reconcile operations whose clients saw an uncertain response by retrying the
   same operation IDs.

### Suspected JWT secret exposure

1. Disable ingress or authentication-sensitive operations.
2. Rotate the signing secret through the secret manager.
3. Restart all API replicas together; existing access tokens become invalid.
4. Revoke refresh-token hashes if the database or token stream may also be
   exposed.
5. Review logs without copying secrets into the incident record.

### Stock discrepancy

1. Freeze affected product operations.
2. Export product movement rows in timestamp/ID order.
3. Recompute from an agreed opening balance using recorded deltas and compare
   every balance-before/after snapshot.
4. Preserve evidence; do not rewrite movement rows.
5. Record a reasoned adjustment as a new operation if correction is approved.

## Compose demo

```bash
cp .env.example .env
# replace secrets
docker compose up --build
./scripts/smoke-compose.sh
```

The smoke script tears down its isolated volumes. The normal Compose stack does
not; use `docker compose down -v` only when intentionally deleting demo data.
