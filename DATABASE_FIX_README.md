# Database Schema Fix: Missing RowVersion Column

## Problem

The application is failing with the error:
```
Npgsql.PostgresException: 42703: column u.RowVersion does not exist
```

This error occurs because the `RowVersion` column is missing from all database tables. The entity models define `RowVersion` (inherited from `BaseEntity`), but the database schema was never updated to include these columns.

## Root Cause

The EF Core migrations directory was empty, meaning no migrations were ever created or applied to track the database schema. The database was likely created using `EnsureCreated()` or manual SQL scripts that didn't include the `RowVersion` column.

## Solution

I've created three fix options below. Choose the one that works best for your environment.

### Option 1: Apply Fix Using Docker (Recommended)

If you have Docker installed and the containers are running:

```bash
# Make the fix script executable
chmod +x fix-database.sh

# Run the fix script
./fix-database.sh

# Restart the API container to apply changes
docker-compose restart api

# Check the logs to verify the fix
docker-compose logs -f api
```

### Option 2: Apply Fix Using EF Core Migrations

If you have .NET SDK 8 installed:

```bash
# The migration files have already been created in:
# src/InventoryAPI.Infrastructure/Data/Migrations/

# Apply the migration using EF Core CLI
cd /home/user/ASP.NET-Core-Inventory-Management-API-
dotnet ef database update \
    --project src/InventoryAPI.Infrastructure \
    --startup-project src/InventoryAPI.Api

# Or use the migration script
./scripts/migrate-database.sh
```

### Option 3: Apply Fix Manually Using SQL

If you have direct database access via psql:

```bash
# Connect to the database
PGPASSWORD='InventoryPass123!' psql -h localhost -p 5433 -U inventoryuser -d inventorydb

# Or from within the Docker container
docker exec -it inventory-postgres psql -U inventoryuser -d inventorydb

# Then execute the fix-rowversion.sql script
\i fix-rowversion.sql

# Or run the SQL commands directly (see fix-rowversion.sql for the complete script)
```

### Option 4: Full Database Reset

If the above options don't work or you want a clean slate:

```bash
# This will drop everything and recreate the database with proper migrations
./reset-database.sh
```

## Files Created

1. **Migration files**:
   - `src/InventoryAPI.Infrastructure/Data/Migrations/20251123000000_AddRowVersionToAllEntities.cs`
   - `src/InventoryAPI.Infrastructure/Data/Migrations/20251123000000_AddRowVersionToAllEntities.Designer.cs`
   - `src/InventoryAPI.Infrastructure/Data/Migrations/ApplicationDbContextModelSnapshot.cs`

2. **SQL fix script**:
   - `fix-rowversion.sql` - Direct SQL commands to add RowVersion columns

3. **Automated fix script**:
   - `fix-database.sh` - Shell script that tries Docker or psql automatically

## Verification

After applying the fix, verify it worked:

1. Check that the API starts without errors:
   ```bash
   docker-compose logs api | grep -i rowversion
   ```

2. Test the login endpoint:
   ```bash
   curl -X POST http://localhost:5000/api/v1/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"admin@inventory.com","password":"Admin123!"}'
   ```

3. Check the database schema:
   ```sql
   SELECT table_name, column_name, data_type
   FROM information_schema.columns
   WHERE column_name = 'RowVersion'
   ORDER BY table_name;
   ```

   You should see `RowVersion` columns in all tables: Users, Products, WorkOrders, WorkOrderItems, StockMovements, FilterPresets.

## Understanding RowVersion

The `RowVersion` column is used for optimistic concurrency control. It:

- Automatically increments on each update
- Prevents lost updates when multiple users edit the same record
- Is configured in EF Core as `.IsRowVersion()`
- Maps to `bytea` (byte array) type in PostgreSQL

In the code, it's defined in `BaseEntity.cs`:
```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
```

## Prevention

To prevent this issue in the future:

1. Always create EF Core migrations when changing entity models:
   ```bash
   dotnet ef migrations add YourMigrationName \
       --project src/InventoryAPI.Infrastructure \
       --startup-project src/InventoryAPI.Api
   ```

2. Apply migrations in Development mode (automatic) or Production mode (manual)

3. Never use `EnsureCreated()` in production - always use migrations

4. Keep the `Migrations/` directory in source control

## Support

If you encounter issues applying this fix, check:

1. Database connectivity: `docker-compose ps`
2. API logs: `docker-compose logs api`
3. Database logs: `docker-compose logs postgres`

For additional help, see [CLAUDE.md](./CLAUDE.md) for the full architecture documentation.
