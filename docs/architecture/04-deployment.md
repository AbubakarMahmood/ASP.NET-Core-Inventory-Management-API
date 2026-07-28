# C4 level 4 — local evidence deployment

```mermaid
flowchart TB
    user["Developer / reviewer browser"]

    subgraph host["Docker host"]
      subgraph compose["StockVerity Compose project"]
        ui["ui container\nnginx:alpine\nnon-root, read-only\nport 8080"]
        api["api container\n.NET ASP.NET 8.0.29\nnon-root, read-only\nport 8080"]
        pg["postgres container\nPostgreSQL 16\nbackend-only"]
        pgvol[("postgres_data")]
        keyvol[("dataprotection_keys")]
        logvol[("api_logs")]
        tmp[("API tmpfs")]
      end
    end

    user -->|localhost:3000 to 8080| ui
    user -->|localhost:5000 to 8080\noptional direct API| api
    ui -->|frontend bridge\napi:8080| api
    api -->|internal backend bridge\npostgres:5432| pg
    pg --> pgvol
    api --> keyvol
    api --> logvol
    api --> tmp
```

This is a demo/evidence topology. A production deployment should terminate TLS,
manage secrets externally, apply migrations as a controlled release step,
back up PostgreSQL, and collect logs centrally.
