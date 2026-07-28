# StockVerity Software Requirements Specification

**Version:** 1.0 source-ready baseline
**Date:** 2026-07-26
**Status:** Proposed baseline pending authoritative build, PostgreSQL, Compose,
and CI evidence

## 1. Purpose and standards posture

This SRS defines the current, bounded behavior of StockVerity. Its organization
is informed by ISO/IEC/IEEE 29148:2018 requirements-engineering guidance. It
does not claim certification, audited conformance, or reproduction of the
standard.

Normative terms **shall**, **should**, and **may** indicate required behavior,
recommended behavior, and permitted behavior respectively.

## 2. Product scope

StockVerity supports a small maintenance storeroom that controls parts and
issues them against work orders. Its primary integrity boundary is the
relationship between a product's on-hand balance, immutable stock-movement
records, and requested/issued work-order quantities.

The current release is a single-organization, single-database modular monolith.
One product row represents one aggregate balance at one recorded location.

## 3. Stakeholders and actors

| Actor | Goals |
|---|---|
| Operator | View parts, record permitted movements, progress assigned work |
| Manager | Maintain products, approve/reject work, supervise inventory |
| Administrator | Manage users and roles, inspect status, perform all manager actions |
| Maintainer | Apply migrations, configure secrets, observe health, back up and recover data |
| Reviewer | Reproduce tests and determine whether public claims are earned |

## 4. Assumptions and constraints

- PostgreSQL is the authoritative datastore.
- Browser clients use HTTPS in real deployments; local Compose uses HTTP only
  as an explicit demo setting.
- Access tokens are bearer credentials and are not a substitute for network or
  browser hardening.
- The current data model is not multi-location inventory.
- The wider activity timeline is not a tamper-evident audit journal.
- The project remains on .NET 8 until the migration proposal in RFC-0002 is
  implemented and verified.

## 5. Functional requirements

### 5.1 Authentication and identity

| ID | Requirement |
|---|---|
| AUTH-001 | The system shall authenticate an active user by email and password before issuing tokens. |
| AUTH-002 | Login and refresh endpoints shall be rate limited. |
| AUTH-003 | Access tokens shall be signed with a deployment-supplied secret of at least 32 characters. |
| AUTH-004 | Refresh tokens shall be random bearer values whose SHA-256 hashes, not plaintext values, are stored. |
| AUTH-005 | A successful refresh shall rotate the refresh token and invalidate the prior stored hash. |
| AUTH-006 | Logout and password change shall revoke the stored refresh-token hash. |
| AUTH-007 | Passwords shall use a versioned PBKDF2-SHA256 format with 600,000 iterations for new hashes. |
| AUTH-008 | A successful login using the legacy hash format shall transparently upgrade the stored hash. |
| AUTH-009 | Invalid credentials and token failures shall not disclose whether an account exists. |

### 5.2 Authorization and users

| ID | Requirement |
|---|---|
| USER-001 | The system shall enforce Operator, Manager, and Admin role boundaries at API endpoints. |
| USER-002 | Only administrators shall manage users and role assignments. |
| USER-003 | Inactive users shall not authenticate. |
| USER-004 | A work order shall not be assigned to an inactive user. |
| USER-005 | Email addresses shall be unique under the database model. |
| USER-006 | A user referenced by stock movements or work orders shall not be deleted; the account may instead be deactivated. |

### 5.3 Product catalogue

| ID | Requirement |
|---|---|
| PROD-001 | Managers and administrators shall create and update products with SKU, name, unit, cost, reorder data, and a recorded location. |
| PROD-002 | SKUs shall be unique. |
| PROD-003 | Product stock shall never become negative. |
| PROD-004 | Product mutations shall participate in PostgreSQL optimistic concurrency control. |
| PROD-005 | Product deletion shall be soft deletion and hidden by the default query filter. |
| PROD-006 | The system shall expose pagination, search, category, and low-stock filtering. |
| PROD-007 | A product with stock-movement or work-order history shall not be deleted. |

### 5.4 Stock ledger

| ID | Requirement |
|---|---|
| LEDGER-001 | Every successful stock change shall create a stock-movement ledger row in the same database commit as the balance change. |
| LEDGER-002 | A ledger row shall record product, type, signed or absolute quantity according to type, timestamp, actor, reason, reference, locations, unit cost, and balance before/after. |
| LEDGER-003 | Ledger rows shall be append-only through the application DbContext. |
| LEDGER-004 | Direct movement requests shall require a non-empty operation ID. |
| LEDGER-005 | After an equivalent direct movement has committed, repeating its operation ID shall return the original movement without changing stock again. |
| LEDGER-006 | Reusing an operation ID with a materially different payload shall return a conflict. |
| LEDGER-007 | The database shall enforce uniqueness of operation ID and product for ledger rows. |
| LEDGER-008 | An issue shall require sufficient stock and derive its recorded source location from the product. |
| LEDGER-009 | A receipt or return shall derive its recorded destination location from the product. |
| LEDGER-010 | An adjustment shall be non-zero and shall not produce a negative balance. |
| LEDGER-011 | New transfer postings shall be rejected because one aggregate balance cannot represent source and destination balances. |
| LEDGER-012 | Historical transfer rows may remain queryable for migration compatibility but shall not authorize new transfer behavior. |
| LEDGER-013 | Work-order-linked issues shall use the dedicated work-order issue endpoint. |
| LEDGER-014 | Opening balances shall be created only with a new product and committed with that product. |
| LEDGER-015 | Persisted movement rows shall be protected from update and delete by both application and PostgreSQL controls. |

### 5.5 Work orders

| ID | Requirement |
|---|---|
| WO-001 | A work order shall contain at least one requested product before submission. |
| WO-002 | A work order shall not contain the same product more than once. |
| WO-003 | State transitions shall follow Draft → Submitted → Approved or Rejected → InProgress → Completed, with cancellation subject to domain rules. |
| WO-004 | Approval shall require a valid active assignee. |
| WO-005 | Only an in-progress work order shall accept issued items. |
| WO-006 | An issue batch shall require an operation ID and contain each product at most once. |
| WO-007 | Issuing shall decrement product stock and increment the corresponding work-order item's issued quantity atomically. |
| WO-008 | An issue shall not exceed the item's remaining requested quantity. |
| WO-009 | After an equivalent issue batch has committed, repeating its operation ID shall not issue parts twice. |
| WO-010 | Reuse of an issue-batch operation ID with different products, quantities, notes, or work order shall conflict. |
| WO-011 | A work order shall not complete while any requested quantity remains unissued. |

### 5.6 Query, export, and user experience

| ID | Requirement |
|---|---|
| QUERY-001 | Authorized users shall query product, movement, and work-order data through versioned `/api/v1` endpoints. |
| QUERY-002 | Movement history shall return the recorded historical balance-after value rather than the product's current balance. |
| QUERY-003 | The browser client shall send request property names that match the public API contract. |
| QUERY-004 | The system may export supported views to CSV, Excel, or PDF, subject to authorization and memory limits. |
| QUERY-005 | The activity view shall identify itself as a derived timeline, not an immutable compliance audit journal. |
| QUERY-006 | OpenAPI shall be disabled by default and enabled only by explicit configuration. |
| QUERY-007 | Deleted unreferenced entities and immutable movement attribution shall remain readable in authorized historical views. |

## 6. Quality requirements

### 6.1 Security

| ID | Requirement |
|---|---|
| SEC-001 | Secrets shall not be committed in application settings, Compose, images, or deployment scripts. |
| SEC-002 | Unknown server errors shall return generic problem details while internal logs retain diagnostic context. |
| SEC-003 | Request bodies shall be bounded at the server. |
| SEC-004 | Browser origins shall be explicitly configured; wildcard credentialed CORS shall not be used. |
| SEC-005 | Data-protection keys shall persist outside the container writable layer. |
| SEC-006 | The API container shall run as a non-root user with no-new-privileges in Compose. |

### 6.2 Reliability and consistency

| ID | Requirement |
|---|---|
| REL-001 | Database startup shall fail closed when connectivity or migration state is invalid. |
| REL-002 | Migration application and demo seeding shall be explicit independent flags, not implicit environment behavior. |
| REL-003 | PostgreSQL shall enforce non-negative stock and key uniqueness constraints. |
| REL-004 | Concurrent product writes shall surface as conflict responses rather than silent last-write-wins updates. |
| REL-005 | The service shall expose liveness and database-backed readiness endpoints. |

### 6.3 Maintainability and evidence

| ID | Requirement |
|---|---|
| MAINT-001 | The repository shall pin the intended .NET SDK and container runtime patch versions. |
| MAINT-002 | Significant architecture decisions shall be recorded as ADRs. |
| MAINT-003 | Significant proposed semantic changes shall use RFCs before implementation. |
| MAINT-004 | Requirements shall map to implementation and verification evidence. |
| TEST-001 | Unit tests shall cover domain transitions, retry semantics, password/token behavior, and validators. |
| TEST-002 | HTTP integration tests shall require PostgreSQL and shall apply the real migration chain; no in-memory fallback is permitted. |
| TEST-003 | CI shall build, test, publish, and execute a full-stack Compose smoke scenario. |
| TEST-004 | Public claims shall distinguish source inspection, static checks, local execution, and remotely observed CI evidence. |

## 7. External interfaces

- JSON HTTP API under `/api/v1`.
- JWT bearer authentication.
- SignalR endpoint at `/api/v1/notifications`.
- PostgreSQL database through Npgsql/EF Core.
- Static Blazor WebAssembly UI, with nginx same-origin proxying in Compose.
- Health endpoints at `/health/live` and `/health/ready`.

Detailed payload semantics are in [`DATA-CONTRACTS.md`](DATA-CONTRACTS.md).

## 8. Acceptance and release conditions

The requirements baseline is accepted only when:

1. a clean checkout restores and builds with the pinned SDK;
2. unit tests pass;
3. PostgreSQL-backed integration tests apply every migration and pass;
4. the API and UI publish;
5. Compose smoke passes, including idempotent retry behavior;
6. GitHub Actions is green on the final commit;
7. documentation links and Mermaid sources are checked;
8. the owner resolves license/version/repository-rename decisions.

See [`DEFINITION-OF-DONE.md`](DEFINITION-OF-DONE.md).
