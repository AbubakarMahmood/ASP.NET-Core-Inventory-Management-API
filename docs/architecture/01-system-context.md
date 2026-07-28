# C4 level 1 — system context

```mermaid
flowchart LR
    operator["Operator\nRecords movements and progresses work"]
    manager["Manager\nMaintains catalogue and approves work"]
    admin["Administrator\nManages users and operations"]
    maintainer["Maintainer\nDeploys, migrates, backs up, observes"]

    depot[["StockVerity\nMaintenance-parts ledger and work-order control"]]
    mail["External notification channels\nNot implemented"]

    operator -->|HTTPS / browser| depot
    manager -->|HTTPS / browser| depot
    admin -->|HTTPS / browser| depot
    maintainer -->|configuration, migrations, health| depot
    depot -. "No current integration" .-> mail
```

## Boundary notes

- StockVerity is the system of record only for its own product, movement,
  work-order, user, and filter data.
- Purchasing, finance, HR identity, and regulatory audit services are outside
  the current boundary.
