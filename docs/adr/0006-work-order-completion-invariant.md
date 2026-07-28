# ADR-0006: Require full requested issuance before completion

- **Status:** Accepted
- **Date:** 2026-07-26

## Context

A completed work order with outstanding requested parts is ambiguous: the work
may be incomplete, the request may have changed, or unrecorded stock may have
been consumed.

## Decision

Completion is rejected while any item has `QuantityIssued < QuantityRequested`.
Requested changes require an explicit future amendment workflow rather than
implicit completion.

## Consequences

The lifecycle is strict and explainable. Partial fulfillment can remain
in-progress, be cancelled according to policy, or be addressed by a future RFC.
