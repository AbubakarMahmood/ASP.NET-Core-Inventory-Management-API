using InventoryAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAPI.Infrastructure.Data.Configurations;

/// <summary>Entity configuration for the append-only stock ledger.</summary>
public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements", table =>
        {
            table.HasCheckConstraint(
                "CK_StockMovements_Quantity_NonZero",
                "\"Quantity\" <> 0");
            table.HasCheckConstraint(
                "CK_StockMovements_Type_Range",
                "\"Type\" BETWEEN 1 AND 6");
            table.HasCheckConstraint(
                "CK_StockMovements_Balances_NonNegative",
                "\"BalanceBefore\" >= 0 AND \"BalanceAfter\" >= 0");
            table.HasCheckConstraint(
                "CK_StockMovements_Balance_Delta",
                "((\"Type\" IN (1, 5, 6) AND \"Quantity\" > 0 AND \"BalanceAfter\" = \"BalanceBefore\" + \"Quantity\") " +
                "OR (\"Type\" = 2 AND \"Quantity\" > 0 AND \"BalanceAfter\" = \"BalanceBefore\" - \"Quantity\") " +
                "OR (\"Type\" = 3 AND \"BalanceAfter\" = \"BalanceBefore\" + \"Quantity\") " +
                "OR (\"Type\" = 4 AND \"BalanceAfter\" = \"BalanceBefore\"))");
        });

        builder.HasQueryFilter(movement =>
            !movement.Product.IsDeleted && !movement.PerformedBy.IsDeleted);

        builder.HasKey(movement => movement.Id);

        builder.Property(movement => movement.OperationId).IsRequired();

        builder.Property(movement => movement.SourceLocation)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(movement => movement.DestinationLocation)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(movement => movement.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(movement => movement.Reference)
            .HasMaxLength(100);

        builder.Property(movement => movement.UnitCostAtTransaction)
            .HasPrecision(18, 2);

        builder.HasOne(movement => movement.Product)
            .WithMany(product => product.StockMovements)
            .HasForeignKey(movement => movement.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(movement => movement.PerformedBy)
            .WithMany(user => user.StockMovements)
            .HasForeignKey(movement => movement.PerformedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(movement => movement.WorkOrder)
            .WithMany()
            .HasForeignKey(movement => movement.WorkOrderId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasIndex(movement => new { movement.OperationId, movement.ProductId })
            .IsUnique();
        builder.HasIndex(movement => movement.ProductId);
        builder.HasIndex(movement => movement.PerformedById);
        builder.HasIndex(movement => movement.WorkOrderId);
        builder.HasIndex(movement => movement.Timestamp);
        builder.HasIndex(movement => movement.Type);
        builder.HasIndex(movement => new { movement.ProductId, movement.Timestamp });
    }
}
