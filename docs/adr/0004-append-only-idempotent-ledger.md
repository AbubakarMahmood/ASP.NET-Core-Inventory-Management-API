# ADR-0004: Append-only, idempotent stock ledger

- **Status:** Accepted
- **Date:** 2026-07-26

## Context

Network retries can duplicate receipts or issues, while mutable movement rows
would destroy the historical explanation for a balance.

## Decision

Require an operation ID for every direct movement and work-order issue batch.
Equivalent retries return the original result; conflicting reuse returns 409.
Movement rows store balance-before/after and cannot be modified or deleted
through `ApplicationDbContext`.

## Consequences

Clients must generate and retain operation IDs across retries. Database unique
constraints are the final race boundary. Concurrent identical requests may
produce one success and one conflict before a later retry resolves to the
persisted result; this is acceptable and documented.
