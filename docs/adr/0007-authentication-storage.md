# ADR-0007: Versioned password hashes and hashed refresh tokens

- **Status:** Accepted
- **Date:** 2026-07-26

## Context

The legacy password representation was unversioned and refresh tokens were
stored as reusable plaintext bearer credentials.

## Decision

New passwords use `pbkdf2-sha256$600000$salt$hash`. Legacy hashes remain
verifiable only to support login-time upgrade. Refresh tokens are stored as
SHA-256 hashes and rotate on refresh.

## Consequences

A database leak no longer directly reveals active refresh tokens. Hash policy
can evolve without breaking existing users. Access tokens remain bearer tokens
and require normal transport/browser protections.
