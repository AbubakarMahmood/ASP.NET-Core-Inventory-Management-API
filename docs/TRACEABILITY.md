# Requirements traceability matrix

This matrix names the primary implementation and verification surfaces. A
source path is not proof by itself; the Evidence column identifies the command
or scenario that must be observed.

| Requirement(s) | Primary implementation | Verification / evidence |
|---|---|---|
| AUTH-001, AUTH-003, AUTH-009 | `LoginCommandHandler`, `TokenService`, auth middleware | login handler/unit tests; HTTP auth tests |
| AUTH-002 | `Program.cs` per-client auth limiter; `AuthController` policy | `AuthRateLimitTests.LoginLimiter_RejectsTheEleventhRequestFromOneClient` |
| AUTH-004–AUTH-006 | `User.RefreshTokenHash`, `RefreshTokenHasher`, login/refresh/revoke handlers | token, login, refresh unit tests; logout integration test |
| AUTH-007–AUTH-008 | `PasswordService` versioned and legacy verification | `PasswordServiceTests`; legacy-login handler test |
| USER-001–USER-003 | controller `[Authorize]` attributes; login active check | `AuthEndpointsTests.ProtectedSurfaces_EnforceAuthenticationAndRoles`; auth integration tests |
| USER-004 | `ApproveWorkOrderCommandHandler` | handler/unit or lifecycle test with inactive assignee |
| USER-005 | `UserConfiguration` | PostgreSQL migration-backed integration run |
| USER-006 | `DeleteUserCommandHandler` historical-attribution guard | `AuthEndpointsTests.UserWithLedgerHistory_CannotBeDeleted` |
| PROD-001–PROD-002 | product commands, validators, configuration | product HTTP tests; PostgreSQL constraints |
| PROD-003 | `Product.ApplyStockDelta`; stock check constraint | domain tests; negative-issue HTTP test; PostgreSQL tests |
| PROD-004 | PostgreSQL `xmin` concurrency token; exception middleware | `ProductEndpointsTests.ConcurrentUpdates_WithSameVersion_AllowOnlyOneWinner` |
| PROD-005, PROD-007 | `BaseAuditableEntity`, DbContext query filters, delete handler history guard | delete/query integration scenario |
| PROD-006 | product queries and controller | query integration/UI scenarios |
| LEDGER-001–LEDGER-003 | movement handlers; `ApplicationDbContext.SaveChangesAsync` guard | handler tests; persistence test; PostgreSQL integration |
| LEDGER-004–LEDGER-007 | command validator, replay logic, unique index | validator/handler tests; sequential HTTP replay; `StockMovementEndpointsTests.ConcurrentEquivalentReceipts_ApplyTheBalanceChangeOnce` |
| LEDGER-008–LEDGER-014 | `StockMovement.Post`, `Product.ApplyStockDelta`, validators and handlers | movement domain/handler tests; stock endpoint integration tests |
| LEDGER-015 | DbContext mutation guard; PostgreSQL append-only trigger | application persistence tests; provider-real raw SQL rejection test |
| LEDGER-013 | direct handler work-order issue rejection | handler test and API error scenario |
| WO-001–WO-003 | work-order entity, create validators | domain and validator tests; lifecycle integration |
| WO-004 | approval handler active-user check | inactive-assignee test |
| WO-005–WO-010 | issue command validator and handler | `IssueWorkOrderItemsCommandHandlerTests`; HTTP lifecycle tests |
| WO-011 | `WorkOrder.Complete` | domain test; premature-completion HTTP test |
| QUERY-001 | versioned controllers | HTTP integration suite |
| QUERY-002–QUERY-003 | stock DTO mapping and Blazor request/response models | mapping/unit checks; client compile; Compose smoke |
| QUERY-004 | export services/controllers | `ProductEndpointsTests.Exports_ReturnReadableXlsxAndPdfPayloads`; 10,000-row controller cap |
| QUERY-005 | activity UI and claims documentation | static text review; reviewer inspection |
| QUERY-007 | audit queries disable soft-delete filters intentionally | `AuditEndpointsTests.DeletedProductsAndUsers_RemainVisibleInAuditHistory` |
| QUERY-006 | `OpenApi:Enabled`, conditional middleware | configuration/startup tests |
| SEC-001 | empty settings, required Compose variables, retired HF demo | secret scan; Compose config review |
| SEC-002 | `ExceptionHandlingMiddleware` | integration test for unknown failure shape; log review |
| SEC-003 | Kestrel request-body limit; handled `BadHttpRequestException` | Compose oversized-body `413` smoke |
| SEC-004 | named CORS policy/configuration | startup review and browser smoke |
| SEC-005–SEC-006 | Compose volumes, Dockerfile user, read-only filesystems, network split, security options | image/config inspection, Trivy image scan, and Compose smoke |
| REL-001–REL-002 | startup initialization flow and explicit flags | startup tests with missing DB/pending migrations |
| REL-003 | EF constraints and hardening migration | PostgreSQL suite; `MigrationRehearsalTests` apply, rollback/reapply, and unsafe-data cases |
| REL-004 | `xmin`, exception middleware | concurrent PostgreSQL update test |
| REL-005 | health mappings and database health check | Compose readiness poll plus PostgreSQL stop/restart recovery |
| MAINT-001 | `global.json`, exact .NET image tags | CI tool output and image metadata |
| MAINT-002 | `docs/adr` | documentation index check |
| MAINT-003 | `docs/rfc` | documentation index check |
| MAINT-004 | this matrix | requirement-ID/link checker |
| TEST-001 | unit test project | `dotnet test tests/InventoryAPI.UnitTests` |
| TEST-002 | PostgreSQL-only `TestWebApplicationFactory` | local PostgreSQL test and CI PostgreSQL job |
| TEST-003 | `.github/workflows/ci.yml`, smoke and verify scripts | local full verification and Compose smoke; observed green Actions run for a published SHA |
| TEST-004 | claims ledger and audit report | release-review sign-off |
