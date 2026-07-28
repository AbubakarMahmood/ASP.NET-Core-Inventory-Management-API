# RFC-0002: Migrate StockVerity to .NET 10 LTS

- **Status:** Draft
- **Created:** 2026-07-26
- **Target:** before .NET 8 support ends

## Summary

Move all projects, Microsoft runtime-family packages, CI, and container bases
from .NET 8 to .NET 10 LTS in one evidence-backed compatibility change.

## Motivation

As of this baseline, .NET 8 is in maintenance support and its support ends on
2026-11-10. .NET 10 is the active LTS line. Remaining on .NET 8 indefinitely
would weaken security and maintenance claims.

## Scope

- Update target frameworks and `global.json`.
- Update Microsoft.AspNetCore, EF Core, test host, and identity packages.
- Select a compatible Npgsql/EF provider version.
- Regenerate and inspect migrations only if the model changes; a framework
  upgrade alone must not rewrite migration history.
- Rebuild both container images and rerun all PostgreSQL/Compose evidence.

## Rollout

Use a dedicated branch and PR. Capture API compatibility warnings, migration
SQL diff, test evidence, image scan, and rollback instructions. Do not mix
feature work into the framework upgrade.

## Compatibility risks

Authentication defaults, OpenAPI packages, EF translations, `xmin` support,
Blazor/MudBlazor compatibility, test-host behavior, and container user/layout
changes.

## Acceptance

No schema drift, all unit and PostgreSQL tests pass, Compose smoke passes, and a
staging backup/restore drill succeeds on the new runtime.
