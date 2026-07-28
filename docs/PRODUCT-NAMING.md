# Product naming decision

## Recommendation

- **Working codename:** StockVerity
- **Safe descriptive repository slug:** `auditable-inventory-ledger`
- **Final product name:** owner decision after clearance
- **Subtitle:** Auditable stock movements and work-order fulfilment

The original name, `ASP.NET-Core-Inventory-Management-API`, describes a framework
and a generic tutorial category. It does not communicate the project’s most
defensible capability: reconciling an on-hand cache with attributable movement
records while issuing parts against controlled work orders.

“StockVerity” is only a working portfolio identity. A preliminary collision
check found an established warehouse-inventory business using “Verity”, creating
material adjacent-market confusion risk even without an exact-name match. Do not
publish or register StockVerity as the final brand without professional
clearance. Before any public rename, the owner must check relevant trademarks,
domains, package names, company names, app stores, and search results in the
intended jurisdictions and markets.

A descriptive repository name is preferable to a clever but collision-prone
brand. `auditable-inventory-ledger` communicates the differentiator without
pretending that a product-name decision has been completed.

## Rename boundary

Do not rename every namespace, project, migration, or assembly merely for
presentation. Existing `InventoryAPI.*` identifiers are internal history; a
mechanical rewrite would create migration and review risk without changing the
product behavior.

After all evidence gates pass:

1. obtain owner approval and complete name-clearance checks;
2. rename the GitHub repository to `auditable-inventory-ledger` or a separately
   cleared product slug;
3. update repository description, topics, profile copy, clone links, and deploy
   references;
4. optionally migrate solution/project namespaces in a separate behavior-free
   change with a clean build and migration check;
5. preserve database migration history and record compatibility consequences.

Until then, StockVerity may remain in the source as an explicitly provisional
codename. Keep the remote rename owner-controlled and do not describe the name
as cleared or commercially available.

## Alternatives considered

| Name | Assessment |
|---|---|
| Inventory Management API | Accurate but generic, framework-shaped, and difficult to distinguish |
| PartTrack | Clear, but undersells the ledger and fulfilment integrity model |
| StockLedger | Highly descriptive, but generic and likely collision-prone |
| StoreLedger | Broad, but less connected to maintenance-parts workflows |
| BinLedger | Memorable, but misleading before per-location balances exist |
