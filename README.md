# StockVerity *(working codename)*

> **Provisional product identity:** an auditable single-location stock ledger
> tied to work-order fulfilment.

Under the working codename StockVerity, this repository is a portfolio-scale
ASP.NET Core and Blazor system for a maintenance storeroom. Its central invariant is deliberately narrower than “inventory
management”: **every successful quantity change has an attributable,
append-only movement, and work-order completion requires every requested unit to
be issued.**

This source snapshot is **not labelled production-ready**. On 2026-07-28, the
pinned SDK produced a zero-warning Release build, 101 unit tests and 25
PostgreSQL integration tests passed, both applications published, both
container images built, and the Compose smoke passed its request-size,
readiness outage/recovery, idempotency, and backup/restore scenarios. A green
GitHub Actions run remains commit-specific evidence rather than something this
README can guarantee. See
[`docs/CLAIMS-AND-EVIDENCE.md`](docs/CLAIMS-AND-EVIDENCE.md).

## What is implemented

- Product catalogue with SKU uniqueness, reorder metadata, one recorded
  location, PostgreSQL `xmin` optimistic concurrency, and soft deletion.
- Explicit `openingStock` creation semantics: a non-zero opening balance creates
  an immutable ledger posting in the same commit as the product.
- Receipt, return, issue, and signed adjustment postings with actor, reason,
  reference, before/after balances, historical unit cost, and retry-safe
  operation IDs.
- Work orders with draft, submission, approval/rejection, start, issue,
  completion, and cancellation rules.
- Atomic multi-line fulfilment: the entire issue batch is validated before any
  requested quantity or stock balance changes.
- JWT role authorization, versioned PBKDF2 password hashes, one-way refresh-token
  storage, token rotation, and rate-limited login/refresh endpoints.
- PostgreSQL migrations, database-backed readiness, explicit startup migration
  and demo switches, Blazor WebAssembly UI, exports, Docker Compose, and CI.

## Intentional boundaries

- One product has one aggregate on-hand balance and one descriptive location.
- **New transfers are rejected.** A true transfer requires per-location balances
  and is proposed in [`RFC-0001`](docs/rfc/0001-multi-location-balances-and-reservations.md).
- The activity screen is a derived timeline, not a tamper-evident compliance
  journal.
- Browser bearer tokens currently use local storage; a BFF/cookie redesign is a
  draft proposal, not an implemented control.
- XLSX export uses MIT-licensed ClosedXML. PDF export uses QuestPDF Community;
  its current eligibility terms must be reviewed before a deployment whose
  ownership or revenue no longer qualifies.
- No lot, serial, expiry, reservation, costing-layer, tenant, ERP, WMS, HA, SLO,
  or regulatory-compliance claim is made.

## Architecture

```text
Browser / API client
        |
        v
ASP.NET Core API  <---->  Blazor WebAssembly + nginx
        |
        v
Application commands, queries, validation, and policies
        |
        v
Domain entities and invariants
        |
        v
EF Core / Npgsql / PostgreSQL
```

The code remains a modular monolith. C4 context, container, component, and
deployment views live in [`docs/architecture`](docs/architecture/README.md).
Accepted trade-offs are recorded in [`docs/adr`](docs/adr/README.md); proposed
semantic changes are in [`docs/rfc`](docs/rfc/README.md).

## Local Compose demo

Copy the environment template and replace both secrets:

```bash
cp .env.example .env
# edit POSTGRES_PASSWORD and JWT_SECRET
docker compose up --build
```

| Surface | Default URL |
|---|---|
| Blazor UI | `http://localhost:3000` |
| API | `http://localhost:5000` |
| OpenAPI, when enabled | `http://localhost:5000/swagger` |
| Readiness | `http://localhost:5000/health/ready` |

The checked-in `.env.example` enables migrations, demo data, and OpenAPI for the
local demo only. Demo identities are:

| Role | Email | Password |
|---|---|---|
| Admin | `admin@stockverity.local` | `Admin123!` |
| Manager | `manager@stockverity.local` | `Manager123!` |
| Operator | `operator@stockverity.local` | `Operator123!` |

Never enable those identities in a real environment.

## Verification

The repository pins SDK `8.0.423`. Unit tests can run independently; HTTP
integration tests deliberately require PostgreSQL because the system relies on
migrations, constraints, triggers, and `xmin`. The supplied test identity must
have permission to create and drop isolated databases.

```bash
dotnet restore InventoryAPI.sln
dotnet build InventoryAPI.sln -c Release --no-restore
dotnet test tests/InventoryAPI.UnitTests -c Release --no-build

export STOCKVERITY_TEST_POSTGRES='Host=127.0.0.1;Port=5432;Database=postgres;Username=postgres;Password=postgres'
dotnet test tests/InventoryAPI.IntegrationTests -c Release --no-build

./scripts/smoke-compose.sh
```

`./scripts/verify.sh` runs restore, build, both test projects, and API/UI publish;
it expects `STOCKVERITY_TEST_POSTGRES` to be set.

The latest recorded local baseline is 101 passing unit tests and 25 passing
PostgreSQL integration tests. `./scripts/smoke-compose.sh` additionally builds
the images and verifies a `413` oversized-body response, UI/API startup,
authentication, opening stock, retry-safe posting, conflict handling,
database-backed readiness failure/recovery, and backup/restore recovery.

## Documentation baseline

- [Software requirements specification](docs/SRS.md)
- [Requirements traceability](docs/TRACEABILITY.md)
- [Data/API contracts](docs/DATA-CONTRACTS.md)
- [Security model](docs/SECURITY.md)
- [Operations runbook](docs/OPERATIONS.md)
- [Testing and evidence policy](docs/TESTING.md)
- [Definition of done](docs/DEFINITION-OF-DONE.md)
- [Product naming decision](docs/PRODUCT-NAMING.md)

The SRS is informed by ISO/IEC/IEEE 29148:2018 structure; this repository does
not claim certification or audited standards conformance. ADRs and RFCs are
used because they fit this repository’s governance scale. KEP machinery is not
used because there is no multi-team Kubernetes-style enhancement process to
coordinate.

## Repository name

`ASP.NET-Core-Inventory-Management-API` is an accurate historical label but a
weak portfolio identity. A collision check found material adjacent-market confusion risk around the
word “Verity”, so **StockVerity remains a working codename, not the recommended
public repository name**. Until the owner completes legal and market clearance, the
safer descriptive slug is **`auditable-inventory-ledger`**. Internal
`InventoryAPI.*` namespaces remain unchanged in this pass to avoid low-value
migration churn.

## License

[MIT](LICENSE)
