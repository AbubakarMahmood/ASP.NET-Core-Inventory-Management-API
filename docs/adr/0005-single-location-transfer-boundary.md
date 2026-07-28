# ADR-0005: Reject new transfers in the single-location model

- **Status:** Accepted
- **Date:** 2026-07-26

## Context

A product row contains one aggregate balance and one descriptive location. A
transfer requires at least two independently accountable location balances.
Changing the location string while leaving quantity unchanged does not prove
that stock left one balance and entered another; moving only part of the balance
cannot be represented at all.

## Decision

Reject every new `Transfer` posting. Derive receipt/return destinations and
issue sources from the product's recorded location. Keep historical transfer
rows readable only for migration compatibility. Implement real transfers only
through the per-location balance and reservation design proposed in RFC-0001.

## Consequences

The current feature set is narrower but truthful. Product location remains
catalogue metadata for one aggregate balance, not a warehouse-bin ledger. Public
material must not claim transfer or multi-location support.
