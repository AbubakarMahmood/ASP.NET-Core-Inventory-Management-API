# Architecture Decision Records

ADRs record accepted, architecturally significant choices. They are immutable
history: a later decision supersedes an older record rather than rewriting it.

| ADR | Status | Decision |
|---|---|---|
| [0001](0001-product-focus-and-name.md) | Accepted | Focus on maintenance-parts ledger integrity; keep branding provisional |
| [0002](0002-modular-monolith.md) | Accepted | Retain a Clean Architecture modular monolith |
| [0003](0003-postgresql-and-xmin.md) | Accepted | PostgreSQL, EF Core migrations, and `xmin` concurrency |
| [0004](0004-append-only-idempotent-ledger.md) | Accepted | Append-only movement ledger with operation-ID retries |
| [0005](0005-single-location-transfer-boundary.md) | Accepted | Reject new transfers in the current single-location model |
| [0006](0006-work-order-completion-invariant.md) | Accepted | Require requested parts to be issued before completion |
| [0007](0007-authentication-storage.md) | Accepted | Versioned password hashes and hashed refresh tokens |
| [0008](0008-explicit-database-startup.md) | Accepted | Explicit migration/seeding flags and fail-closed startup |
| [0009](0009-documentation-governance.md) | Accepted | SRS + C4 + ADR + RFC; no KEP process at this scale |

Use [`0000-template.md`](0000-template.md) for new records.
