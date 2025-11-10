using InventoryAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAPI.Infrastructure.Data.Configuration;

/// <summary>
/// Entity configuration for StockMovement
/// </summary>
public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");

        builder.HasKey(sm => sm.Id);

        builder.Property(sm => sm.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(sm => sm.SourceLocation)
            .HasMaxLength(100);

        builder.Property(sm => sm.DestinationLocation)
            .HasMaxLength(100);

        builder.Property(sm => sm.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(sm => sm.Reference)
            .HasMaxLength(100);

        builder.Property(sm => sm.UnitCostAtTransaction)
            .HasPrecision(18, 2);

        // Use PostgreSQL's xmin system column for optimistic concurrency
        builder.UseXminAsConcurrencyToken();
        builder.Ignore(sm => sm.RowVersion);

        // Indexes for common queries
        builder.HasIndex(sm => sm.ProductId);
        builder.HasIndex(sm => sm.Timestamp);
        builder.HasIndex(sm => sm.Type);
        builder.HasIndex(sm => sm.WorkOrderId);

        // Relationships
        builder.HasOne(sm => sm.WorkOrder)
            .WithMany()
            .HasForeignKey(sm => sm.WorkOrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
