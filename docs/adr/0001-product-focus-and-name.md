# ADR-0001: Focus the product around ledger integrity

- **Status:** Accepted
- **Date:** 2026-07-26

## Context

“ASP.NET Core Inventory Management API” describes a stack and generic CRUD
category. The existing work-order, stock-movement, and concurrency mechanisms
can support a more credible portfolio thesis, but only if claims are narrowed.

## Decision

Present the product around **auditable maintenance-parts inventory and
work-order control**. Use StockVerity only as a working codename, and prefer the
descriptive repository slug `auditable-inventory-ledger` until a final name is
cleared. Preserve internal `InventoryAPI.*` identifiers until a separate
compatibility-neutral rename is justified.

## Consequences

Public material emphasizes integrity invariants. ERP, WMS, multi-location, and
compliance-audit claims are prohibited. The remote repository rename and final product brand are delayed until closure
evidence and owner-controlled name clearance exist.

## Alternatives considered

Archive the project; keep the generic name; rename every assembly immediately.
The first wastes a viable core, the second is undifferentiated, and the third
creates risk without behavioral value.
