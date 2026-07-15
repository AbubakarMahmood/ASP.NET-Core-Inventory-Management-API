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

        builder.Property(woi => woi.Notes)
            .HasMaxLength(1000);

        // Relationships
        builder.HasOne(woi => woi.WorkOrder)
            .WithMany(wo => wo.Items)
            .HasForeignKey(woi => woi.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(woi => woi.Product)
            .WithMany(p => p.WorkOrderItems)
            .HasForeignKey(woi => woi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(woi => woi.WorkOrderId);
        builder.HasIndex(woi => woi.ProductId);
    }
}
