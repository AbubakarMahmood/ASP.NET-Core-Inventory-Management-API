# ADR-0002: Retain a modular monolith

- **Status:** Accepted
- **Date:** 2026-07-26

## Context

The product has one transactional consistency boundary and no independent team
or scaling requirement that justifies distributed services.

## Decision

Retain Domain, Application, Infrastructure, API, and Blazor projects with
inward dependencies and one PostgreSQL database.

## Consequences

Stock and work-order changes can commit atomically. Deployment remains simple.
The repository must resist service extraction until a measured boundary,
independent lifecycle, and failure strategy exist.
