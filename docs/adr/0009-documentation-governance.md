# ADR-0009: Proportionate documentation governance

- **Status:** Accepted
- **Date:** 2026-07-26

## Context

The project needs requirements, architecture, and decision history without
turning documentation volume into a substitute for working evidence.

## Decision

Maintain an SRS and traceability matrix, four C4 views, ADRs for accepted
architecture, and RFCs for significant proposed semantic changes. Do not add a
KEP process unless the project develops multi-team governance, release trains,
feature graduation, compatibility policy, and formal production-readiness
review needs.

## Consequences

Documentation is comprehensive but bounded. Every artifact must name its
status and map to source/tests. KEP terminology is not used decoratively.
