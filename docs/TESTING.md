# Testing and evidence strategy

## Evidence levels

1. **Static/source inspection** — configuration parsing, link checks, secret
   scans, source-shape checks, and reasoned review.
2. **Unit execution** — domain, handler, validator, and cryptographic-service
   tests under the real .NET test runner.
3. **HTTP integration with PostgreSQL** — the real ASP.NET pipeline, complete EF
   migration chain, relational constraints, transactions, triggers, and `xmin`.
4. **Container smoke** — built API/UI images, PostgreSQL, readiness, static
   assets, authentication, and a representative idempotent operation.
5. **Observed CI on the final commit** — authoritative repository evidence.

There is deliberately no EF InMemory integration mode. It cannot establish the
provider-specific guarantees that distinguish this project.

## Unit focus

- Work-order transitions, fulfilment, and completion invariants.
- Product balance arithmetic and overflow/negative-stock rejection.
- Direct-movement snapshots and retry semantics.
- Work-order issue-batch prevalidation and retry semantics.
- Password legacy upgrade and versioned hashing.
- Refresh-token hashing and rotation.
- FluentValidation contracts.

## PostgreSQL HTTP integration

```bash
export STOCKVERITY_TEST_POSTGRES='Host=127.0.0.1;Port=5432;Database=postgres;Username=postgres;Password=postgres'
dotnet test tests/InventoryAPI.IntegrationTests -c Release
```

The fixture creates a unique database, starts the real application, applies all
migrations, seeds deterministic demo identities, and exercises public HTTP
contracts. It verifies opening-balance creation, strict product updates,
sequential and concurrent idempotency, concurrent `xmin` conflicts, conflicting
operation-key reuse, insufficient-stock atomicity, append-only database
triggers, migration rollback/reapply and unsafe-data rejection, refresh
rotation, per-client authentication throttling, protected API and SignalR
surfaces, historical-attribution deletion guards, deleted-entity audit
visibility, export payload signatures, role boundaries, and work-order
fulfilment.

The supplied PostgreSQL identity must be allowed to create and drop test
databases. Test databases use the `stockverity_tests_*` prefix.

## Compose smoke

`./scripts/smoke-compose.sh` verifies that:

- Compose resolves only after required secrets are supplied;
- all images build;
- PostgreSQL becomes healthy and the API reaches readiness;
- the Blazor client is served through nginx;
- a request larger than the one-megabyte limit returns `413`;
- a demo administrator can authenticate;
- product creation emits an opening balance;
- an equivalent receipt retry returns the same movement and changes stock once;
- materially different operation-key reuse returns `409`;
- a custom-format backup restores to a new database with matching core-table
  and migration fingerprints;
- the API starts against that restore with migration, demo, and OpenAPI switches
  disabled;
- readiness fails while PostgreSQL is stopped and recovers after restart.

## Recorded local baseline

The 2026-07-28 source snapshot produced:

- zero Release build warnings and errors;
- 101 passing unit tests with no skips;
- 25 passing PostgreSQL integration tests with no skips;
- successful API and Blazor publish;
- a passing Compose smoke with PostgreSQL 16.

## Remaining release gates

- Append-only rejection using the restricted deployment database role, not only
  the migration owner used by tests.
- Rate-limit window reset behavior; per-client enforcement and `429` rejection
  are covered.
- Export load/memory characterization at the current 10,000-row hard cap;
  readable XLSX/PDF signatures are covered.
- SignalR notification visibility policy; authenticated and unauthenticated
  negotiation behavior is covered.
- Browser end-to-end checks for critical UI flows.
- Green GitHub Actions jobs on the exact published commit.

## Test-data policy

Tests generate unique SKUs and databases. Public demo credentials may appear
only in demo/test configuration. Real personal or operational data must not
appear in fixtures, logs, screenshots, or artifacts.
