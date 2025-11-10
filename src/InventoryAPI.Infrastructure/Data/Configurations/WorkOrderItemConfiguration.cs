using InventoryAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAPI.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for WorkOrderItem
/// </summary>
public class WorkOrderItemConfiguration : IEntityTypeConfiguration<WorkOrderItem>
{
    public void Configure(EntityTypeBuilder<WorkOrderItem> builder)
    {
        builder.ToTable("WorkOrderItems");

        builder.HasKey(woi => woi.Id);

        // Configure properties
        builder.Property(woi => woi.Notes)
            .HasMaxLength(1000);

        builder.Property(woi => woi.RowVersion)
            .IsRowVersion();

        // Configure relationships
        builder.HasOne(woi => woi.WorkOrder)
            .WithMany(wo => wo.Items)
            .HasForeignKey(woi => woi.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(woi => woi.Product)
            .WithMany()
            .HasForeignKey(woi => woi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance
        builder.HasIndex(woi => woi.WorkOrderId);
        builder.HasIndex(woi => woi.ProductId);
    }
}
