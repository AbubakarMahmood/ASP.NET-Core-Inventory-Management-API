using InventoryAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAPI.Infrastructure.Data.Configuration;

/// <summary>
/// Entity configuration for WorkOrderItem
/// </summary>
public class WorkOrderItemConfiguration : IEntityTypeConfiguration<WorkOrderItem>
{
    public void Configure(EntityTypeBuilder<WorkOrderItem> builder)
    {
        builder.ToTable("WorkOrderItems");

        builder.HasKey(woi => woi.Id);

        builder.Property(woi => woi.Notes)
            .HasMaxLength(500);

        // Use PostgreSQL's xmin system column for optimistic concurrency
        builder.UseXminAsConcurrencyToken();
        builder.Ignore(woi => woi.RowVersion);

        // Composite index for common queries
        builder.HasIndex(woi => new { woi.WorkOrderId, woi.ProductId });

        // Relationships
        builder.HasOne(woi => woi.Product)
            .WithMany()
            .HasForeignKey(woi => woi.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(woi => woi.WorkOrder)
            .WithMany(wo => wo.Items)
            .HasForeignKey(woi => woi.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
