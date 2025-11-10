using InventoryAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAPI.Infrastructure.Data.Configuration;

/// <summary>
/// Entity configuration for WorkOrder
/// </summary>
public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("WorkOrders");

        builder.HasKey(wo => wo.Id);

        builder.Property(wo => wo.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(wo => wo.OrderNumber)
            .IsUnique();

        builder.Property(wo => wo.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(wo => wo.Description)
            .HasMaxLength(2000);

        builder.Property(wo => wo.Priority)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(wo => wo.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(wo => wo.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate();

        // Indexes for common queries
        builder.HasIndex(wo => wo.Status);
        builder.HasIndex(wo => wo.DueDate);
        builder.HasIndex(wo => wo.RequestedById);
        builder.HasIndex(wo => wo.AssignedToId);

        // Relationships
        builder.HasMany(wo => wo.Items)
            .WithOne(woi => woi.WorkOrder)
            .HasForeignKey(woi => woi.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
