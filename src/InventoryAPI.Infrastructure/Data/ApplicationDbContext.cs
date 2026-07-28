using System.Reflection;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Common;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Infrastructure.Data;

/// <summary>
/// Main application database context. It enforces soft-delete/audit metadata
/// and the append-only inventory-ledger boundary before persistence.
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderItem> WorkOrderItems => Set<WorkOrderItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<FilterPreset> FilterPresets => Set<FilterPreset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.Version))
                    .IsRowVersion();
            }

            if (typeof(BaseAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(GenerateSoftDeleteFilter(entityType.ClrType));
            }
        }
    }

    private static System.Linq.Expressions.LambdaExpression GenerateSoftDeleteFilter(Type entityType)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "e");
        var property = System.Linq.Expressions.Expression.Property(parameter, nameof(BaseAuditableEntity.IsDeleted));
        var falseConstant = System.Linq.Expressions.Expression.Constant(false);
        var equals = System.Linq.Expressions.Expression.Equal(property, falseConstant);
        return System.Linq.Expressions.Expression.Lambda(equals, parameter);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareForSave();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        PrepareForSave();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepareForSave()
    {
        ChangeTracker.DetectChanges();
        EnforceAppendOnlyStockLedger();
        ApplyAuditMetadata();
    }

    private void EnforceAppendOnlyStockLedger()
    {
        var invalidMovement = ChangeTracker.Entries<StockMovement>()
            .FirstOrDefault(entry => entry.State is EntityState.Modified or EntityState.Deleted);

        if (invalidMovement != null)
        {
            throw new BusinessRuleViolationException(
                $"Stock movement {invalidMovement.Entity.Id} is immutable and cannot be updated or deleted.");
        }

        var addedMovements = ChangeTracker.Entries<StockMovement>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity)
            .ToList();

        if (addedMovements.Any(movement => movement.OperationId == Guid.Empty))
        {
            throw new BusinessRuleViolationException("Every stock movement requires a non-empty operation id.");
        }

        if (addedMovements.Any(movement => movement.Type == StockMovementType.Transfer))
        {
            throw new BusinessRuleViolationException(
                "New transfer movements are not supported by the single-location inventory model.");
        }

        var duplicateOperationProduct = addedMovements
            .GroupBy(movement => new { movement.OperationId, movement.ProductId })
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateOperationProduct != null)
        {
            throw new BusinessRuleViolationException(
                $"Operation {duplicateOperationProduct.Key.OperationId} contains more than one movement for product {duplicateOperationProduct.Key.ProductId}.");
        }

        var changedProducts = ChangeTracker.Entries<Product>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
            .ToDictionary(entry => entry.Entity.Id);

        foreach (var movementGroup in addedMovements.GroupBy(movement => movement.ProductId))
        {
            if (!changedProducts.TryGetValue(movementGroup.Key, out var productEntry))
            {
                throw new BusinessRuleViolationException(
                    $"A stock movement for product {movementGroup.Key} must be committed with the corresponding tracked product balance.");
            }

            var runningBalance = productEntry.State == EntityState.Added
                ? 0
                : productEntry.OriginalValues.GetValue<int>(nameof(Product.CurrentStock));

            foreach (var movement in movementGroup.OrderBy(item => item.Timestamp).ThenBy(item => item.Id))
            {
                if (movement.Type == StockMovementType.OpeningBalance
                    && (productEntry.State != EntityState.Added || runningBalance != 0))
                {
                    throw new BusinessRuleViolationException(
                        "Opening-balance movements are valid only while creating a zero-balance product.");
                }

                var delta = StockMovement.CalculateQuantityDelta(movement.Type, movement.Quantity);
                var expectedAfter = checked((long)runningBalance + delta);

                if (movement.BalanceBefore != runningBalance
                    || movement.BalanceAfter != expectedAfter)
                {
                    throw new BusinessRuleViolationException(
                        $"Movement {movement.Id} snapshots {movement.BalanceBefore}->{movement.BalanceAfter} " +
                        $"do not match the expected ledger transition {runningBalance}->{expectedAfter}.");
                }

                runningBalance = movement.BalanceAfter;
            }

            if (runningBalance != productEntry.Entity.CurrentStock)
            {
                throw new BusinessRuleViolationException(
                    $"Ledger snapshots for product {movementGroup.Key} end at {runningBalance}, " +
                    $"but the tracked cached balance is {productEntry.Entity.CurrentStock}.");
            }
        }

        foreach (var productEntry in changedProducts.Values)
        {
            var balanceBefore = productEntry.State == EntityState.Added
                ? 0
                : productEntry.OriginalValues.GetValue<int>(nameof(Product.CurrentStock));
            var balanceDelta = (long)productEntry.Entity.CurrentStock - balanceBefore;

            if (balanceDelta == 0)
            {
                continue;
            }

            if (!addedMovements.Any(movement => movement.ProductId == productEntry.Entity.Id))
            {
                throw new BusinessRuleViolationException(
                    $"Product {productEntry.Entity.Id} balance changed by {balanceDelta} without matching immutable ledger evidence.");
            }
        }
    }

    private void ApplyAuditMetadata()
    {
        var now = DateTime.UtcNow;
        var actor = _currentUserService?.Email
            ?? _currentUserService?.UserId?.ToString()
            ?? "system";

        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    if (string.IsNullOrWhiteSpace(entry.Entity.CreatedBy))
                    {
                        entry.Entity.CreatedBy = actor;
                    }
                    break;

                case EntityState.Modified:
                    entry.Entity.ModifiedAt = now;
                    entry.Entity.ModifiedBy = actor;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.DeletedBy = actor;
                    entry.Entity.ModifiedAt = now;
                    entry.Entity.ModifiedBy = actor;
                    break;
            }
        }
    }
}
