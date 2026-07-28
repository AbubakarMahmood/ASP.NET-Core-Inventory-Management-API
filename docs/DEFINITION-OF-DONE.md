# Definition of done

StockVerity may be called **portfolio-complete** only when every mandatory item
below is checked against the final commit.

## Product and scope

- [x] README accurately describes implemented behavior and limitations.
- [ ] Product identity and repository rename are owner-approved.
- [ ] No unsupported ERP, WMS, audit-compliance, multi-location, or production
      claims remain in the profile, repository description, README, or demo.

## Source quality

- [ ] Clean checkout has no generated build outputs, logs, keys, secrets, or
      local databases tracked.
- [x] Pinned SDK restores and builds the full solution in Release mode.
- [x] API and Blazor projects publish from the current source snapshot.
- [x] Formatting/analyzer policy is selected and green.

## Functional evidence

- [x] Unit suite passes with no unexplained skips.
- [x] PostgreSQL-backed HTTP integration suite applies all migrations and passes.
- [x] Sequential and concurrent idempotency scenarios pass.
- [x] `xmin` concurrency scenario passes.
- [x] Completion-with-outstanding-parts scenario is rejected.
- [ ] Append-only movement behavior is verified at application and database
      permission levels.

## Operational evidence

- [x] API and UI images build.
- [x] Compose smoke passes in an isolated, volume-cleaned project.
- [x] Readiness fails when PostgreSQL is unavailable and recovers correctly.
- [x] Migration apply, rollback/reapply, and unsafe-data failure paths pass.
- [x] PostgreSQL backup and restore drill passes.
- [x] Demo, migration, and OpenAPI flags are disabled for the restored-database
      recovery run.

## Security

- [x] Secret scan, dependency review, and container vulnerability scan are clean
      or have documented accepted findings.
- [ ] JWT/database secrets come from an external secret store in the deployment
      evidence.
- [ ] CORS, TLS, logging redaction, token storage, and SignalR visibility have
      explicit review outcomes.
- [x] Security reporting channel is enabled.

## Documentation

- [x] SRS requirements are stable and traceable.
- [x] C4 diagrams render and match the deployed topology.
- [x] ADR/RFC indexes contain every record and statuses are accurate.
- [x] Operations, testing, security, data-contract, and claims documents match
      the final source.
- [x] All repository-local links resolve.

## Repository closure

- [ ] GitHub Actions is green on the final commit.
- [ ] Final source tag/release and changelog are created if the owner wants a
      release artifact.
- [ ] Repository slug/description/topics are updated after, not before, evidence.
- [ ] Portfolio/profile copy links to evidence and avoids stronger wording than
      the claims ledger permits.

## Kill condition

If PostgreSQL migration/concurrency evidence cannot be made reliable without a
major rewrite, or if the owner does not want to maintain the project through the
.NET 8-to-.NET 10 transition, archive the repository after preserving the audit
bundle. Do not leave it public as an indefinitely “almost production” project.
