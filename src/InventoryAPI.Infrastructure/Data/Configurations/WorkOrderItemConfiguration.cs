using InventoryAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAPI.Infrastructure.Data.Configurations;

public class WorkOrderItemConfiguration : IEntityTypeConfiguration<WorkOrderItem>
{
    public void Configure(EntityTypeBuilder<WorkOrderItem> builder)
    {
        builder.ToTable("WorkOrderItems", table =>
        {
            table.HasCheckConstraint("CK_WorkOrderItems_QuantityRequested_Positive", "\"QuantityRequested\" > 0");
            table.HasCheckConstraint("CK_WorkOrderItems_QuantityIssued_Range",
                "\"QuantityIssued\" >= 0 AND \"QuantityIssued\" <= \"QuantityRequested\"");
        });

        builder.HasQueryFilter(item =>
            !item.Product.IsDeleted && !item.WorkOrder.IsDeleted);

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Notes)
            .HasMaxLength(1000);

        builder.HasOne(item => item.WorkOrder)
            .WithMany(order => order.Items)
            .HasForeignKey(item => item.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.Product)
            .WithMany(product => product.WorkOrderItems)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => item.ProductId);
        builder.HasIndex(item => new { item.WorkOrderId, item.ProductId })
            .IsUnique();
    }
}
