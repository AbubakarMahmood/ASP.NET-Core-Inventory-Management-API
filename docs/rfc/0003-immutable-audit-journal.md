# RFC-0003: Immutable audit journal and transactional outbox

- **Status:** Draft
- **Created:** 2026-07-26

## Summary

Add a separate append-only audit journal for security- and business-significant
actions, populated transactionally through an outbox. This would replace the
current derived cross-entity activity view as the authoritative audit source.

## Motivation

Created/modified timestamps are useful operational metadata but do not record
all actions, before/after values, failed attempts, authorization context, or
proof against later mutation. Calling that view an audit log would overstate it.

## Proposed properties

- Stable event ID, correlation ID, actor, role, action, target, timestamp,
  request origin, outcome, and redacted structured detail.
- Insert-only database permissions for the application role.
- Transactional outbox entry in the same commit as the domain change.
- Asynchronous export to an independently retained sink.
- Hash chaining or signing only after a clear threat model and key-management
  design; cryptography must not be decorative.

## Privacy and security

Define retention, subject-access, redaction, and access controls. Never store
passwords, access tokens, refresh tokens, or raw secrets. Failed login logging
must avoid account enumeration and excessive personal data.

## Acceptance

Fault-injection tests, privilege tests proving update/delete denial, event/domain
transaction atomicity, redaction tests, retention tests, and independent sink
recovery evidence.
