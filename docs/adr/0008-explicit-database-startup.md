# ADR-0008: Explicit, fail-closed database startup

- **Status:** Accepted
- **Date:** 2026-07-26

## Context

Coupling migrations and demo seeding to `Development` caused environment names
to change data implicitly, while failed initialization could leave a listening
but unusable API.

## Decision

`Database:ApplyMigrations` and `DemoData:Enabled` independently control those
actions. Startup verifies connectivity and migration state in every environment
and aborts when the database is not ready.

## Consequences

Deployments are more predictable. Local users must opt in explicitly. Production
should normally run migrations as a release step and leave both flags false.
