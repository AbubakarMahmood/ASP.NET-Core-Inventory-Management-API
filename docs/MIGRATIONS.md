# Database Migrations Guide

## Overview

This guide covers everything you need to know about creating, managing, and applying Entity Framework Core migrations for the Inventory Management API.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Creating Migrations](#creating-migrations)
3. [Viewing Migrations](#viewing-migrations)
4. [Applying Migrations](#applying-migrations)
5. [Reverting Migrations](#reverting-migrations)
6. [Best Practices](#best-practices)
7. [Common Scenarios](#common-scenarios)
8. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Tools

```bash
# Install .NET SDK 8.0 or later
# Download from: https://dotnet.microsoft.com/download

# Install EF Core CLI tools globally
dotnet tool install --global dotnet-ef

# Verify installation
dotnet ef --version
# Should show: Entity Framework Core .NET Command-line Tools 8.0.x
```

### Project Structure

```
ASP.NET-Core-Inventory-Management-API-/
├── src/
│   ├── InventoryAPI.Api/              # Startup project
│   ├── InventoryAPI.Application/      # Application layer
│   ├── InventoryAPI.Domain/           # Domain entities
│   └── InventoryAPI.Infrastructure/   # DbContext and migrations
│       └── Data/
│           └── Migrations/            # Migration files stored here
```

---

## Creating Migrations

### Initial Migration (First Time Setup)

If you're starting fresh and there are no migrations:

```bash
# Navigate to project root
cd /path/to/ASP.NET-Core-Inventory-Management-API-

# Create initial migration
dotnet ef migrations add InitialCreate \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api \
  --output-dir Data/Migrations
```

**What This Does:**
- Scans all entity configurations in `InventoryAPI.Infrastructure`
- Compares current model to empty database
- Creates migration files:
  - `20250110154530_InitialCreate.cs` (migration code)
  - `20250110154530_InitialCreate.Designer.cs` (metadata)
  - `ApplicationDbContextModelSnapshot.cs` (current model snapshot)

### Adding a New Migration

When you make changes to entities or configurations:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api
```

**Example: Adding a new property to Product**

1. **Modify the entity**:
   ```csharp
   // src/InventoryAPI.Domain/Entities/Product.cs
   public class Product : BaseAuditableEntity
   {
       // Existing properties...

       // NEW: Add manufacturer property
       public string Manufacturer { get; set; } = string.Empty;
   }
   ```

2. **Update configuration** (optional but recommended):
   ```csharp
   // src/InventoryAPI.Infrastructure/Data/Configurations/ProductConfiguration.cs
   public void Configure(EntityTypeBuilder<Product> builder)
   {
       // Existing configuration...

       builder.Property(p => p.Manufacturer)
           .HasMaxLength(100);
   }
   ```

3. **Create migration**:
   ```bash
   dotnet ef migrations add AddManufacturerToProduct \
     --project src/InventoryAPI.Infrastructure \
     --startup-project src/InventoryAPI.Api
   ```

4. **Review generated migration**:
   ```csharp
   // Migration file in Data/Migrations/
   public partial class AddManufacturerToProduct : Migration
   {
       protected override void Up(MigrationBuilder migrationBuilder)
       {
           migrationBuilder.AddColumn<string>(
               name: "Manufacturer",
               table: "Products",
               type: "character varying(100)",
               maxLength: 100,
               nullable: false,
               defaultValue: "");
       }

       protected override void Down(MigrationBuilder migrationBuilder)
       {
           migrationBuilder.DropColumn(
               name: "Manufacturer",
               table: "Products");
       }
   }
   ```

---

## Viewing Migrations

### List All Migrations

```bash
# List migrations with status
dotnet ef migrations list \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api
```

**Output Example:**
```
20250110154530_InitialCreate (Applied)
20250111093045_AddManufacturerToProduct (Pending)
20250112141523_AddProductCategories (Pending)
```

### View Migration SQL

See what SQL will be executed:

```bash
# View SQL for specific migration
dotnet ef migrations script \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api \
  20250110154530_InitialCreate

# View SQL for all pending migrations
dotnet ef migrations script \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api \
  --idempotent > migration_script.sql
```

**Pro Tip:** Use `--idempotent` flag to generate SQL that can be run multiple times safely.

---

## Applying Migrations

### Development Environment

The application **automatically applies migrations** on startup in Development mode.

```bash
# Just run the application
dotnet run --project src/InventoryAPI.Api

# Migrations will be applied automatically
# Check logs for:
# "Applying 2 pending migration(s)..."
# "Database migrations applied successfully"
```

### Production Environment

**NEVER** rely on automatic migrations in production. Always apply manually:

#### Method 1: Using EF Core CLI (Recommended)

```bash
# Apply all pending migrations
dotnet ef database update \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api

# Apply to specific migration
dotnet ef database update <MigrationName> \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api
```

#### Method 2: Using Migration Script

```bash
# Use the provided script
./scripts/migrate-database.sh Production

# Or with custom connection string
./scripts/migrate-database.sh Production "Host=prod-db;Port=5432;Database=inventorydb;Username=user;Password=pass"
```

#### Method 3: Generate SQL Script (Manual Execution)

```bash
# Generate SQL script
dotnet ef migrations script \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api \
  --idempotent \
  --output migration.sql

# Review the SQL
cat migration.sql

# Execute with PostgreSQL client
psql -U inventoryuser -d inventorydb -f migration.sql
```

---

## Reverting Migrations

### Remove Last Migration (Not Applied Yet)

If you created a migration but haven't applied it:

```bash
dotnet ef migrations remove \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api
```

⚠️ **Warning:** Only works if migration hasn't been applied to database!

### Rollback Applied Migration

If migration was already applied to database:

```bash
# Rollback to specific migration
dotnet ef database update <PreviousMigrationName> \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api

# Rollback all migrations (clean slate)
dotnet ef database update 0 \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api
```

**Example:**
```bash
# Current: InitialCreate → AddManufacturer → AddCategories
# Want to rollback AddCategories

# Rollback to AddManufacturer
dotnet ef database update AddManufacturerToProduct \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api

# Now remove the migration file
dotnet ef migrations remove \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api
```

---

## Best Practices

### ✅ DO

1. **Create descriptive migration names**:
   ```bash
   # Good
   dotnet ef migrations add AddEmailVerificationToUser
   dotnet ef migrations add CreateProductCategoryTable

   # Bad
   dotnet ef migrations add Update1
   dotnet ef migrations add Fix
   ```

2. **Review generated migrations**:
   - Always check the generated SQL
   - Ensure it does what you expect
   - Look for potential data loss

3. **Test migrations locally first**:
   ```bash
   # Test on local database
   dotnet ef database update

   # Verify application works
   dotnet run --project src/InventoryAPI.Api
   ```

4. **Add data migrations when needed**:
   ```csharp
   protected override void Up(MigrationBuilder migrationBuilder)
   {
       // Schema change
       migrationBuilder.AddColumn<string>(
           name: "Status",
           table: "Orders",
           nullable: false,
           defaultValue: "Pending");

       // Data migration
       migrationBuilder.Sql(
           "UPDATE Orders SET Status = 'Completed' WHERE CompletedDate IS NOT NULL");
   }
   ```

5. **Use transactions for data migrations**:
   ```csharp
   protected override void Up(MigrationBuilder migrationBuilder)
   {
       migrationBuilder.Sql("BEGIN TRANSACTION;");

       // Your migrations

       migrationBuilder.Sql("COMMIT;");
   }
   ```

### ❌ DON'T

1. **Don't modify applied migrations**:
   - Once applied to any database (even dev), create a new migration instead

2. **Don't delete migration files manually**:
   - Use `dotnet ef migrations remove`

3. **Don't mix model changes and migrations**:
   - Make model changes, THEN create migration
   - Don't edit model while migration is pending

4. **Don't skip migrations**:
   - Apply migrations in order
   - Don't cherry-pick

---

## Common Scenarios

### Scenario 1: Adding a New Table

```csharp
// 1. Create entity
public class Category : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

// 2. Create configuration
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.Property(c => c.Name).HasMaxLength(50).IsRequired();
    }
}

// 3. Add DbSet to context
public DbSet<Category> Categories { get; set; }

// 4. Create migration
dotnet ef migrations add CreateCategoryTable \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api

// 5. Apply
dotnet ef database update \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api
```

### Scenario 2: Renaming a Column

```csharp
// DON'T just rename in entity - creates drop + add

// DO use RenameColumn in migration
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.RenameColumn(
        name: "UnitPrice",
        table: "Products",
        newName: "UnitCost");
}
```

### Scenario 3: Making a Column Nullable

```csharp
// 1. Update entity
public string? Description { get; set; }  // Added ?

// 2. Create migration
dotnet ef migrations add MakeDescriptionNullable \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api

// 3. Review generated migration
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AlterColumn<string>(
        name: "Description",
        table: "Products",
        nullable: true,  // Changed from false
        oldClrType: typeof(string),
        oldNullable: false);
}

// 4. Apply
dotnet ef database update
```

### Scenario 4: Adding Index

```csharp
// 1. Update configuration
builder.HasIndex(p => p.SKU).IsUnique();
builder.HasIndex(p => p.Category);

// 2. Create migration
dotnet ef migrations add AddProductIndexes \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api

// Generated migration will have:
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateIndex(
        name: "IX_Products_SKU",
        table: "Products",
        column: "SKU",
        unique: true);

    migrationBuilder.CreateIndex(
        name: "IX_Products_Category",
        table: "Products",
        column: "Category");
}
```

---

## Troubleshooting

### Error: "Build failed"

**Problem:** Migration command fails because project doesn't compile

**Solution:**
```bash
# Build project first
dotnet build

# Then create migration
dotnet ef migrations add MyMigration \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api
```

### Error: "Your startup project doesn't reference Microsoft.EntityFrameworkCore.Design"

**Problem:** Missing design-time package

**Solution:**
```bash
# Add package to Infrastructure project
cd src/InventoryAPI.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore.Design
```

### Error: "No DbContext was found"

**Problem:** Can't find ApplicationDbContext

**Solution:**
```bash
# Specify context explicitly
dotnet ef migrations add MyMigration \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api \
  --context ApplicationDbContext
```

### Error: "Unable to create migration - model has pending changes"

**Problem:** Model snapshot is out of sync

**Solution:**
```bash
# Remove last migration
dotnet ef migrations remove

# Rebuild
dotnet build

# Recreate migration
dotnet ef migrations add MyMigration
```

### Error: "Cannot connect to database"

**Problem:** Connection string not configured

**Solution:**
```bash
# Set connection string environment variable
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=inventorydb;Username=inventoryuser;Password=YourPassword"

# Or use --connection parameter
dotnet ef database update \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api \
  --connection "Host=localhost;Port=5432;Database=inventorydb;Username=inventoryuser;Password=YourPassword"
```

---

## Quick Reference

### Common Commands

```bash
# Create migration
dotnet ef migrations add <Name> --project src/InventoryAPI.Infrastructure --startup-project src/InventoryAPI.Api

# List migrations
dotnet ef migrations list --project src/InventoryAPI.Infrastructure --startup-project src/InventoryAPI.Api

# Apply all pending
dotnet ef database update --project src/InventoryAPI.Infrastructure --startup-project src/InventoryAPI.Api

# Apply to specific migration
dotnet ef database update <Name> --project src/InventoryAPI.Infrastructure --startup-project src/InventoryAPI.Api

# Remove last migration (not applied)
dotnet ef migrations remove --project src/InventoryAPI.Infrastructure --startup-project src/InventoryAPI.Api

# Generate SQL script
dotnet ef migrations script --project src/InventoryAPI.Infrastructure --startup-project src/InventoryAPI.Api --idempotent

# Drop database
dotnet ef database drop --project src/InventoryAPI.Infrastructure --startup-project src/InventoryAPI.Api
```

### Environment Variables

```bash
# Connection string
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=inventorydb;Username=inventoryuser;Password=pass"

# Environment
export ASPNETCORE_ENVIRONMENT=Development
```

---

## Additional Resources

- [EF Core Migrations Official Docs](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [PostgreSQL Data Types](https://www.postgresql.org/docs/current/datatype.html)
- [Entity Framework Core CLI Reference](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)

---

**Last Updated**: 2025-01-10
**Version**: 1.0.0
