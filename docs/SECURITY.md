# Security model

## Assets

Inventory balances, movement history, work-order state, user/role data,
password hashes, refresh-token hashes, JWT signing secret, database credentials,
data-protection keys, exports, and operational logs.

## Trust boundaries

1. Browser or API client to reverse proxy/API.
2. API authorization boundary between Operator, Manager, and Admin.
3. API process to PostgreSQL.
4. Deployment platform to secret/key volumes.
5. Build pipeline to package registries and container bases.

## Implemented controls

- JWT bearer authentication with explicit issuer/audience/signing-key checks.
- Per-client-IP fixed-window rate limit on login and refresh.
- Role-gated administrative and approval endpoints.
- Versioned PBKDF2-SHA256 password hashes; legacy hashes upgrade on login.
- A fixed-cost dummy PBKDF2 verification for unknown or inactive login accounts.
- SHA-256 refresh-token hashes at rest and rotation on refresh.
- Generic unexpected-error responses with server-side logging.
- One-megabyte request-body limit with explicit `413` handling, explicit CORS
  origins, and conditional OpenAPI.
- Non-root, read-only API and UI containers with dropped capabilities,
  `no-new-privileges`, writable tmpfs mounts, and an internal backend network.
- Persisted data-protection key volume.
- Required secrets in Compose rather than committed defaults.
- Append-only application guard for stock movements and relational constraints.
- Pull-request checkout credentials are not persisted into build steps.
- Authenticated SignalR negotiation is enforced.

## Known limitations

- Blazor stores bearer tokens in browser local storage, which remains exposed to
  successful same-origin script injection. A hardened deployment should assess
  a backend-for-frontend or secure cookie design.
- SignalR business notifications are not yet scoped by resource ownership;
  authenticated users should receive only information permitted by product
  policy before external deployment.
- The activity view is not a tamper-evident audit journal.
- Local 2026-07-28 NuGet, source, secret, and rebuilt-image scans found no known
  package vulnerabilities or fixed HIGH/CRITICAL findings. SAST, DAST, and
  commit-specific CI scanning are not claimed.
- Current EF Core design tooling and the xUnit v3 runner retain deprecated but
  non-vulnerable transitive packages. Track upstream replacements rather than
  pinning unrelated runtime overrides.
- XLSX uses MIT-licensed ClosedXML. QuestPDF Community is not covered by this
  repository's MIT license; confirm current Community eligibility before
  commercial deployment.
- Demo credentials are intentionally public and must never be enabled in a real
  environment.
- Authorization is role-based, not tenant- or location-scoped.

## Deployment requirements

- TLS at the edge; enable HTTPS redirection when the API receives direct HTTP.
- Secrets from a platform secret manager; rotate on suspected exposure.
- `DemoData:Enabled=false`, `OpenApi:Enabled=false`, and normally
  `Database:ApplyMigrations=false` in production.
- Restrictive database network policy and least-privilege application role.
- Central log collection with access control and redaction review.
- Backup encryption and tested restoration.
- Dependency and container scanning as release gates.
- Reassess QuestPDF licensing if ownership, revenue, or distribution changes.

## Reporting

Do not include credentials or exploitable details in a public issue. Use the
repository Security tab's private vulnerability-reporting form and include the
affected version, reproduction, impact, and a minimal proof.
