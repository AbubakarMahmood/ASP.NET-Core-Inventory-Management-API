# Claims and evidence ledger

## Current local evidence

The current source snapshot was exercised locally on 2026-07-28 with:

- .NET SDK `8.0.423` and runtime `8.0.29`;
- a zero-warning, zero-error Release build;
- 101 passing unit tests and 25 passing PostgreSQL integration tests;
- successful API and Blazor publish;
- successful API and UI image builds;
- a Compose smoke covering request-size rejection, readiness outage/recovery,
  authentication, opening balance, retry-safe posting, conflict rejection, and
  backup/restore API recovery;
- no known vulnerable NuGet packages in the direct or transitive graph;
- no HIGH or CRITICAL fixed vulnerabilities in the source or rebuilt images,
  and no detected source secrets in the local scan.

The PostgreSQL suite establishes that:

- stock movements carry operation IDs and balance snapshots;
- equivalent sequential and concurrent retries apply the balance change once;
- materially different operation-ID reuse produces a conflict;
- movement entities are protected from update/delete through the application
  DbContext and PostgreSQL trigger;
- refresh tokens are stored as hashes;
- new password hashes are versioned and legacy hashes can upgrade;
- work orders cannot complete with outstanding requested quantities;
- products and users referenced by operational history cannot be deleted, and
  deleted unreferenced entities remain visible in the derived audit timeline;
- every new transfer posting is rejected in the current single-location model;
- migrations apply from empty and legacy-shaped databases;
- migration rollback/reapply preserves valid rows, reinstalls ledger guards,
  revokes plaintext refresh tokens, and rejects unsafe legacy data
  transactionally;
- `xmin` permits only one winner for concurrent stale product updates;
- one transaction atomically persists movement and balance changes.

## Commit-specific evidence

Local evidence does not substitute for the GitHub Actions result attached to a
published commit. Before citing a SHA, confirm its `build-and-test` and
`compose-smoke` jobs are green and the repository is clean at that exact SHA.

The following remain deployment or product-policy gates rather than unverified
implementation claims:

- least-privilege runtime database-role rehearsal;
- TLS, external secret management, and encrypted backup evidence;
- browser end-to-end coverage and export load/memory characterization;
- SignalR notification visibility policy beyond authenticated negotiation;
- owner approval of the final product/repository name.

The NuGet graph contains deprecated but non-vulnerable transitive build/test
dependencies owned by the current EF Core design tooling and xUnit v3 runner.
They are maintenance residue, not a clean-dependency claim.

## Prohibited or unsupported claims

- “Production-ready,” “enterprise-ready,” or “battle-tested.”
- “Full audit trail” for all entities or regulatory compliance.
- Multi-location inventory, reservation, lot, serial, or expiry support.
- Exactly-once distributed processing. The implementation offers retry-safe
  idempotency within one database boundary, not a distributed guarantee.
- Zero downtime, high availability, horizontal scalability, or measured
  performance/SLO claims.
- Security certification, penetration testing, or complete OWASP coverage.
- Complete ERP/WMS/CMMS functionality.
- OSI-approved licensing for every dependency. QuestPDF Community has separate
  eligibility terms even though the repository source is MIT-licensed.

## Evidence record format

Each release candidate should store:

- commit SHA and dirty-state result;
- SDK/runtime/container versions;
- restore/build/test commands and exit codes;
- test counts and skipped tests;
- PostgreSQL version and migration list;
- Compose image IDs/digests;
- smoke transcript;
- documentation/link/static-check transcript;
- known limitations and owner approvals.
