# Inventory Management API

REST API for inventory and work order management, built with ASP.NET Core 8, PostgreSQL, and a Blazor WebAssembly front end.

It covers the workflows a small warehouse or maintenance department actually needs: product catalog with reorder points, stock movements with a full audit trail, and work orders that move through a draft → submit → approve → execute lifecycle with stock issued against them.

## Stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core 8, MediatR (CQRS), FluentValidation, Serilog |
| Data | PostgreSQL, EF Core 8 (Npgsql), migrations, `xmin` optimistic concurrency |
| Auth | JWT bearer tokens with rotating refresh tokens, role-based authorization |
| Realtime | SignalR hub for work order and low-stock notifications |
| UI | Blazor WebAssembly + MudBlazor |
| Tests | xUnit, Moq, FluentAssertions — unit + full-pipeline integration tests |
| Ops | Docker Compose, health checks, Excel/PDF export (EPPlus, QuestPDF) |

## Architecture

Clean Architecture with the dependency rule pointing inward:

```
API (controllers, middleware, SignalR, Swagger)
 └─> Application (commands/queries, handlers, validators, DTOs)
      └─> Domain (entities, business rules, domain exceptions)
 └─> Infrastructure (EF Core, repositories, JWT/password services)
      └─> Application interfaces
```

Key decisions:

- **CQRS via MediatR** — every endpoint dispatches a command or query; controllers hold no logic.
- **Business rules live in the domain.** Work order state transitions (`Submit`, `Approve`, `Reject`, `Start`, `Complete`, `Cancel`) and stock arithmetic (`AdjustStock` throws on insufficient stock) are entity methods with unit tests, not service-layer `if`s.
- **Validation pipeline** — a MediatR behavior runs FluentValidation on every request before it reaches a handler; failures come back as structured 400s.
- **Soft deletes + audit columns** — a global query filter hides deleted rows; `SaveChangesAsync` stamps created/modified/deleted metadata automatically.
- **Optimistic concurrency on PostgreSQL `xmin`** — no fake rowversion columns; the database's own transaction id detects conflicting writes, surfaced as 409s.
- **Repository + Unit of Work** over EF Core, with an execution-strategy-aware transaction helper (compatible with `EnableRetryOnFailure`).

## Running it

Requires Docker.

```bash
docker compose up --build
```

| Service | URL |
|---|---|
| API + Swagger | http://localhost:5000/swagger |
| Blazor UI | http://localhost:3000 |
| Health check | http://localhost:5000/api/v1/health |

On first start the API applies migrations and seeds demo data.

Demo accounts (`docker compose` development mode only):

| Role | Email | Password |
|---|---|---|
| Admin | admin@inventory.com | Admin123! |
| Manager | manager@inventory.com | Manager123! |
| Operator | operator@inventory.com | Operator123! |

### Running locally without Docker

You need .NET 8 SDK and a PostgreSQL instance. Point `ConnectionStrings__DefaultConnection` at your database, set `JwtSettings__SecretKey` (32+ chars), then:

```bash
dotnet run --project src/InventoryAPI.Api
```

## API overview

All endpoints are versioned under `/api/v1` and documented in Swagger. Authenticate via `POST /api/v1/auth/login`, then send the access token as a `Bearer` header.

| Area | Endpoints |
|---|---|
| Auth | `login`, `refresh` (rotating refresh tokens), `logout` |
| Products | CRUD, pagination, search, category/price/stock filters, multi-column sort, low-stock filter, Excel/PDF export |
| Stock movements | record Receipt/Issue/Adjustment/Transfer/Return, per-product history, aggregate statistics |
| Work orders | full lifecycle (`submit`, `approve`, `reject`, `start`, `complete`, `cancel`), item issuing with stock decrement, export |
| Users | admin-only CRUD, password change, role assignment |
| Audit | derived audit log across entities, export |
| Filter presets | saved per-user filters, sharing, defaults |

Role model: **Operator** works with stock and work orders, **Manager** additionally manages products and approvals, **Admin** additionally manages users and database status.

## Testing

```bash
dotnet test
```

- **Unit tests** cover the domain rules (work order transitions, stock arithmetic), token/password services, auth handlers, and validators.
- **Integration tests** boot the real HTTP pipeline via `WebApplicationFactory` and exercise login/refresh/logout, role enforcement, the complete work order lifecycle, and stock movement bookkeeping end to end.

## Migrations

Schema changes are managed with EF Core migrations:

```bash
dotnet ef migrations add <Name> \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api
```

In development the API applies pending migrations at startup. In production it refuses to start until migrations have been applied (`dotnet ef database update`), so a deploy can never run against a half-migrated schema.

## Configuration

| Setting | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| `JwtSettings:SecretKey` | HMAC signing key, minimum 32 characters — **required** |
| `JwtSettings:ExpiryMinutes` / `RefreshTokenExpiryDays` | Token lifetimes |
| `Cors:AllowedOrigins` | Array of allowed browser origins |
| `DataProtection:KeysPath` | Where data-protection keys persist (mounted volume in Docker) |

All settings can be supplied as environment variables (`Section__Key` form), as `docker-compose.yml` does.

## Project layout

```
src/
  InventoryAPI.Domain/          Entities, enums, domain exceptions
  InventoryAPI.Application/     Commands, queries, validators, DTOs, interfaces
  InventoryAPI.Infrastructure/  DbContext, migrations, repositories, auth services
  InventoryAPI.Api/             Controllers, middleware, SignalR hub, exports
  InventoryAPI.BlazorUI/        Blazor WASM front end (MudBlazor)
tests/
  InventoryAPI.UnitTests/
  InventoryAPI.IntegrationTests/
```

## License

MIT
