# C4 level 3 — API components

```mermaid
flowchart TB
    http["Controllers / middleware\nVersioning, JWT, rate limit, errors"]
    hub["SignalR hub and notification service"]
    app["Application layer\nMediatR commands/queries, validators, DTO mapping"]
    domain["Domain model\nStock arithmetic and work-order state rules"]
    uow["Unit of Work / repositories"]
    auth["Password and token services"]
    dbctx["ApplicationDbContext\nMappings, filters, append-only guard"]
    migrations["EF Core migrations"]
    pg[("PostgreSQL")]

    http --> app
    http --> auth
    app --> domain
    app --> uow
    app --> hub
    uow --> dbctx
    auth --> dbctx
    migrations --> pg
    dbctx --> pg
```

## Integrity path

For a stock operation, the controller sends a validated command to its handler.
The handler detects an operation-ID replay, loads the product/work order,
applies domain rules, appends a movement with balance snapshots, updates the
balance, and calls one unit-of-work save. PostgreSQL constraints and `xmin`
provide the final persistence boundary.
