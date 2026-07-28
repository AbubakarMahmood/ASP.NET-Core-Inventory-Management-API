# RFC-0001: Multi-location balances and reservations

- **Status:** Draft
- **Created:** 2026-07-26
- **Target:** no committed release

## Summary

Replace `Product.CurrentStock + Product.Location` as the sole quantity model
with per-location balances, explicit reservations, and atomic transfer records.

## Motivation

The current model correctly represents one aggregate balance in one location.
It cannot represent partial transfers, picks across bins, reserved stock, or
available-versus-on-hand quantities. Adding those features to the existing row
would corrupt semantics.

## Goals

- Store on-hand balance by product and location.
- Distinguish on-hand, reserved, and available quantities.
- Transfer any valid quantity atomically from one location to another.
- Reserve and release quantities for work orders without oversubscription.
- Preserve an immutable ledger with before/after snapshots per affected
  location.

## Non-goals

Lots, serial numbers, expiry, warehouse routing, replenishment optimization,
and offline synchronization are separate proposals.

## Proposed model

- `Location(Id, Code, Name, IsActive)`
- `InventoryBalance(ProductId, LocationId, OnHand, Reserved, Version)`
- `Reservation(Id, OperationId, WorkOrderId, ProductId, LocationId, Quantity, Status)`
- `StockOperation(Id, OperationId, Type, Actor, Timestamp, Reference)`
- `StockOperationLine(OperationId, ProductId, LocationId, QuantityDelta, Before, After)`

A transfer creates one operation with a negative source line and positive
destination line in one serializable or otherwise concurrency-safe transaction.

## API

Introduce operation-level endpoints with an `operationId`, one or more lines,
and explicit source/destination location IDs. Keep the current direct endpoint
only through a documented compatibility period.

## Migration

Create a default location for each existing product, migrate `CurrentStock` to
one balance row, then stop writing the old fields. A later migration may remove
or derive them. Migration must reconcile the sum of balance rows against every
product before cutover.

## Security and operations

Location permissions may become necessary. Reservation expiry requires a
scheduled, observable process. Reconciliation reports become mandatory.

## Verification

Property-based conservation tests, concurrency tests against PostgreSQL,
idempotent replay tests, migration reconciliation, and fault injection between
operation lines and commit.

## Open questions

- Isolation level versus explicit advisory/product locks.
- Whether work orders reserve at approval or at start.
- How backorders and substitutions are represented.
