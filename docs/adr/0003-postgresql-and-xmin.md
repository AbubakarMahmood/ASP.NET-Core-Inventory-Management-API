# ADR-0003: PostgreSQL migrations and `xmin` concurrency

- **Status:** Accepted
- **Date:** 2026-07-26

## Context

Inventory balances are vulnerable to lost updates. The application already
uses Npgsql and PostgreSQL-specific concurrency metadata.

## Decision

Use PostgreSQL as the authoritative datastore, EF Core migrations for schema
history, relational constraints for invariants, and PostgreSQL `xmin` as the
optimistic concurrency token for mutable rows.

## Consequences

Provider-real integration tests are mandatory. The HTTP integration suite has no EF InMemory fallback because it cannot establish migration, constraint, transaction, trigger, or `xmin` behavior.
