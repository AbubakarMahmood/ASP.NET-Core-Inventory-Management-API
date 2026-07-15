using InventoryAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAPI.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for StockMovement
/// </summary>
public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");

        builder.HasKey(sm => sm.Id);

        builder.Property(sm => sm.SourceLocation)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sm => sm.DestinationLocation)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sm => sm.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(sm => sm.Reference)
            .HasMaxLength(100);

        builder.Property(sm => sm.UnitCostAtTransaction)
            .HasPrecision(18, 2);

        // Relationships (each configured once, with its inverse navigation)
        builder.HasOne(sm => sm.Product)
            .WithMany(p => p.StockMovements)
            .HasForeignKey(sm => sm.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sm => sm.PerformedBy)
            .WithMany(u => u.StockMovements)
            .HasForeignKey(sm => sm.PerformedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sm => sm.WorkOrder)
            .WithMany()
            .HasForeignKey(sm => sm.WorkOrderId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Indexes for common access paths
        builder.HasIndex(sm => sm.ProductId);
        builder.HasIndex(sm => sm.PerformedById);
        builder.HasIndex(sm => sm.WorkOrderId);
        builder.HasIndex(sm => sm.Timestamp);
        builder.HasIndex(sm => sm.Type);
    }
}
