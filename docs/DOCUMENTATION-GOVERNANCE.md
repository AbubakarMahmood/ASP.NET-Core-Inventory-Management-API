# Documentation governance

## Change triggers

Update the SRS, traceability, data contracts, and tests whenever observable
behavior changes. Update C4 views when a runtime/container/component boundary
changes. Add an ADR when an architecturally significant decision is accepted.
Write an RFC before implementing a change that alters core semantics, schema,
security boundaries, compatibility, deployment topology, or operational burden.

## Status rules

- SRS requirements describe current intended behavior, not aspirations.
- ADRs are accepted history and are superseded, not silently rewritten.
- Draft RFCs are not features.
- C4 diagrams describe as-built architecture; proposed diagrams stay in RFCs.
- Claims documents distinguish designed, statically inspected, locally
  executed, provider-real, container, and remote-CI evidence.

## Review checklist

1. Does the document match source and public API names?
2. Are limitations and non-goals explicit?
3. Do requirement IDs remain stable?
4. Are new links relative and resolvable?
5. Does a test or evidence step verify each normative change?
6. Is sensitive data absent?
7. Could a shorter document communicate the same decision more accurately?

## Why there are no KEPs

Kubernetes Enhancement Proposals support a large, multi-team project with
sponsoring groups, feature graduation, version-skew policy, production
readiness, and release coordination. StockVerity has one repository and one
maintainer-controlled release boundary. Duplicating KEP machinery alongside
ADRs and RFCs would add ceremony without new governance information.
